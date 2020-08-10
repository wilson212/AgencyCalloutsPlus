using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.RageUIMenus;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Xml;

namespace AgencyCalloutsPlus.Callouts.Scenarios.DomesticViolence
{
    /// <summary>
    /// One scenario of the <see cref="Callouts.DomesticViolence"/> callout
    /// </summary>
    internal class DomesticArguingOnly : CalloutScenario
    {
        /// <summary>
        /// The callout that owns this instance
        /// </summary>
        private Callouts.DomesticViolence Callout { get; set; }

        /// <summary>
        /// Gets the SpawnPoint location of this <see cref="CalloutScenario"/>
        /// </summary>
        public SpawnPoint SpawnPoint { get; protected set; }

        private Ped Victim;
        private VehicleClass VictimVehicleType;
        private Vehicle VictimVehicle;
        private Blip VictimBlip;

        private Ped Suspect;
        private VehicleClass SuspectVehicleType;
        private Vehicle SuspectVehicle;
        private Blip SuspectBlip;

        private CalloutPedInteractionMenu Menu;

        public DomesticArguingOnly(Callouts.DomesticViolence callout, XmlNode scenarioNode) : base(scenarioNode)
        {
            // Store spawn point
            this.SpawnPoint = callout.SpawnPoint;

            // Get Victim 1 vehicle type using probability defined in the CalloutMeta.xml
            var cars = scenarioNode.SelectSingleNode("Victim1/VehicleTypes").ChildNodes;
            VictimVehicleType = GetRandomVehicleType(cars);

            // Get Victim 2 vehicle type using probability defined in the CalloutMeta.xml
            cars = scenarioNode.SelectSingleNode("Victim2/VehicleTypes").ChildNodes;
            SuspectVehicleType = GetRandomVehicleType(cars);
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
                "~r~Disturbance",
                "Reports of a two people arguing, respond ~b~CODE-2"
            );

            // Create victim car
            var victimV = VehicleInfo.GetRandomVehicleByType(VictimVehicleType);
            VictimVehicle = new Vehicle(victimV.ModelName, SpawnPoint.Position, SpawnPoint.Heading);
            VictimVehicle.IsPersistent = true;
            VictimVehicle.EngineHealth = 0;
            VictimVehicle.DeformRear(200, 200);

            // Create Victim
            Victim = VictimVehicle.CreateRandomDriver();
            Victim.IsPersistent = true;
            Victim.BlockPermanentEvents = true;

            // Create suspect vehicle 1m behind victim vehicle
            var vector = VictimVehicle.GetOffsetPositionFront(-(VictimVehicle.Length + 1f));
            var suspectV = VehicleInfo.GetRandomVehicleByType(SuspectVehicleType);

            SuspectVehicle = new Vehicle(suspectV.ModelName, vector, SpawnPoint.Heading);
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
        }

        /// <summary>
        /// Event fired when the "Speak with Subject" button is pushed on the Callout Interaction
        /// Menu
        /// </summary>
        private void SpeakWithButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            
        }

        public override void Process()
        {
            
        }

        public override void Cleanup()
        {
            
        }
    }
}
