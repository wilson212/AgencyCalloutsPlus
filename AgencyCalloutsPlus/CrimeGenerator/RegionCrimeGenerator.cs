using AgencyCalloutsPlus.API;
using Rage;
using System;
using System.Collections.Generic;

namespace AgencyCalloutsPlus.CrimeGenerator
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
        /// Containts a range of time between calls.
        /// </summary>
        public Range<int> CallTimerRange { get; set; }

        /// <summary>
        /// Gets the current crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public CrimeLevel CurrentCrimeLevel { get; private set; }

        /// <summary>
        /// Gets the average crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public CrimeLevel AverageCrimeLevel { get; private set; }

        /// <summary>
        /// Gets the max crime level index of this agency
        /// </summary>
        public int MaxCrimeIndex { get; protected set; }

        /// <summary>
        /// Gets the average crime level index of this Region
        /// </summary>
        public int AverageCrimeIndex { get; protected set; }

        /// <summary>
        /// Gets the optimum number of patrols to handle the crime load
        /// </summary>
        public double OptimumPatrols { get; protected set; }

        /// <summary>
        /// Gets the average number of calls per In game hour
        /// </summary>
        public double AverageCallsPerHour => (AverageCrimeIndex / 4d);

        /// <summary>
        /// Contains a list Scenarios seperated by CalloutType that will be used
        /// to populate the calls board
        /// </summary>
        public Dictionary<CalloutType, SpawnGenerator<CalloutScenarioInfo>> ScenarioPool { get; set; }

        /// <summary>
        /// Contains a list of scenario's by callout name
        /// </summary>
        public Dictionary<string, SpawnGenerator<CalloutScenarioInfo>> Scenarios { get; set; }

        /// <summary>
        /// Gets the number of zones in this jurisdiction
        /// </summary>
        public int ZoneCount => ZoneGenerator.ItemCount;

        /// <summary>
        /// Gets a list of zones in this jurisdiction
        /// </summary>
        protected SpawnGenerator<ZoneInfo> ZoneGenerator { get; set; }

        /// <summary>
        /// GameFiber containing the CallCenter functions
        /// </summary>
        private GameFiber CrimeFiber { get; set; }

        static RegionCrimeGenerator()
        {
            // Create next random call ID
            Randomizer = new CryptoRandom();
            NextCallId = Randomizer.Next(21234, 34567);
        }

        public RegionCrimeGenerator(Agency agency, ZoneInfo[] zones)
        {
            ZoneGenerator = new SpawnGenerator<ZoneInfo>();
            ZoneGenerator.AddRange(zones);

            EvaluateCrimeValues();

            // Overall crime level is number of calls per 4 hours ?
            var hourGameTimeToSecondsRealTime = (60d / Settings.TimeScale) * 60;
            var callsPerHour = (AverageCrimeIndex / 4d);
            var callsPerSecondRT = (callsPerHour / hourGameTimeToSecondsRealTime);
            var realSecondsPerCall = (1d / callsPerSecondRT);
            var milliseconds = (int)(realSecondsPerCall * 1000);

            // Create call timer range
            CallTimerRange = new Range<int>(
                (int)(milliseconds / 2d),
                (int)(milliseconds * 1.5d)
            );
        }

        public void AddZone(ZoneInfo zone)
        {
            ZoneGenerator.Add(zone);
            EvaluateCrimeValues();
        }

        public void Begin()
        {
            if (CrimeFiber == null)
            {
                IsRunning = true;
                CrimeFiber = GameFiber.StartNew(ProcessCrimeLogic);
            }
        }

        public void End()
        {
            IsRunning = false;
            CrimeFiber.Abort();
            CrimeFiber = null;
        }

        public ZoneInfo GetNextRandomCrimeZone()
        {
            if (ZoneGenerator.TrySpawn(out ZoneInfo zone))
            {
                return zone;
            }

            return null;
        }

        protected void EvaluateCrimeValues()
        {
            // Determine our overall crime level
            foreach (var zone in ZoneGenerator.GetItems())
            {
                // Skip dead zones
                if (zone.CrimeLevel == CrimeLevel.None)
                    continue;

                MaxCrimeIndex += (int)CrimeLevel.VeryHigh;
                AverageCrimeIndex += (int)zone.CrimeLevel;
                OptimumPatrols += zone.IdealPatrolCount;
            }

            // Determine our overall crime level in this agencies jurisdiction
            double percent = (AverageCrimeIndex / (double)MaxCrimeIndex);
            int val = (int)Math.Ceiling(percent * (int)CrimeLevel.VeryHigh);
            AverageCrimeLevel = (CrimeLevel)val;
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
                Dispatch.RegisterCall(call);

                // Determine random time till next call
                var time = Randomizer.Next(CallTimerRange.Minimum, CallTimerRange.Maximum);
                Log.Debug($"Starting next call in {time}ms");

                // Wait
                GameFiber.Wait(time);
            }
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
                    CalloutType type = zone.GetNextRandomCrimeType();
                    if (!Dispatch.ScenarioPool[type].TrySpawn(out CalloutScenarioInfo scenario))
                    {
                        Log.Debug($"CrimeGenerator: Unable to pull CalloutType {type} from zone '{zone.FriendlyName}'");
                        continue;
                    }

                    // Get a random location!
                    WorldLocation location = GetScenarioLocation(zone, scenario);
                    if (location == null)
                    {
                        Log.Debug($"CrimeGenerator: Unable to pull Location type {scenario.LocationType} from zone '{zone.FriendlyName}'");
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

        protected WorldLocation GetScenarioLocation(ZoneInfo zone, CalloutScenarioInfo scenario)
        {
            switch (scenario.LocationType)
            {
                case LocationType.SideOfRoad:
                    return zone.GetRandomSideOfRoadLocation();
            }

            return null;
        }
    }
}
