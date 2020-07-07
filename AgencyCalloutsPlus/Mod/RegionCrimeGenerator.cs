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
        /// Containts a range of time between calls, using the current <see cref="CrimeLevel"/>.
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
        protected SpawnGenerator<ZoneInfo> CrimeZoneGenerator { get; set; }

        /// <summary>
        /// Spawn generator for random crime levels
        /// </summary>
        private static SpawnGenerator<CrimeLevelWrapper> CrimeLevelGenerator { get; set; }

        /// <summary>
        /// GameFiber containing the CallCenter functions
        /// </summary>
        private GameFiber CrimeFiber { get; set; }

        static RegionCrimeGenerator()
        {
            // Create our crime level generator
            CrimeLevelGenerator = new SpawnGenerator<CrimeLevelWrapper>();
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
            CrimeZoneGenerator = new SpawnGenerator<ZoneInfo>();

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

        public void Begin()
        {
            if (CrimeFiber == null)
            {
                IsRunning = true;

                // Register for Dispatch event
                Dispatch.OnTimeOfDayChange += Dispatch_OnTimeOfDayChange;

                // Determine our Crime level during this period
                CurrentCrimeLevel = CrimeLevelGenerator.Spawn().CrimeLevel;

                // Must be called
                AdjustCallFrequencyTimer();

                // Start GameFiber
                CrimeFiber = GameFiber.StartNew(ProcessCrimeLogic);
            }
        }

        public void End()
        {
            Dispatch.OnTimeOfDayChange -= Dispatch_OnTimeOfDayChange;
            IsRunning = false;
            CrimeFiber.Abort();
            CrimeFiber = null;
        }

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
                        optimumPatrols += zone.IdealPatrolCount;
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
                Log.Debug($"Starting next call in {time}ms (Current Crime Level: {Enum.GetName(typeof(CrimeLevel), CurrentCrimeLevel)})");

                // Wait
                GameFiber.Wait(time);
            }
        }

        /// <summary>
        /// Method called on event <see cref="Dispatch.OnTimeOfDayChange"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dispatch_OnTimeOfDayChange(object sender, EventArgs e)
        {
            var oldLevel = Enum.GetName(typeof(CrimeLevel), CurrentCrimeLevel);

            // Change our Crime level during this period
            CurrentCrimeLevel = CrimeLevelGenerator.Spawn().CrimeLevel;

            // Call process
            AdjustCallFrequencyTimer();

            // Determine message
            string current = Enum.GetName(typeof(CrimeLevel), CurrentCrimeLevel);

            // Show the player some dialog
            Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "Agency Dispatch and Callouts+",
                "~b~Call Center Update",
                $"Crime levels have changed from {oldLevel} to {current}"
            );
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
                    WorldLocation location = GetScenarioLocation(zone, scenario);
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

        protected virtual WorldLocation GetScenarioLocation(ZoneInfo zone, CalloutScenarioInfo scenario)
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
