using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.NativeUI;
using AgencyDispatchFramework.Xml;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace AgencyDispatchFramework.Callouts.TrafficAccident
{
    /// <summary>
    /// One scenario of the TrafficAccident callout
    /// </summary>
    /// <remarks>
    /// Victim may or may not be at fault. No injuries in this accident
    /// </remarks>
    internal class RearEndNoInjuries : CalloutScenario
    {
        /// <summary>
        /// The callout that owns this instance
        /// </summary>
        private Controller Callout { get; set; }

        /// <summary>
        /// Gets the SpawnPoint location of this <see cref="CalloutScenario"/>
        /// </summary>
        public RoadShoulder Location => Callout.Location;

        /// <summary>
        /// Contains the interaction menu for this scenario
        /// </summary>
        private CalloutInteractionMenu Menu { get; set; }

        #region Victim fields

        private GamePed Victim;
        private VehicleClass VictimVehicleType;
        private Vehicle VictimVehicle;
        private Blip VictimBlip;

        #endregion

        #region Suspect fields

        private GamePed Suspect;
        private VehicleClass SuspectVehicleType;
        private Vehicle SuspectVehicle;
        private Blip SuspectBlip;
        private FlowSequence SuspectSeq;
        private FlowSequence VictimSeq;

        #endregion

        /// <summary>
        /// Creates a new instance of this scenario
        /// </summary>
        /// <param name="callout">The parent callout instance</param>
        /// <param name="scenarioNode">The <see cref="XmlNode"/> for this scenario specifically</param>
        public RearEndNoInjuries(Controller callout, CalloutScenarioInfo info) : base(info)
        {
            // Set internals
            Callout = callout;
        }

        /// <summary>
        /// Sets up the current CalloutScene vehicles and peds. This method must be called
        /// in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.OnCalloutAccepted()"/> method
        /// </summary>
        public override void Setup()
        {
            // Show player notification
            Rage.Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "~b~Dispatch",
                "~r~MVA",
                "Reports of a vehicle accident, respond ~b~CODE-2"
            );

            // Build a random car type generator
            var generator = new ProbabilityGenerator<VehicleSpawn>();
            generator.Add(new VehicleSpawn() { Type = VehicleClass.Compact, Probability = 5 });
            generator.Add(new VehicleSpawn() { Type = VehicleClass.Sedan, Probability = 5 });
            generator.Add(new VehicleSpawn() { Type = VehicleClass.Coupe, Probability = 5 });
            generator.Add(new VehicleSpawn() { Type = VehicleClass.Sport, Probability = 5 });
            generator.Add(new VehicleSpawn() { Type = VehicleClass.SUV, Probability = 5 });
            generator.Add(new VehicleSpawn() { Type = VehicleClass.Emergency, Probability = 3 });
            generator.Add(new VehicleSpawn() { Type = VehicleClass.Muscle, Probability = 3 });
            generator.Add(new VehicleSpawn() { Type = VehicleClass.Super, Probability = 2 });

            // Spawn random vehicle types for both the victim and suspect
            VictimVehicleType = generator.Spawn().Type;
            SuspectVehicleType = generator.Spawn().Type;

            // Create victim car
            var victimV = VehicleInfo.GetRandomVehicleByType(VictimVehicleType);
            VictimVehicle = new Vehicle(victimV.ModelName, Location.Position, Location.Heading)
            {
                IsPersistent = true,
                EngineHealth = 200f
            };

            // Create Victim
            Victim = VictimVehicle.CreateRandomDriver();
            Victim.Ped.MakeMissionPed(true);
            Victim.Ped.StartScenario("WORLD_HUMAN_STAND_MOBILE");

            // Set victim location
            var location = Location.GetSpawnPositionById(RoadShoulderPosition.SidewalkGroup1);
            Victim.Ped.SetPositionWithSnap(location);
            Victim.Ped.Heading = location.Heading;

            // Create suspect vehicle 1m behind victim vehicle
            var vector = VictimVehicle.GetOffsetPositionFront(-(VictimVehicle.Length + 1f));
            var suspectV = VehicleInfo.GetRandomVehicleByType(SuspectVehicleType);
            SuspectVehicle = new Vehicle(suspectV.ModelName, vector, Location.Heading)
            {
                IsPersistent = true,
                EngineHealth = 200f
            };

            // Create suspect
            Suspect = SuspectVehicle.CreateRandomDriver();
            Suspect.Ped.MakeMissionPed(true);
            Suspect.Ped.StartScenario("WORLD_HUMAN_STAND_IMPATIENT");

            // Set victim location
            location = Location.GetSpawnPositionById(RoadShoulderPosition.SidewalkGroup2);
            Suspect.Ped.SetPositionWithSnap(location);
            Suspect.Ped.Heading = Location.Heading; // Use forward heading

            // Attach Blips
            VictimBlip = Victim.Ped.AttachBlip();
            VictimBlip.IsRouteEnabled = true;
            SuspectBlip = Suspect.Ped.AttachBlip();

            // Damage the rear of the victim vehicle and front of suspect vehicle
            VictimVehicle.DeformRear(200, 200);
            SuspectVehicle.DeformFront(200, 200);

            // Add parser variables
            Parser.SetParamater("Victim", Victim);
            Parser.SetParamater("Suspect", Suspect);

            // Register menu
            Menu = new CalloutInteractionMenu("Traffic Accident", "Rear End Collision");

            // Create variable dictionary
            var variables = new Dictionary<string, object>()
            {
                { "SuspectVehicleType", VehicleInfo.GetVehicleTypeDescription(SuspectVehicleType) },
                { "VictimVehicleType", VehicleInfo.GetVehicleTypeDescription(VictimVehicleType) },
                { "SuspectTitle", $"{Suspect.GenderTitle}. {Suspect.Persona.Surname}" },
                { "VictimTitle", $"{Victim.GenderTitle}. {Victim.Persona.Surname}" },
            };

            // Load converation flow sequence for the suspect
            var path = Path.Combine(Main.FrameworkFolderPath, "Callouts", "TrafficAccident", "FlowSequence", nameof(RearEndNoInjuries), "Suspect.xml");
            using (var file = new FlowSequenceFile(path))
            {
                SuspectSeq = file.Parse("Suspect", FlowOutcome, Suspect, Parser);
                SuspectSeq.SetVariableDictionary(variables);
                Menu.RegisterPedConversation(Suspect, SuspectSeq);
            }

            // Load converation flow sequence for the victim
            path = Path.Combine(Main.FrameworkFolderPath, "Callouts", "TrafficAccident", "FlowSequence", nameof(RearEndNoInjuries), "Victim.xml");
            using (var file = new FlowSequenceFile(path))
            {
                VictimSeq = file.Parse("Victim", FlowOutcome, Victim, Parser);
                VictimSeq.SetVariableDictionary(variables);
                Menu.RegisterPedConversation(Victim, VictimSeq);
            }
        }

        /// <summary>
        /// Processes the current <see cref="CalloutScenario"/>. This method must be called
        /// in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.Process()"/> method
        /// on every tick
        /// </summary>
        public override void Process()
        {
            // Process Menu events
            Menu.Process();

            // Temporary
            if (Keyboard.IsComputerKeyDown(System.Windows.Forms.Keys.Enter))
            {
                World.TeleportLocalPlayer(Location.Position.Around(15f), true);
            }
        }

        /// <summary>
        /// This method is responsible for cleaning up all of the objects in this <see cref="CalloutScenario"/>.
        /// This method must be called in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.End()"/> 
        /// method
        /// </summary>
        public override void Cleanup()
        {
            // Clean up
            Menu?.Dispose();
            Menu = null;

            Victim?.Ped?.Cleanup();
            Victim = null;

            VictimVehicle?.Cleanup();
            VictimVehicle = null;

            Suspect?.Ped?.Cleanup();
            Suspect = null;

            SuspectVehicle?.Cleanup();
            SuspectVehicle = null;

            if (VictimBlip.Exists())
            {
                VictimBlip.Delete();
                //VictimBlip = null;
            }

            if (SuspectBlip.Exists())
            {
                SuspectBlip.Delete();
                //SuspectBlip = null;
            }

            SuspectSeq?.Dispose();
            VictimSeq?.Dispose();
        }

        private class VehicleSpawn : ISpawnable
        {
            public int Probability { get; set; }

            public VehicleClass Type { get; set; }
        }
    }
}
