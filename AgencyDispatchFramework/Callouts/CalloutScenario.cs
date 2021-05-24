using AgencyDispatchFramework.Conversation;
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
        /// Constructor
        /// </summary>
        public CalloutScenario(XmlNode scenarioNode)
        {
            Parser = new ExpressionParser();
            Parser.SetParamater("Weather", new WeatherInfo());
            Parser.SetParamater("Call", Dispatch.PlayerActiveCall);

            // Select a random FlowOutcome for this scenario
            FlowOutcome = GetRandomFlowOutcome(scenarioNode);
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

        /// <summary>
        /// Fethes a random <see cref="VehicleClass"/> using the probabilites set in the
        /// CalloutMeta.xml
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        protected VehicleClass GetRandomVehicleType(XmlNodeList nodes)
        {
            // Create a new spawn generator
            var generator = new ProbabilityGenerator<VehicleSpawn>();

            // Add each item
            foreach (XmlNode n in nodes)
            {
                // Ensure we have attributes
                if (n.Attributes == null)
                {
                    Log.Warning($"Scenario VehicleTypes item has no attributes in 'CalloutMeta.xml->Sceanrios'");
                    continue;
                }

                // Try and extract type value
                if (!Enum.TryParse(n.InnerText, out VehicleClass vehicleType))
                {
                    Log.Warning($"Unable to extract VehicleType value in 'CalloutMeta.xml'");
                    continue;
                }

                // Try and extract probability value
                if (n.Attributes["probability"]?.Value == null || !int.TryParse(n.Attributes["probability"].Value, out int probability))
                {
                    Log.Warning($"Unable to extract VehicleType probability value in 'CalloutMeta.xml'");
                    continue;
                }

                // Add vehicle type
                generator.Add(new VehicleSpawn() { Probability = probability, Type = vehicleType });
            }

            return generator.Spawn().Type;
        }

        /// <summary>
        /// Fethes a random <see cref="VehicleClass"/> using the probabilites set in the
        /// CalloutMeta.xml
        /// </summary>
        /// <param name="nodes">An <see cref="XmlNodeList"/> containing the "FlowOutcome" nodes</param>
        /// <returns></returns>
        protected FlowOutcome GetRandomFlowOutcome(XmlNode scenarioNode)
        {
            // We must have a scenario XmlNode
            if (scenarioNode == null)
            {
                throw new ArgumentNullException(nameof(scenarioNode));
            }

            // Create a new spawn generator
            var generator = new ProbabilityGenerator<FlowOutcome>();

            // Fetch each FlowOutcome item
            var nodes = scenarioNode.SelectSingleNode("FlowSequence")?.SelectNodes("FlowOutcome");

            // Evaluate each flow outcome, and add it to the probability generator
            foreach (XmlNode n in nodes)
            {
                // Ensure we have attributes
                if (n.Attributes == null)
                {
                    Log.Warning($"Scenario FlowOutcome item has no attributes in 'CalloutMeta.xml->FlowSequence'");
                    continue;
                }

                // Try and extract type value
                if (n.Attributes["id"]?.Value == null)
                {
                    Log.Warning($"Unable to extract the 'id' attribute value in 'CalloutMeta.xml->FlowSequence'");
                    continue;
                }

                // Try and extract probability value
                if (n.Attributes["probability"]?.Value == null || !int.TryParse(n.Attributes["probability"].Value, out int probability))
                {
                    Log.Warning($"Unable to extract VehicleType probability value in 'CalloutMeta.xml'");
                    continue;
                }

                // See if we have an IF statement, and if we do have a condition statement,
                // and evaluate the condition. The FlowOutcome is added to the list if
                // the condition evaluates to true
                if (!String.IsNullOrEmpty(n.Attributes["if"]?.Value))
                {
                    // Do we have a parser?
                    if (Parser == null)
                    {
                        string id = n.Attributes["id"].Value;
                        Log.Warning($"FlowOutcome with ID '{id}' has 'if' statement in 'CalloutMeta.xml', but no ExpressionParser is defined... Skipping");
                        continue;
                    }

                    // evaluate the expression. This will return false if the return value is not a bool
                    if (!Parser.Evaluate<bool>(n.Attributes["if"].Value))
                    {
                        // If we failed to evaluate to true, skip this FlowOutcome
                        continue;
                    }
                }

                // Add vehicle type
                generator.Add(new FlowOutcome() { Probability = probability, Id = n.Attributes["id"].Value });
            }

            return generator.Spawn();
        }

        private class VehicleSpawn : ISpawnable
        {
            public int Probability { get; set; }

            public VehicleClass Type { get; set; }
        }
    }
}