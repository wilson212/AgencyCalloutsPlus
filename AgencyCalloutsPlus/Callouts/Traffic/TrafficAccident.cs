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

namespace AgencyCalloutsPlus.Callouts.Traffic
{
    [CalloutInfo("TrafficAccident", CalloutProbability.Never)]
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
            // Load spawnpoint and show area to player
            SpawnPoint = Agency.GetRandomLocationInJurisdiction(LocationType.SideOfRoad, new Range<float>(100f, 2000f));
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint.Position, 40f);

            //
            CalloutMessage = "~r~911 Report:~s~ Vehicle accident.";
            CalloutPosition = SpawnPoint.Position;

            // Play scanner
            PlayScannerAudioUsingPrefix("WE_HAVE_01 CRIME_TRAFFIC_ALERT IN_OR_ON_POSITION", CalloutPosition);

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
