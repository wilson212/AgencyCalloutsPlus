using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Callouts.Scenarios.TrafficAccident;
using LSPD_First_Response.Mod.Callouts;
using System;
using System.Xml;

namespace AgencyCalloutsPlus.Callouts
{
    /// <summary>
    /// A callout representing a routine traffic accident call with multiple scenarios
    /// </summary>
    /// <remarks>
    /// All AgencyCallout type callouts must have a CalloutProbability of Never!
    /// This is due to a reliance on the <see cref="Dispatch"/> class for location
    /// and <see cref="CalloutScenarioInfo"/> information.
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
            // Grab the priority call dispatched to player
            PriorityCall call = Dispatch.RequestPlayerCallInfo(typeof(TrafficAccident));
            if (call == null)
            {
                Log.Error("AgencyCallout.TrafficAccident: This is awkward... No PriorityCall of this type for player");
                return false;
            }

            // Store data
            ActiveCall = call;
            SpawnPoint = call.Location as SpawnPoint;
            ScenarioNode = LoadScenarioNode(call.ScenarioInfo);

            // Create scenario class handler
            Scenario = CreateScenarioInstance();

            // Show are blip and message
            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint.Position, 40f);
            CalloutMessage = "Vehicle Accident";
            CalloutPosition = SpawnPoint.Position;

            // Play scanner audio
            if (call.ScenarioInfo.ResponseCode == 3)
                PlayScannerAudioUsingCallsign(String.Concat(call.ScenarioInfo.ScannerText, " UNITS_RESPOND_CODE_03_02"));
            else
                PlayScannerAudioUsingCallsign(call.ScenarioInfo.ScannerText);

            // Return base
            return base.OnBeforeCalloutDisplayed();
        }

        /// <summary>
        /// Event called right after the Callout is accepted by the player
        /// </summary>
        /// <returns>false on failure, true otherwise</returns>
        public override bool OnCalloutAccepted()
        {
            try
            {
                // Setup active scene
                Scenario.Setup();
            }
            catch (Exception e)
            {
                // Log exception
                Log.Exception(e);

                // Clean up entities
                Scenario.Cleanup();

                // Clear call
                base.OnCalloutNotAccepted();

                // Tell LSPDFR we failed to setup the scenario
                return false;
            }

            // AgencyCallout base class will handle the Dispatch stuff
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// Event called right if the callout is not accepted by the player
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            // AgencyCallout base class will handle the Dispatch stuff
            base.OnCalloutNotAccepted();
        }

        /// <summary>
        /// Method called every tick
        /// </summary>
        public override void Process()
        {
            base.Process();
            Scenario.Process();
        }

        /// <summary>
        /// Method called when the callout has ended
        /// </summary>
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
            switch (ActiveCall.ScenarioInfo.Name)
            {
                case "RearEndNoInjuries":
                    return new RearEndNoInjuries(this, ScenarioNode);
                default:
                    throw new Exception($"Unsupported TrafficAccident Scenario '{ActiveCall.ScenarioInfo.Name}'");
            }
        }
    }
}
