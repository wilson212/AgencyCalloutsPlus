using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Callouts.Scenarios.TrafficAccident;
using AgencyCalloutsPlus.Integration;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Xml;

namespace AgencyCalloutsPlus.Callouts
{
    /// <summary>
    /// A callout representing a routine traffic accident call with multiple scenarios
    /// </summary>
    /// <remarks>
    /// All AgencyCallout type callouts must have a CalloutProbability of Never!
    /// </remarks>
    [CalloutInfo("AgencyCallout.TrafficAccident", CalloutProbability.Never)]
    public class TrafficAccident : AgencyCallout
    {
        /// <summary>
        /// Stores the <see cref="API.SpawnPoint"/> where the accident occured
        /// </summary>
        public SpawnPoint SpawnPoint { get; protected set; }

        /// <summary>
        /// Stores the current randomized scenario
        /// </summary>
        private CalloutScenario Scenario;

        /// <summary>
        /// Stores the current <see cref="CalloutScenarioInfo"/>
        /// </summary>
        private CalloutScenarioInfo ScenarioInfo;

        /// <summary>
        /// Stores the CalloutMeta.xml document as an object
        /// </summary>
        private XmlDocument ScenarioDocument;

        /// <summary>
        /// Stores the selected random scenario XmlNode
        /// </summary>
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
            // Load a random side of road location in our jurisdiction, within 2 miles travel distance
            SpawnPoint = Agency.GetRandomSideOfRoadLocation(new Range<float>(200f, 3220f));
            if (SpawnPoint == null)
            {
                Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Unable to find a location for callout: Traffic.TrafficAccident");
                return false;
            }

            // Select a random scenario based on probabilities
            ScenarioInfo = LoadRandomScenario("AgencyCallout.TrafficAccident");
            if (ScenarioInfo == null)
            {
                Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: AgencyCallout.TrafficAccident has no scenarios!");
                return false;
            }

            // Load ColloutMeta.xml document
            ScenarioDocument = LoadScenarioFile(
                "AgencyCalloutsPlus", "Callouts", "TrafficAccident", "CalloutMeta.xml"
            );

            // Select our scenario node
            ScenarioNode = ScenarioDocument.DocumentElement.SelectSingleNode("Scenarios").SelectSingleNode(ScenarioInfo.Name);
            if (ScenarioNode == null)
            {
                Game.LogTrivial(
                    $"[ERROR] AgencyCalloutsPlus: AgencyCallout.TrafficAccident does not contain scenario named: '{ScenarioInfo.Name}'!"
                );
                return false;
            }

            // Register callout with Computer+ if installed
            if (ComputerPlusRunning)
            {
                var details = ScenarioNode.SelectSingleNode("CalloutDetails").ChildNodes;
                var rando = new CryptoRandom();
                var callDetails = details[rando.Next(0, details.Count - 1)].InnerText
                    .Replace("{{location}}", World.GetStreetName(SpawnPoint.Position));

                // Create callout
                CalloutID = ComputerPlusAPI.CreateCallout(
                    "Vehicle Accident",
                    "Vehicle Accident",
                    CalloutPosition,
                    (ScenarioInfo.RespondCode3) ? 1 : 0,
                    callDetails,
                    1, null, null);
            }

            // Create scenario class handler
            Scenario = CreateScenarioInstance();

            // Show are blip and message
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint.Position, 40f);
            CalloutMessage = "Vehicle Accident";
            CalloutPosition = SpawnPoint.Position;

            // Play scanner audio
            string scannerText = ScenarioNode.SelectSingleNode("Scanner").InnerText;
            if (ScenarioInfo.RespondCode3)
                PlayScannerAudioUsingPrefix(String.Concat(scannerText, " UNITS_RESPOND_CODE_03_02"));
            else
                PlayScannerAudioUsingPrefix(scannerText);

            // Return base
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            // Setup active scene
            Scenario.Setup();

            /*
            Agency agency = Agency.GetCurrentPlayerAgency();
            SpawnPoint info = new SpawnPoint(Game.LocalPlayer.Character.Position.Around(15), Game.LocalPlayer.Character.Heading);
            var copcar = agency.SpawnPoliceVehicleOfType(PatrolType.LocalPatrol, info);*/

            // Return base
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            // AgencyCallout base class will handle the Computer+ stuff
            base.OnCalloutNotAccepted();
        }

        public override void Process()
        {
            base.Process();
            Scenario.Process();
        }

        public override void End()
        {
            Scenario.Cleanup();
            base.End();
        }

        /// <summary>
        /// Creates a new instance of the chosen scenario by name
        /// </summary>
        /// <returns></returns>
        private CalloutScenario CreateScenarioInstance()
        {
            switch (ScenarioInfo.Name)
            {
                case "RearEndNoInjuries":
                    return new RearEndNoInjuries(this, ScenarioNode);
                default:
                    throw new Exception($"Unsupported TrafficAccident Scenario '{ScenarioInfo.Name}'");
            }
        }
    }
}
