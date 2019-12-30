using System;
using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Integration;
using LSPD_First_Response.Mod.Callouts;
using Rage;

namespace AgencyCalloutsPlus.Callouts.Traffic
{
    [CalloutInfo("AgencyCallout.TrafficAccident", CalloutProbability.Never)]
    public class TrafficAccident : AgencyCallout
    {
        private Blip Blip;
        private bool OnScene;
        private LocationInfo SpawnPoint;

        private Ped Victim;
        private Vehicle VictimVehicle;

        private Ped Suspect;
        private Vehicle SuspectVehicle;

        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("[TRACE] AgencyCalloutsPlus.Callouts.Traffic.TrafficAccident");

            // Load spawnpoint and show area to player
            SpawnPoint = Agency.GetRandomLocationInJurisdiction(LocationType.SideOfRoad, new Range<float>(100f, 1000f));
            if (SpawnPoint == null)
            {
                Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Unable to find a location for callout: Traffic.TrafficAccident");
                return false;
            }

            // Show are blip and message
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint.Position, 40f);
            CalloutMessage = "Vehicle Accident";
            CalloutPosition = SpawnPoint.Position;

            // Register callout with Computer+ if installed
            if (ComputerPlusRunning)
            {
                CalloutID = ComputerPlusAPI.CreateCallout(
                    "Vehicle Accident: Officer Required", 
                    "Vehicle Accident", 
                    CalloutPosition, 
                    0, // Code 2
                    "Citizens reporting a traffic accident. Please respond.",
                    1, null, null);
            }

            // Play scanner
            PlayScannerAudioUsingPrefix("WE_HAVE_01 CRIME_TRAFFIC_ALERT IN_OR_ON_POSITION");

            // Return base
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            // Example notification
            Game.DisplayNotification(
                "3dtextures", 
                "mpgroundlogo_cops", 
                "~o~ANPR Hit: ~r~Owner Wanted", 
                "Dispatch to ~b~ |||", 
                "The ~o~ANPR Hit ~s~is for ~r~ ||| . ~b~Use appropriate caution."
            );
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            base.Process();
        }

        public override void End()
        {
            base.End();
        }
    }
}
