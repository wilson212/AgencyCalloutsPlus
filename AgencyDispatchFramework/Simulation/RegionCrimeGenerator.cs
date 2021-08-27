﻿using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Scripting;
using Rage;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// This class is responsible for generating crime related <see cref="PriorityCall"/>s.
    /// </summary>
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
        public Dictionary<TimePeriod, RegionCrimeInfo> RegionCrimeInfoByTimePeriod { get; private set; }

        /// <summary>
        /// Containts a range of time between calls in milliseconds (real life time), using the current <see cref="CrimeLevel"/>.
        /// </summary>
        public Range<int> CallTimerRange { get; set; }

        /// <summary>
        /// Gets the current crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public CrimeLevel CurrentCrimeLevel { get; private set; }

        /// <summary>
        /// Gets a list of zones in this region
        /// </summary>
        public WorldZone[] Zones => CrimeZoneGenerator.GetItems();

        /// <summary>
        /// Gets a list of zones in this jurisdiction
        /// </summary>
        protected ProbabilityGenerator<WorldZone> CrimeZoneGenerator { get; set; }

        /// <summary>
        /// Spawn generator for random crime levels
        /// </summary>
        private static ProbabilityGenerator<Spawnable<CrimeLevel>> CrimeLevelGenerator { get; set; }

        /// <summary>
        /// GameFiber containing the CallCenter functions
        /// </summary>
        private GameFiber CrimeFiber { get; set; }

        static RegionCrimeGenerator()
        {
            // Create our crime level generator
            CrimeLevelGenerator = new ProbabilityGenerator<Spawnable<CrimeLevel>>();
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(6, CrimeLevel.None));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(12, CrimeLevel.VeryLow));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(20, CrimeLevel.Low));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(30, CrimeLevel.Moderate));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(20, CrimeLevel.High));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(12, CrimeLevel.VeryHigh));

            // Create next random call ID
            Randomizer = new CryptoRandom();
            NextCallId = Randomizer.Next(21234, 34567);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RegionCrimeGenerator"/>
        /// </summary>
        /// <param name="agency"></param>
        /// <param name="zones"></param>
        public RegionCrimeGenerator(WorldZone[] zones)
        {
            // Create instance variables
            RegionCrimeInfoByTimePeriod = new Dictionary<TimePeriod, RegionCrimeInfo>();
            CrimeZoneGenerator = new ProbabilityGenerator<WorldZone>();
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                RegionCrimeInfoByTimePeriod.Add(period, null);
            }

            // Only attempt to add if we have zones
            if (zones.Length > 0)
            {
                CrimeZoneGenerator.AddRange(zones);
            }
            else
            {
                throw new ArgumentNullException(nameof(zones));
            }

            // Do initial evaluation
            EvaluateCrimeValues();

            // Register for this event right away
            TimeScale.OnTimeScaleChanged += TimeScale_OnTimeScaleChanged;

            // Determine our initial Crime level during this period
            CurrentCrimeLevel = CrimeLevelGenerator.Spawn().Value;
        }

        /// <summary>
        /// Adds a zone to the <see cref="RegionCrimeGenerator"/>
        /// </summary>
        /// <param name="zone"></param>
        public void AddZone(WorldZone zone)
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
        /// based on current <see cref="TimePeriod"/>
        /// </summary>
        public void Begin()
        {
            if (CrimeFiber == null)
            {
                IsRunning = true;

                // Register for Dispatch event
                GameWorld.OnTimePeriodChanged += GameWorld_OnTimeOfDayChanged;

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
            if (IsRunning)
            {
                GameWorld.OnTimePeriodChanged -= GameWorld_OnTimeOfDayChanged;
                IsRunning = false;
                CrimeFiber?.Abort();
                CrimeFiber = null;
            }
        }

        /// <summary>
        /// Uses the <see cref="ProbabilityGenerator{T}"/> to spawn which zone the next crime
        /// will be commited in
        /// </summary>
        /// <returns>returns a <see cref="WorldZone"/>, or null on failure</returns>
        public WorldZone GetNextRandomCrimeZone()
        {
            if (CrimeZoneGenerator.TrySpawn(out WorldZone zone))
            {
                return zone;
            }

            return null;
        }

        /// <summary>
        /// Gets the average calls per specified <see cref="TimePeriod"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public int GetAverageCrimeCalls(TimePeriod time)
        {
            return RegionCrimeInfoByTimePeriod[time].AverageCrimeCalls;
        }

        /// <summary>
        /// Itterates through each zone and calculates the <see cref="RegionCrimeInfo"/>
        /// </summary>
        protected void EvaluateCrimeValues()
        {
            // Clear old stuff
            RegionCrimeInfoByTimePeriod.Clear();

            // Declare vars
            int timeScaleMult = TimeScale.GetCurrentTimeScaleMultiplier();
            int msPerGameMinute = TimeScale.GetMillisecondsPerGameMinute();
            int hourGameTimeToMSRealTime = 60 * msPerGameMinute;

            // Loop through each time period and cache crime numbers
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                // Create info struct
                var crimeInfo = new RegionCrimeInfo();

                // Determine our overall crime numbers by adding each zones
                // individual crime statistics
                if (Zones.Length > 0)
                {
                    foreach (var zone in Zones)
                    {
                        // Get average calls per period
                        var calls = zone.AverageCalls[period];
                        crimeInfo.AverageCrimeCalls += calls;
                    }
                }

                // Get our average real time milliseconds per call
                if (crimeInfo.AverageCallsPerGameHour > 0)
                {
                    int realTimeMsPerCall = (int)(hourGameTimeToMSRealTime / crimeInfo.AverageCallsPerGameHour);
                    crimeInfo.AverageMillisecondsPerCall = realTimeMsPerCall;
                }

                // Add period statistics
                RegionCrimeInfoByTimePeriod[period] = crimeInfo;
            }
        }

        /// <summary>
        /// Generates a new <see cref="PriorityCall"/> within a set range of time,
        /// determined by the <see cref="Agency.OverallCrimeLevel"/>
        /// </summary>
        private void ProcessCrimeLogic()
        {
            // stored local variables
            int time = 0;
            int timesFailed = 0;

            // While we are on duty accept calls
            while (IsRunning)
            {
                // Generate a new call
                var call = GenerateCall();
                if (call == null)
                {
                    // If we keep failing, then show the player a message and quit
                    if (timesFailed > 3)
                    {
                        // Display notification to the player
                        Rage.Game.DisplayNotification(
                            "3dtextures",
                            "mpgroundlogo_cops",
                            "Agency Dispatch Framework",
                            "~o~Region Crime Generator.",
                            $"Failed to generate a call too many times. Disabling the crime generator. Please check your ~y~Game.log~w~ for errors"
                        );

                        // Log the error
                        Log.Error("RegionCrimeGenerator.ProcessCrimeLogic(): Failed to generate a call 3 times. Please contact the developer.");

                        // Turn off
                        IsRunning = false;
                        break;
                    }

                    // Log as warning for developer
                    Log.Warning($"Failed to generate a PriorityCall. Trying again in 1 second.");
                    time = 1000;

                    // Count
                    timesFailed++;
                }
                else
                {
                    // Register call so that it can be dispatched
                    Dispatch.AddIncomingCall(call);

                    // Determine random time till next call
                    time = Randomizer.Next(CallTimerRange.Minimum, CallTimerRange.Maximum);
                    Log.Debug($"Starting next call in {time}ms");

                    // Reset
                    timesFailed = 0;
                }

                // Wait
                GameFiber.Wait(time);
            }
        }

        /// <summary>
        /// Adjusts the crime frequency timer based on current <see cref="CrimeLevel"/>
        /// </summary>
        private void AdjustCallFrequencyTimer()
        {
            // Grab our RegionCrimeInfo for this time period
            var crimeInfo = RegionCrimeInfoByTimePeriod[GameWorld.CurrentTimePeriod];
            int realMSPerCall = crimeInfo.AverageMillisecondsPerCall;

            // Get time until the next TimeOfDay change
            var timeScaleMult = TimeScale.GetCurrentTimeScaleMultiplier();
            var timerUntilNext = GetTimeUntilNextTimePeriod();
            var nextChangeRealMS = (int)(timerUntilNext.TotalMilliseconds / timeScaleMult);
            var hourGameTimeToMSRealTime = 60 * TimeScale.GetMillisecondsPerGameMinute();

            int min = 0; 
            int max = 0;

            // Ensure we have any calls
            if (realMSPerCall > 0)
            {
                // Adjust call frequency timer based on current Crime Level
                switch (CurrentCrimeLevel)
                {
                    case CrimeLevel.VeryHigh:
                        min = Convert.ToInt32(realMSPerCall / 2.5d);
                        max = Convert.ToInt32(realMSPerCall / 1.75d);
                        break;
                    case CrimeLevel.High:
                        min = Convert.ToInt32(realMSPerCall / 1.75d);
                        max = Convert.ToInt32(realMSPerCall / 1.25d);
                        break;
                    case CrimeLevel.Moderate:
                        min = Convert.ToInt32(realMSPerCall / 1.25d);
                        max = Convert.ToInt32(realMSPerCall * 1.25d);
                        break;
                    case CrimeLevel.Low:
                        min = Convert.ToInt32(realMSPerCall * 1.5d);
                        max = Convert.ToInt32(realMSPerCall * 2d);
                        break;
                    case CrimeLevel.VeryLow:
                        min = Convert.ToInt32(realMSPerCall * 2d);
                        max = Convert.ToInt32(realMSPerCall * 2.5d);
                        break;
                    default:
                        // None - This gets fixed later down
                        break;
                }
            }

            // Ensure we do not float too far into the next timer period
            if (realMSPerCall == 0 || CurrentCrimeLevel == CrimeLevel.None || min > nextChangeRealMS)
            {
                min = nextChangeRealMS;
                max = nextChangeRealMS + hourGameTimeToMSRealTime;
            }

            // Adjust call frequency timer
            CallTimerRange = new Range<int>(min, max);
            if (min == 0 || !CallTimerRange.IsValid())
            {
                Log.Error($"RegionCrimeGenerator.AdjustCallFrequencyTimer(): Detected a bad call timer range of {CallTimerRange}");
                Log.Debug($"\t\t\tCurrent Crime Level: {CurrentCrimeLevel}");
                Log.Debug($"\t\t\tAvg MS Per Call: {realMSPerCall}");
                Log.Debug($"\t\t\tTime Until Next TimeOfDay MS: {nextChangeRealMS}");
            }
        }
        
        /// <summary>
        /// Gets a <see cref="TimeSpan"/> until the next time of day change using the
        /// game's current time scale
        /// </summary>
        /// <returns></returns>
        private TimeSpan GetTimeUntilNextTimePeriod()
        {
            // Now get time difference
            var gt = World.TimeOfDay;

            // Get target timespan
            var target = TimeSpan.Zero;
            switch (GameWorld.CurrentTimePeriod)
            {
                case TimePeriod.Morning:
                    target = TimeSpan.FromHours(12);
                    break;
                case TimePeriod.Day:
                    target = TimeSpan.FromHours(18);
                    break;
                case TimePeriod.Evening:
                    target = TimeSpan.FromSeconds(86399);
                    break;
                case TimePeriod.Night:
                    target = TimeSpan.FromHours(6);
                    break;
            }

            // Now get time difference
            var untilNextChange = target - gt;
            return untilNextChange;
        }

        /// <summary>
        /// Creates a <see cref="PriorityCall"/> with a crime location for the <paramref name="scenario"/>.
        /// This method does not add the call to <see cref="Dispatch"/> call queue
        /// </summary>
        /// <param name="scenario"></param>
        /// <returns></returns>
        internal PriorityCall CreateCallFromScenario(CalloutScenarioInfo scenario)
        {
            // Try to generate a call
            for (int i = 0; i < Settings.MaxLocationAttempts; i++)
            {
                try
                {
                    // Spawn a zone in our jurisdiction
                    WorldZone zone = GetNextRandomCrimeZone();
                    if (zone == null)
                    {
                        Log.Error($"RegionCrimeGenerator.CreateCallFromScenario(): Attempted to pull a zone but zone is null");
                        break;
                    }

                    // Get a random location!
                    WorldLocation location = GetScenarioLocationFromZone(zone, scenario);
                    if (location == null)
                    {
                        Log.Warning($"RegionCrimeGenerator.CreateCallFromScenario(): Zone '{zone.FullName}' does not have any available '{scenario.LocationTypeCode}' locations");
                        continue;
                    }

                    // Add call to the dispatch Queue
                    return new PriorityCall(NextCallId++, scenario, location);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new <see cref="PriorityCall"/> using <see cref="WorldStateMultipliers"/>
        /// </summary>
        /// <returns></returns>
        public virtual PriorityCall GenerateCall()
        {
            // Spawn a zone in our jurisdiction
            WorldZone zone = GetNextRandomCrimeZone();
            if (zone == null)
            {
                Log.Error($"RegionCrimeGenerator.GenerateCall(): Attempted to pull a random zone but zone is null");
                return null;
            }

            // Try to generate a call
            for (int i = 0; i < Settings.MaxLocationAttempts; i++)
            {
                try
                {
                    // Spawn crime type from our spawned zone
                    CallCategory type = zone.GetNextRandomCrimeType();
                    if (!ScenarioPool.ScenariosByCalloutType[type].TrySpawn(out CalloutScenarioInfo scenario))
                    {
                        Log.Warning($"RegionCrimeGenerator.GenerateCall(): Unable to find a CalloutScenario of CalloutType '{type}' in '{zone.FullName}'");
                        continue;
                    }

                    // Get a random location!
                    WorldLocation location = GetScenarioLocationFromZone(zone, scenario);
                    if (location == null)
                    {
                        // Log this as a warning... May need to add more locations!
                        Log.Warning($"RegionCrimeGenerator.GenerateCall(): Zone '{zone.FullName}' does not have any available '{scenario.LocationTypeCode}' locations");
                        continue;
                    }

                    // Add call to the dispatch Queue
                    return new PriorityCall(NextCallId++, scenario, location);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    break;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a <see cref="WorldLocation"/> from a <see cref="WorldZone"/> for a <see cref="CalloutScenarioInfo"/>
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="scenario"></param>
        /// <returns></returns>
        protected virtual WorldLocation GetScenarioLocationFromZone(WorldZone zone, CalloutScenarioInfo scenario)
        {
            switch (scenario.LocationTypeCode)
            {
                case LocationTypeCode.RoadShoulder:
                    return zone.GetRandomRoadShoulder(scenario.LocationFilters, true);
                case LocationTypeCode.Residence:
                    return zone.GetRandomResidence(scenario.LocationFilters, true);
            }

            return null;
        }

        #region Events

        /// <summary>
        /// Method called on event <see cref="GameWorld.OnTimePeriodChanged"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameWorld_OnTimeOfDayChanged(TimePeriod oldPeriod, TimePeriod period)
        {
            var oldLevel = CurrentCrimeLevel;

            // Change our Crime level during this period
            CurrentCrimeLevel = CrimeLevelGenerator.Spawn().Value;
            var name = Enum.GetName(typeof(TimePeriod), period);

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
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~b~Call Center Update",
                    $"The time of day is transitioning to ~y~{name}~w~. Crime levels are starting to {text}"
                );
            }
            else
            {
                // Show the player some dialog
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~b~Call Center Update",
                    $"The time of day is transitioning to ~y~{name}~w~. Crime levels are not expected to change"
                );
            }
        }

        /// <summary>
        /// Method called if the TimeScale in game is changed
        /// </summary>
        /// <param name="oldMultiplier"></param>
        /// <param name="newMultiplier"></param>
        private void TimeScale_OnTimeScaleChanged(int oldMultiplier, int newMultiplier)
        {
            // Re-evaluate
            EvaluateCrimeValues();

            // Re-adjust
            if (IsRunning)
                AdjustCallFrequencyTimer();
        }

        #endregion
    }
}
