using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;
using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Integration;

namespace AgencyCalloutsPlus.Callouts.Traffic
{
    [CalloutInfo("AgencyCallout.TrafficAccident", CalloutProbability.Never)]
    public class TrafficAccident : AgencyCallout
    {
        private Blip Blip;
        private bool OnScene;
        private Location SpawnPoint;

        private Ped Victim;
        private Vehicle VictimVehicle;

        private Ped Suspect;
        private Vehicle SuspectVehicle;

        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("[TRACE] AgencyCalloutsPlus.Callouts.Traffic.TrafficAccident");

            // Load spawnpoint and show area to player
            SpawnPoint = Agency.GetRandomLocationInJurisdiction(LocationType.SideOfRoad, new Range<float>(100f, 2000f));
            if (SpawnPoint == null)
            {
                Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Unable to find a location for callout: Traffic.TrafficAccident");
                return false;
            }

            // Show are blip and message
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint.Position, 40f);
            CalloutMessage = "~r~911 Report:~s~ Vehicle accident.";
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
            return base.OnCalloutAccepted();
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
