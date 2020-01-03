using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.RageUIMenus;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Xml;

namespace AgencyCalloutsPlus.Callouts.Scenarios.TrafficAccident
{
    /// <summary>
    /// One scenario of the <see cref="Callouts.TrafficAccident"/> callout
    /// </summary>
    internal class RearEndNoInjuries : CalloutScenario
    {
        /// <summary>
        /// The callout that owns this instance
        /// </summary>
        private Callouts.TrafficAccident Callout { get; set; }

        private Ped Victim;
        private VehicleClass VictimVehicleType;
        private Vehicle VictimVehicle;
        private Blip VictimBlip;

        private Ped Suspect;
        private VehicleClass SuspectVehicleType;
        private Vehicle SuspectVehicle;
        private Blip SuspectBlip;

        private CalloutPedInteractionMenu Menu;

        public RearEndNoInjuries(Callouts.TrafficAccident callout, XmlNode scenarioNode)
        {
            // Store spawn point
            this.SpawnPoint = callout.SpawnPoint;

            // Get Victim 1 vehicle type using probability defined in the CalloutMeta.xml
            var cars = scenarioNode.SelectSingleNode("Victim1/VehicleTypes").ChildNodes;
            VictimVehicleType = GetRandomCarTypeFromScenarioNodeList(cars);

            // Get Victim 2 vehicle type using probability defined in the CalloutMeta.xml
            cars = scenarioNode.SelectSingleNode("Victim2/VehicleTypes").ChildNodes;
            SuspectVehicleType = GetRandomCarTypeFromScenarioNodeList(cars);
        }

        /// <summary>
        /// Sets up the current CalloutScene vehicles and peds. This method must be called
        /// in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.OnCalloutAccepted()"/> method
        /// </summary>
        public override void Setup()
        {
            // Show player notification
            Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "~b~Dispatch",
                "~r~MVA",
                "Reports of a vehicle accident, respond ~b~CODE-2"
            );

            // Create victim car
            var victimV = VehicleInfo.GetRandomVehicleByType(VictimVehicleType);
            var sideLocation = (SideOfRoadLocation)SpawnPoint;
            VictimVehicle = new Vehicle(victimV.ModelName, SpawnPoint.Position, sideLocation.Heading);
            VictimVehicle.IsPersistent = true;
            VictimVehicle.EngineHealth = 0;
            VictimVehicle.DeformRear(200, 200);

            var dime = VictimVehicle.Model.Dimensions;
            Game.LogTrivial($"Victim1 dimensions: X={dime.X}; Y={dime.Y}; Z={dime.Z}");

            // Create Victim
            Victim = VictimVehicle.CreateRandomDriver();
            Victim.IsPersistent = true;
            Victim.BlockPermanentEvents = true;

            // Create suspect vehicle 1m behind victim vehicle
            var vector = VictimVehicle.GetOffsetPositionFront(-(VictimVehicle.Length + 1f));
            var suspectV = VehicleInfo.GetRandomVehicleByType(SuspectVehicleType);

            SuspectVehicle = new Vehicle(suspectV.ModelName, vector, sideLocation.Heading);
            SuspectVehicle.IsPersistent = true;
            SuspectVehicle.EngineHealth = 0;
            SuspectVehicle.DeformFront(200, 200);

            // Create suspect
            Suspect = SuspectVehicle.CreateRandomDriver();
            Suspect.IsPersistent = true;
            Suspect.BlockPermanentEvents = true;

            // Attach Blips
            VictimBlip = Victim.AttachBlip();
            VictimBlip.IsRouteEnabled = true;
            SuspectBlip = Suspect.AttachBlip();

            // Register menu
            Menu = new CalloutPedInteractionMenu("Callout Interaction", "~b~Traffic Accident: ~y~Rear End Collision");
            Menu.RegisterPed(Suspect);
            Menu.RegisterPed(Victim);

            // Register for events
            Menu.SpeakWithButton.Activated += SpeakWithButton_Activated;
        }

        /// <summary>
        /// Event fired when the "Speak with Subject" button is pushed on the Callout Interaction
        /// Menu
        /// </summary>
        private void SpeakWithButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Temporary
            Game.DisplayNotification($"~o~Button pushed: ~b~{selectedItem.Text}");
        }

        public override void Process()
        {
            Menu.Process();
            if (Game.IsKeyDown(System.Windows.Forms.Keys.Enter))
            {
                World.TeleportLocalPlayer(SpawnPoint.Position.Around(15f), true);
            }
        }

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
