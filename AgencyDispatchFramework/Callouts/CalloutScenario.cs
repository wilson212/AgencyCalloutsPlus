using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using Rage;
using System;
using System.IO;
using System.Xml;

namespace AgencyDispatchFramework.Callouts
{
    /// <summary>
    /// An object base that represents a callout scenario.
    /// </summary>
    internal abstract class CalloutScenario
    {
        /// <summary>
        /// Contains the 
        /// </summary>
        internal ExpressionParser Parser { get; set; }

        /// <summary>
        /// Gets the randomly selected <see cref="FlowOutcome"/>
        /// </summary>
        protected FlowOutcome FlowOutcome { get; set; }

        /// <summary>
        /// Gets the <see cref="CalloutScenarioInfo"/> for this instance
        /// </summary>
        protected CalloutScenarioInfo ScenarioInfo { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public CalloutScenario(CalloutScenarioInfo scenarioInfo)
        {
            Parser = new ExpressionParser();
            Parser.SetParamater("Weather", GameWorld.GetWeatherSnapshot());
            Parser.SetParamater("Call", Dispatch.PlayerActiveCall);
            ScenarioInfo = scenarioInfo;

            // Select a random FlowOutcome for this scenario
            if (!ScenarioInfo.GetRandomFlowOutcome(Parser, out FlowOutcome flowOutcome))
            {
                throw new Exception($"Unable to select a FlowOutcome for callout scenario {ScenarioInfo.Name}");
            }

            FlowOutcome = flowOutcome;
        }

        /// <summary>
        /// Sets up the current CalloutScene vehicles and peds. This method must be called
        /// in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.OnCalloutAccepted()"/> method
        /// </summary>
        public abstract void Setup();

        /// <summary>
        /// Processes the current <see cref="CalloutScenario"/>. This method must be called
        /// in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.Process()"/> method
        /// on every tick
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// This method is responsible for cleaning up all of the objects in this <see cref="CalloutScenario"/>.
        /// This method must be called in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.End()"/> 
        /// method
        /// </summary>
        public abstract void Cleanup();

        /// <summary>
        /// Loads an xml file and returns the XML document back as an object
        /// </summary>
        /// <param name="paths">The path to the f</param>
        /// <returns></returns>
        protected static XmlDocument LoadFlowSequenceFile(params string[] paths)
        {
            // Create file path
            string path = Path.Combine(Main.FrameworkFolderPath, "Callouts");
            foreach (string p in paths)
                path = Path.Combine(path, p);

            // Ensure file exists
            if (File.Exists(path))
            {
                // Load XML document
                XmlDocument document = new XmlDocument();
                using (var file = new FileStream(path, FileMode.Open))
                {
                    document.Load(file);
                }

                return document;
            }

            throw new Exception($"[ERROR] AgencyCalloutsPlus: Scenario FlowSequence file does not exist: '{path}'");
        }
    }
}