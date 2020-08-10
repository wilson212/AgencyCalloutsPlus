using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Callouts;
using Rage;
using System;
using System.Collections.Generic;

namespace AgencyCalloutsPlus.Mod
{
    internal class RegionCrimeGenerator
    {
        /// <summary>
        /// Contains the last Call ID used
        /// </summary>
        protected static int NextCallId { get; set; }

        /// <summary>
        /// Randomizer method used to randomize callouts and locations
        /// </summary>
        protected static CryptoRandom Randomizer { get; set; }

        /// <summary>
        /// Indicates whether this CrimeGenerator is currently creating calls
        /// </summary>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Gets the <see cref="RegionCrimeInfo"/> based on TimeOfDay
        /// </summary>
        public Dictionary<TimeOfDay, RegionCrimeInfo> RegionCrimeInfoByTimeOfDay { get; private set; }

        /// <summary>
        /// Containts a range of time between calls in milliseconds (real life timne), using the current <see cref="CrimeLevel"/>.
        /// </summary>
        public Range<int> CallTimerRange { get; set; }

        /// <summary>
        /// Gets the current crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public CrimeLevel CurrentCrimeLevel { get; private set; }

        /// <summary>
        /// Gets a list of zones in this region
        /// </summary>
        public ZoneInfo[] Zones => CrimeZoneGenerator.GetItems();

        /// <summary>
        /// Gets a list of zones in this jurisdiction
        /// </summary>
        protected ProbabilityGenerator<ZoneInfo> CrimeZoneGenerator { get; set; }

        /// <summary>
        /// Spawn generator for random crime levels
        /// </summary>
        private static ProbabilityGenerator<CrimeLevelWrapper> CrimeLevelGenerator { get; set; }

        /// <summary>
        /// GameFiber containing the CallCenter functions
        /// </summary>
        private GameFiber CrimeFiber { get; set; }

        static RegionCrimeGenerator()
        {
            // Create our crime level generator
            CrimeLevelGenerator = new ProbabilityGenerator<CrimeLevelWrapper>();
            CrimeLevelGenerator.Add(new CrimeLevelWrapper() { CrimeLevel = CrimeLevel.None, Probability = 6 });
            CrimeLevelGenerator.Add(new CrimeLevelWrapper() { CrimeLevel = CrimeLevel.VeryLow, Probability = 12 });
            CrimeLevelGenerator.Add(new CrimeLevelWrapper() { CrimeLevel = CrimeLevel.Low, Probability = 20 });
            CrimeLevelGenerator.Add(new CrimeLevelWrapper() { CrimeLevel = CrimeLevel.Moderate, Probability = 30 });
            CrimeLevelGenerator.Add(new CrimeLevelWrapper() { CrimeLevel = CrimeLevel.High, Probability = 20 });
            CrimeLevelGenerator.Add(new CrimeLevelWrapper() { CrimeLevel = CrimeLevel.VeryHigh, Probability = 12 });

            // Create next random call ID
            Randomizer = new CryptoRandom();
            NextCallId = Randomizer.Next(21234, 34567);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RegionCrimeGenerator"/>
        /// </summary>
        /// <param name="agency"></param>
        /// <param name="zones"></param>
        public RegionCrimeGenerator(Agency agency, ZoneInfo[] zones)
        {
            // Create instance variables
            RegionCrimeInfoByTimeOfDay = new Dictionary<TimeOfDay, RegionCrimeInfo>();
            CrimeZoneGenerator = new ProbabilityGenerator<ZoneInfo>();

            // Only attempt to add if we have zones
            if (zones.Length > 0)
            {
                CrimeZoneGenerator.AddRange(zones);
            }
            else
            {
                Log.Warning($"RegionCrimeGenerator.ctor: Agency with name {agency} has no zones!");
            }

            // Do initial evaluation
            EvaluateCrimeValues();
        }

        /// <summary>
        /// Adds a zone to the <see cref="RegionCrimeGenerator"/>
        /// </summary>
        /// <param name="zone"></param>
        public void AddZone(ZoneInfo zone)
        {
            // Add zone
            CrimeZoneGenerator.Add(zone);

            // Re-do crime evaluation
            EvaluateCrimeValues();

            // Re-calculate call timers
            AdjustCallFrequencyTimer();
        }

        /// <summary>
        /// Begins a new <see cref="Rage.GameFiber"/> to spawn <see cref="CalloutScenario"/>s
        /// based on current <see cref="TimeOfDay"/>
        /// </summary>
        public void Begin()
        {
            if (CrimeFiber == null)
            {
                IsRunning = true;

                // Register for Dispatch event
                Dispatch.OnTimeOfDayChanged += Dispatch_OnTimeOfDayChanged;

                // Determine our initial Crime level during this period
                CurrentCrimeLevel = CrimeLevelGenerator.Spawn().CrimeLevel;

                // Must be called
                AdjustCallFrequencyTimer();

                // Start GameFiber
                CrimeFiber = GameFiber.StartNew(ProcessCrimeLogic);
            }
        }

        /// <summary>
        /// Stops this <see cref="RegionCrimeGenerator"/> from spawning anymore calls
        /// </summary>
        public void End()
        {
            Dispatch.OnTimeOfDayChanged -= Dispatch_OnTimeOfDayChanged;
            IsRunning = false;
            CrimeFiber.Abort();
            CrimeFiber = null;
        }

        /// <summary>
        /// Uses the <see cref="ProbabilityGenerator{T}"/> to spawn which zone the next crime
        /// will be commited in
        /// </summary>
        /// <returns>returns a <see cref="ZoneInfo"/>, or null on failure</returns>
        public ZoneInfo GetNextRandomCrimeZone()
        {
            if (CrimeZoneGenerator.TrySpawn(out ZoneInfo zone))
            {
                return zone;
            }

            return null;
        }

        /// <summary>
        /// Gets the average calls per specified <see cref="TimeOfDay"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public int GetAverageCrimeCalls(TimeOfDay time)
        {
            return RegionCrimeInfoByTimeOfDay[time].AverageCrimeCalls;
        }

        /// <summary>
        /// Itterates through each zone and calculates the <see cref="RegionCrimeInfo"/>
        /// </summary>
        protected void EvaluateCrimeValues()
        {
            // Clear old stuff
            RegionCrimeInfoByTimeOfDay.Clear();

            // Loop through each time period and cache crime numbers
            foreach (TimeOfDay period in Enum.GetValues(typeof(TimeOfDay)))
            {
                // Create info struct
                var crimeInfo = new RegionCrimeInfo();
                double optimumPatrols = 0;

                // Determine our overall crime numbers by adding each zones
                // individual crime statistics
                if (Zones.Length > 0)
                {
                    foreach (var zone in Zones)
                    {
                        // Get average calls per period
                        var calls = zone.AverageCalls[period];
                        crimeInfo.AverageCrimeCalls += calls;
                        optimumPatrols += GetOptimumPatrolCountForZone(calls, zone.Size, zone.Population);
                    }
                }

                // Get our average real time milliseconds per call
                var hourGameTimeToSecondsRealTime = (60d / Settings.TimeScale) * 60;
                var callsPerSecondRT = (crimeInfo.AverageCallsPerHour / hourGameTimeToSecondsRealTime);
                var realSecondsPerCall = (1d / callsPerSecondRT);

                // Set numbers
                crimeInfo.OptimumPatrols = (int)Math.Ceiling(optimumPatrols);
                crimeInfo.AverageMillisecondsPerCall = (int)(realSecondsPerCall * 1000);

                // Add period statistics
                RegionCrimeInfoByTimeOfDay.Add(period, crimeInfo);
            }
        }

        /// <summary>
        /// Generates a new <see cref="PriorityCall"/> within a set range of time,
        /// determined by the <see cref="Agency.OverallCrimeLevel"/>
        /// </summary>
        private void ProcessCrimeLogic()
        {
            // While we are on duty accept calls
            while (IsRunning)
            {
                // Generate a new call
                var call = GenerateCall();
                if (call == null)
                {
                    // Log as warning for developer
                    Log.Warning($"Unable to generate a PriorityCall for player Agency!");
                }
                else
                {
                    // Register call so that it can be dispatched
                    Dispatch.AddIncomingCall(call);
                }

                // Determine random time till next call
                var time = Randomizer.Next(CallTimerRange.Minimum, CallTimerRange.Maximum);
                Log.Debug($"Starting next call in {time}ms");

                // Wait
                GameFiber.Wait(time);
            }
        }

        /// <summary>
        /// Method called on event <see cref="Dispatch.OnTimeOfDayChanged"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dispatch_OnTimeOfDayChanged(object sender, EventArgs e)
        {
            var oldLevel = CurrentCrimeLevel;

            // Change our Crime level during this period
            CurrentCrimeLevel = CrimeLevelGenerator.Spawn().CrimeLevel;
            var name = Enum.GetName(typeof(TimeOfDay), Dispatch.CurrentTimeOfDay);

            // Log change
            Log.Info($"RegionCrimeGenerator: The time of day is transitioning to {name}. Settings crime level to {Enum.GetName(typeof(CrimeLevel), CurrentCrimeLevel)}");

            // Adjust our call frequency based on new crime level
            AdjustCallFrequencyTimer();

            // Determine message
            string current = Enum.GetName(typeof(CrimeLevel), CurrentCrimeLevel);
            if (CurrentCrimeLevel != oldLevel)
            {
                var oldName = Enum.GetName(typeof(CrimeLevel), CurrentCrimeLevel);
                var text = (oldLevel > CurrentCrimeLevel) ? "~g~decrease~w~" : "~y~increase~w~";

                // Show the player some dialog
                Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch and Callouts+",
                    "~b~Call Center Update",
                    $"The time of day is transitioning to ~y~{name}~w~. Crime levels are starting to {text}"
                );
            }
            else
            {
                // Show the player some dialog
                Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch and Callouts+",
                    "~b~Call Center Update",
                    $"The time of day is transitioning to ~y~{name}~w~. Crime levels are not expected to change"
                );
            }
        }

        /// <summary>
        /// Adjusts the crime frequency timer based on current <see cref="CrimeLevel"/>
        /// </summary>
        private void AdjustCallFrequencyTimer()
        {
            // Grab our RegionCrimeInfo for this time period
            var crimeInfo = RegionCrimeInfoByTimeOfDay[Dispatch.CurrentTimeOfDay];
            int ms = crimeInfo.AverageMillisecondsPerCall;

            int min = 0; 
            int max = 0;

            // Ensure we have any calls
            if (ms > 0)
            {
                // Adjust call frequency timer based on current Crime Level
                switch (CurrentCrimeLevel)
                {
                    case CrimeLevel.VeryHigh:
                        min = (int)(ms / 2.5d);
                        max = (int)(ms / 1.75d);
                        break;
                    case CrimeLevel.High:
                        min = (int)(ms / 1.75d);
                        max = (int)(ms / 1.25d);
                        break;
                    case CrimeLevel.Moderate:
                        min = (int)(ms / 1.25d);
                        max = (int)(ms * 1.25d);
                        break;
                    case CrimeLevel.Low:
                        min = (int)(ms * 1.5d);
                        max = (int)(ms * 2d);
                        break;
                    case CrimeLevel.VeryLow:
                        min = (int)(ms * 2d);
                        max = (int)(ms * 2.5d);
                        break;
                }
            }

            // Get time until the next TimeOfDay change
            var timerUntilNext = GetTimeUntilNextTimeOfDay();
            var nextChangeRealTimeMS = (timerUntilNext.Milliseconds / Settings.TimeScale);
            var hourGameTimeToSecondsRealTime = (60d / Settings.TimeScale) * 60;

            // Ensure we do not float too far into the next timer period
            if (min > nextChangeRealTimeMS)
            {
                min = nextChangeRealTimeMS;
                max = (int)(nextChangeRealTimeMS + (hourGameTimeToSecondsRealTime * 1000));
            }
            
            // Adjust call frequency timer
            CallTimerRange = new Range<int>(min, max);
        }
        
        /// <summary>
        /// Gets a <see cref="TimeSpan"/> until the next time of day change using the
        /// game's current time scale
        /// </summary>
        /// <returns></returns>
        private TimeSpan GetTimeUntilNextTimeOfDay()
        {
            // Now get time difference
            var gt = World.TimeOfDay;

            // Get target timespan
            var target = TimeSpan.Zero;
            switch (Dispatch.CurrentTimeOfDay)
            {
                case TimeOfDay.Morning:
                    target = TimeSpan.FromHours(12);
                    break;
                case TimeOfDay.Day:
                    target = TimeSpan.FromHours(18);
                    break;
                case TimeOfDay.Evening:
                    target = TimeSpan.FromSeconds(86399);
                    break;
                case TimeOfDay.Night:
                    target = TimeSpan.FromHours(6);
                    break;
            }

            // Now get time difference
            var untilNextChange = target - gt;
            return untilNextChange;
        }

        /// <summary>
        /// Determines the optimal number of patrols this zone should have based
        /// on <see cref="ZoneSize"/>, <see cref="API.Population"/> and
        /// <see cref="API.CrimeLevel"/> of crimes
        /// </summary>
        /// <param name="averageCalls"></param>
        /// <param name="Size"></param>
        /// <param name="Population"></param>
        /// <returns></returns>
        private static double GetOptimumPatrolCountForZone(int averageCalls, ZoneSize Size, Population Population)
        {
            double baseCount = Math.Max(1, averageCalls / 6d);

            switch (Size)
            {
                case ZoneSize.VerySmall:
                case ZoneSize.Small:
                    baseCount--;
                    break;
                case ZoneSize.Medium:
                case ZoneSize.Large:
                    break;
                case ZoneSize.VeryLarge:
                case ZoneSize.Massive:
                    baseCount += 1;
                    break;
            }

            switch (Population)
            {
                default: // None
                    return 0;
                case Population.Scarce:
                    baseCount *= 0.75;
                    // No adjustment
                    break;
                case Population.Moderate:
                    break;
                case Population.Dense:
                    baseCount *= 1.25;
                    break;
            }

            return Math.Max(0.25d, baseCount);
        }

        /// <summary>
        /// Creates a <see cref="PriorityCall"/> with a crime location for the <paramref name="scenario"/>.
        /// This method does not add the call to <see cref="Dispatch"/> call queue
        /// </summary>
        /// <param name="scenario"></param>
        /// <returns></returns>
        public PriorityCall CreateCallFromScenario(CalloutScenarioInfo scenario)
        {
            // Try to generate a call
            for (int i = 0; i < Settings.MaxLocationAttempts; i++)
            {
                try
                {
                    // Spawn a zone in our jurisdiction
                    ZoneInfo zone = GetNextRandomCrimeZone();
                    if (zone == null)
                    {
                        Log.Debug($"CrimeGenerator: Attempted to pull a zone but zone is null");
                        continue;
                    }

                    // Get a random location!
                    WorldLocation location = GetScenarioLocationFromZone(zone, scenario);
                    if (location == null)
                    {
                        Log.Debug($"CrimeGenerator: Unable to pull Location type {scenario.LocationType} from zone '{zone.FullName}'");
                        continue;
                    }

                    // Create PriorityCall wrapper
                    var call = new PriorityCall(NextCallId++, scenario)
                    {
                        Location = location,
                        CallStatus = CallStatus.Created,
                        Zone = zone
                    };

                    // Add call to priority Queue
                    return call;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }

            return null;
        }

        protected virtual PriorityCall GenerateCall()
        {
            // Try to generate a call
            for (int i = 0; i < Settings.MaxLocationAttempts; i++)
            {
                try
                {
                    // Spawn a zone in our jurisdiction
                    ZoneInfo zone = GetNextRandomCrimeZone();
                    if (zone == null)
                    {
                        Log.Debug($"CrimeGenerator: Attempted to pull a zone but zone is null");
                        continue;
                    }

                    // Spawn crime type from our spawned zone
                    var tod = Dispatch.CurrentTimeOfDay;
                    CalloutType type = zone.GetNextRandomCrimeType();
                    if (!Dispatch.ScenarioPool[type].TrySpawn(tod, out CalloutScenarioInfo scenario))
                    {
                        Log.Debug($"CrimeGenerator: Unable to pull CalloutType {type} from zone '{zone.FullName}'");
                        continue;
                    }

                    // Get a random location!
                    WorldLocation location = GetScenarioLocationFromZone(zone, scenario);
                    if (location == null)
                    {
                        Log.Debug($"CrimeGenerator: Unable to pull Location type {scenario.LocationType} from zone '{zone.FullName}'");
                        continue;
                    }

                    // Create PriorityCall wrapper
                    var call = new PriorityCall(NextCallId++, scenario)
                    {
                        Location = location,
                        CallStatus = CallStatus.Created,
                        Zone = zone
                    };

                    // Add call to priority Queue
                    return call;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }

            return null;
        }

        protected virtual WorldLocation GetScenarioLocationFromZone(ZoneInfo zone, CalloutScenarioInfo scenario)
        {
            switch (scenario.LocationType)
            {
                case LocationType.SideOfRoad:
                    return zone.GetRandomSideOfRoadLocation();
            }

            return null;
        }

        private class CrimeLevelWrapper : ISpawnable
        {
            public CrimeLevel CrimeLevel { get; set; }

            public int Probability { get; set; }
        }
    }
}
