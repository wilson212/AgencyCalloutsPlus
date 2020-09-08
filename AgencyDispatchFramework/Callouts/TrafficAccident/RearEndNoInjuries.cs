﻿using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.NativeUI;
using Rage;
using System;
using System.Collections.Generic;
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

        private Ped Victim;
        private VehicleClass VictimVehicleType;
        private Vehicle VictimVehicle;
        private Blip VictimBlip;

        #endregion

        #region Suspect fields

        private Ped Suspect;
        private VehicleClass SuspectVehicleType;
        private Vehicle SuspectVehicle;
        private Blip SuspectBlip;

        #endregion

        /// <summary>
        /// Creates a new instance of this scenario
        /// </summary>
        /// <param name="callout">The parent callout instance</param>
        /// <param name="scenarioNode">The <see cref="XmlNode"/> for this scenario specifically</param>
        public RearEndNoInjuries(Controller callout, XmlNode scenarioNode) : base(scenarioNode)
        {
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
            // Ensure our FlowOutcome is not null
            if (FlowOutcome == null)
            {
                throw new ArgumentNullException(nameof(FlowOutcome));
            }

            // Show player notification
            Rage.Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "~b~Dispatch",
                "~r~MVA",
                "Reports of a vehicle accident, respond ~b~CODE-2"
            );

            // Create victim car
            var victimV = VehicleInfo.GetRandomVehicleByType(VictimVehicleType);
            VictimVehicle = new Vehicle(victimV.ModelName, Location.Position, Location.Heading)
            {
                IsPersistent = true,
                EngineHealth = 0
            };

            // Create Victim
            Victim = VictimVehicle.CreateRandomDriver();
            Victim.IsPersistent = true;
            Victim.BlockPermanentEvents = true;

            // Create suspect vehicle 1m behind victim vehicle
            var vector = VictimVehicle.GetOffsetPositionFront(-(VictimVehicle.Length + 1f));
            var suspectV = VehicleInfo.GetRandomVehicleByType(SuspectVehicleType);
            SuspectVehicle = new Vehicle(suspectV.ModelName, vector, Location.Heading)
            {
                IsPersistent = true,
                EngineHealth = 0
            };

            // Create suspect
            Suspect = SuspectVehicle.CreateRandomDriver();
            Suspect.IsPersistent = true;
            Suspect.BlockPermanentEvents = true;

            // Attach Blips
            VictimBlip = Victim.AttachBlip();
            VictimBlip.IsRouteEnabled = true;
            SuspectBlip = Suspect.AttachBlip();

            // Damage the rear of the victim vehicle and front of suspect vehicle
            VictimVehicle.DeformRear(200, 200);
            SuspectVehicle.DeformFront(200, 200);

            // Add parser variables
            var suspect = new GamePed(Suspect);
            var victim = new GamePed(Victim);
            Parser.SetParamater("Victim", victim);
            Parser.SetParamater("Suspect", suspect);

            // Create variable dictionary
            var variables = new Dictionary<string, object>()
            {
                { "SuspectVehicleType", VehicleInfo.GetVehicleTypeDescription(SuspectVehicleType) },
                { "VictimVehicleType", VehicleInfo.GetVehicleTypeDescription(VictimVehicleType) },
                { "SuspectTitle", $"{suspect.GenderTitle}. {suspect.Persona.Surname}" },
                { "VictimTitle", $"{victim.GenderTitle}. {victim.Persona.Surname}" },
            };

            // Load converation flow sequence for the suspect
            var document = LoadFlowSequenceFile("TrafficAccident", "FlowSequence", "RearEndNoInjuries", "Suspect.xml");
            var suspectSeq = new FlowSequence("Suspect", Suspect, FlowOutcome, document, Parser);
            suspectSeq.SetVariableDictionary(variables);

            // Load converation flow sequence for the victim
            document = LoadFlowSequenceFile("TrafficAccident", "FlowSequence", "RearEndNoInjuries", "Victim.xml");
            var victimSeq = new FlowSequence("Victim", Victim, FlowOutcome, document, Parser);
            victimSeq.SetVariableDictionary(variables);

            // Register menu
            Menu = new CalloutInteractionMenu("Traffic Accident", "Rear End Collision");
            Menu.RegisterPedConversation(Suspect, suspectSeq);
            Menu.RegisterPedConversation(Victim, victimSeq);
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
            if (Rage.Game.IsKeyDown(System.Windows.Forms.Keys.Enter))
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

            Victim?.Cleanup();
            Victim = null;

            VictimVehicle?.Cleanup();
            VictimVehicle = null;

            Suspect?.Cleanup();
            Suspect = null;

            SuspectVehicle?.Cleanup();
            SuspectVehicle = null;
        }
    }
}