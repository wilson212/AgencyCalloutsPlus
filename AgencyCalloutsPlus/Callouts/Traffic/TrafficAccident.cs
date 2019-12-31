using System;
using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.Integration;
using AgencyCalloutsPlus.RageUIMenus;
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
        private Blip VictimBlip;

        private Ped Suspect;
        private Vehicle SuspectVehicle;
        private Blip SuspectBlip;

        private CalloutPedInteractionMenu Menu;

        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("[TRACE] AgencyCalloutsPlus.Callouts.Traffic.TrafficAccident");

            // Load spawnpoint and show area to player
            SpawnPoint = Agency.GetRandomLocationInJurisdiction(LocationType.SideOfRoad, new Range<float>(100f, 3000f));
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
            /* Example notification
            Game.DisplayNotification(
                "3dtextures", 
                "mpgroundlogo_cops", 
                "~o~ANPR Hit: ~r~Owner Wanted", 
                "Dispatch to ~b~ |||", 
                "The ~o~ANPR Hit ~s~is for ~r~ ||| . ~b~Use appropriate caution."
            );*/

            // Create victim car
            var victimV = VehicleInfo.GetRandomVehicleByType(API.VehicleType.Sedan);
            var sideLocation = (SideOfRoadLocation)SpawnPoint;
            VictimVehicle = new Vehicle(victimV.ModelName, SpawnPoint.Position, sideLocation.Heading);
            VictimVehicle.IsPersistent = true;
            VictimVehicle.EngineHealth = 0;
            VictimVehicle.Damage(200, 200);

            // Create Victim
            Victim = VictimVehicle.CreateRandomDriver();
            Victim.IsPersistent = true;
            Victim.BlockPermanentEvents = true;

            // Create suspect vehicle
            var vector = VictimVehicle.GetOffsetPositionFront(-(VictimVehicle.Length + 1f));
            var suspectV = VehicleInfo.GetRandomVehicleByType(API.VehicleType.Sedan);

            SuspectVehicle = new Vehicle(suspectV.ModelName, vector, sideLocation.Heading);
            SuspectVehicle.IsPersistent = true;
            SuspectVehicle.EngineHealth = 0;
            SuspectVehicle.Damage(200, 200);

            // Create suspect
            Suspect = SuspectVehicle.CreateRandomDriver();
            Suspect.IsPersistent = true;
            Suspect.BlockPermanentEvents = true;

            // Attach Blips
            VictimBlip = Victim.AttachBlip();
            VictimBlip.IsRouteEnabled = true;
            SuspectBlip = SuspectVehicle.AttachBlip();

            // Register menu
            Menu = new CalloutPedInteractionMenu("Title", "Sub title");
            Menu.RegisterPed(Suspect);
            Menu.RegisterPed(Victim);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            base.Process();
            Menu.Process();

            if (Game.LocalPlayer.Character.DistanceTo(SpawnPoint.Position) < 30f)
            {

            }
        }

        public override void End()
        {
            base.End();
        }
    }
}
