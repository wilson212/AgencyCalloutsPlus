using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.Mod;
using AgencyCalloutsPlus.Mod.Conversation;
using AgencyCalloutsPlus.RageUIMenus;
using Rage;
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

        private string FlowOutcomeId { get; set; }

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

        private ExpressionParser Parser { get; set; }

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

            var nodes = scenarioNode.SelectSingleNode("FlowSequence").SelectNodes("FlowOutcome");
            FlowOutcomeId = GetRandomFlowOutcomeIdFromScenarioNodeList(nodes);

            // Create expression parser
            Parser = new ExpressionParser();
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
            VictimVehicle = new Vehicle(victimV.ModelName, SpawnPoint.Position, SpawnPoint.Heading);
            VictimVehicle.IsPersistent = true;
            VictimVehicle.EngineHealth = 0;
            VictimVehicle.DeformRear(200, 200);

            // Create Victim
            Victim = VictimVehicle.CreateRandomDriver();
            Victim.IsPersistent = true;
            Victim.BlockPermanentEvents = true;
            Parser.SetParamater("Victim1", Victim);

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
            Parser.SetParamater("Victim2", Suspect);

            // Attach Blips
            VictimBlip = Victim.AttachBlip();
            VictimBlip.IsRouteEnabled = true;
            SuspectBlip = Suspect.AttachBlip();

            // Load flow sequences
            var document = LoadFlowSequenceFile("TrafficAccident", "FlowSequence", "RearEndNoInjuries", "Victim2.xml");
            var suspectSeq = new FlowSequence(Suspect, FlowOutcomeId, document, Parser);

            document = LoadFlowSequenceFile("TrafficAccident", "FlowSequence", "RearEndNoInjuries", "Victim1.xml");
            var victimSeq = new FlowSequence(Victim, FlowOutcomeId, document, Parser);
            victimSeq.SetVariable("CarType", GetCarDescription(VictimVehicleType));

            // Register menu
            Menu = new CalloutPedInteractionMenu("Callout Interaction", "~b~Traffic Accident: ~y~Rear End Collision");
            Menu.RegisterPedConversation(Suspect, suspectSeq);
            Menu.RegisterPedConversation(Victim, victimSeq);
        }

        public override void Process()
        {
            // Process Menu
            Menu.Process();

            // Temporary
            if (Game.IsKeyDown(System.Windows.Forms.Keys.Enter))
            {
                World.TeleportLocalPlayer(SpawnPoint.Position.Around(15f), true);
            }
        }

        public override void Cleanup()
        {
            // Clean up
            Menu?.Dispose();
            Menu = null;

            Victim?.Cleanup();
            Victim = null;

            VictimVehicle?.Cleanup();
            VictimVehicle = null;

            Suspect?.Cleanup();
            Suspect = null;

            SuspectVehicle?.Cleanup();
            SuspectVehicle = null;
        }

        private string GetCarDescription(VehicleClass vehicleType)
        {
            switch (vehicleType)
            {
                case VehicleClass.Compact:
                case VehicleClass.Coupe:
                case VehicleClass.Muscle:
                case VehicleClass.Sedan:
                case VehicleClass.Sport:
                case VehicleClass.SportClassic:
                    return "car";
                case VehicleClass.Emergency:
                    return "truck";
                case VehicleClass.SUV:
                case VehicleClass.Van:
                    return "suv";
                default:
                    return "vehicle";
            }
        }
    }
}
