using System;
using System.Xml;
using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.Integration;
using AgencyCalloutsPlus.RageUIMenus;
using LSPD_First_Response.Mod.Callouts;
using Rage;

namespace AgencyCalloutsPlus.Callouts
{
    [CalloutInfo("AgencyCallout.TrafficAccident", CalloutProbability.Never)]
    public class TrafficAccident : AgencyCallout
    {
        private Blip Blip;
        private bool OnScene;
        private LocationInfo SpawnPoint;

        private Ped Victim;
        private API.VehicleType VictimVehicleType;
        private Vehicle VictimVehicle;
        private Blip VictimBlip;

        private Ped Suspect;
        private API.VehicleType SuspectVehicleType;
        private Vehicle SuspectVehicle;
        private Blip SuspectBlip;

        private CalloutPedInteractionMenu Menu;

        private CalloutScenario Scenario;
        private XmlDocument ScenarioDocument;
        private XmlNode ScenarioNode;

        /// <summary>
        /// Event called right before the Callout is displayed to the player
        /// </summary>
        /// <remarks>
        /// Lets do all of our heavy loading here, BEFORE the callout begins for the Player
        /// </remarks>
        /// <returns>false on failure, true otherwise</returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            // Debug tracing
            Game.LogTrivial($"[TRACE] AgencyCalloutsPlus.Callouts.TrafficAccident.OnBeforeCalloutDisplayed()");

            // Load spawnpoint and show area to player
            SpawnPoint = Agency.GetRandomLocationInJurisdiction(LocationType.SideOfRoad, new Range<float>(100f, 3000f));
            if (SpawnPoint == null)
            {
                Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Unable to find a location for callout: Traffic.TrafficAccident");
                return false;
            }

            // Select a random scenario based on probabilities
            Scenario = LoadRandomScenario("AgencyCallout.TrafficAccident");
            if (Scenario == null)
            {
                Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: AgencyCallout.TrafficAccident has no scenarios!");
                return false;
            }

            // Load ColloutMeta.xml document
            ScenarioDocument = LoadScenarioFile(
                "AgencyCalloutsPlus", "Callouts", "TrafficAccident", "CalloutMeta.xml"
            );

            // Select our scenario node
            ScenarioNode = ScenarioDocument.DocumentElement.SelectSingleNode("Scenarios").SelectSingleNode(Scenario.Name);
            if (ScenarioNode == null)
            {
                Game.LogTrivial(
                    $"[ERROR] AgencyCalloutsPlus: AgencyCallout.TrafficAccident does not contain scenario named: '{Scenario.Name}'!"
                    );
                return false;
            }

            // Register callout with Computer+ if installed
            if (ComputerPlusRunning)
            {
                var details = ScenarioNode.SelectSingleNode("CalloutDetails").ChildNodes;
                var rando = new CryptoRandom();

                // Create callout
                CalloutID = ComputerPlusAPI.CreateCallout(
                    "Vehicle Accident",
                    "Vehicle Accident",
                    CalloutPosition,
                    (Scenario.RespondCode3) ? 1 : 0,
                    details[rando.Next(0, details.Count - 1)].InnerText,
                    1, null, null);
            }

            // Get Victim 1 vehicle type using probability defined in the CalloutMeta.xml
            var cars = ScenarioNode.SelectSingleNode("Victim1/VehicleTypes").ChildNodes;
            VictimVehicleType = GetRandomCarTypeFromScenarioNodeList(cars);

            // Get Victim 2 vehicle type using probability defined in the CalloutMeta.xml
            cars = ScenarioNode.SelectSingleNode("Victim2/VehicleTypes").ChildNodes;
            SuspectVehicleType = GetRandomCarTypeFromScenarioNodeList(cars);

            // Show are blip and message
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint.Position, 40f);
            CalloutMessage = "Vehicle Accident";
            CalloutPosition = SpawnPoint.Position;

            // Play scanner audio
            string scannerText = ScenarioNode.SelectSingleNode("Scanner").InnerText;
            if (Scenario.RespondCode3)
                PlayScannerAudioUsingPrefix(String.Concat(scannerText, " UNITS_RESPOND_CODE_03_02"));
            else
                PlayScannerAudioUsingPrefix(scannerText);

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
            var victimV = VehicleInfo.GetRandomVehicleByType(VictimVehicleType);
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
            var suspectV = VehicleInfo.GetRandomVehicleByType(SuspectVehicleType);

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
            Menu = new CalloutPedInteractionMenu("Callout Interaction", "Ped Name Goes Here");
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
        }

        public override void End()
        {
            base.End();
        }
    }
}
