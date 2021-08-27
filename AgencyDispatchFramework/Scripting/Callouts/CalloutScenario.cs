using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Game;
using System;
using System.IO;
using System.Xml;

namespace AgencyDispatchFramework.Scripting.Callouts
{
    /// <summary>
    /// An object base that represents a callout scenario.
    /// </summary>
    internal abstract class CalloutScenario
    {
        /// <summary>
        /// Contains the <see cref="ExpressionParser"/> used to parse the expression strings
        /// in the "if" attributes on <see cref="XmlNode"/>s
        /// </summary>
        protected ExpressionParser Parser { get; set; }

        /// <summary>
        /// Gets the randomly selected <see cref="SelectedCircumstance"/>
        /// </summary>
        protected Circumstance SelectedCircumstance { get; set; }

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

            // Select a random Circumstance for this scenario
            if (!ScenarioInfo.GetRandomCircumstance(Parser, out Circumstance circ))
            {
                throw new Exception($"Unable to select a Circumstance for callout scenario {ScenarioInfo.Name}");
            }

            SelectedCircumstance = circ;
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
        /// Loads an xml file and returns the XML document back as a <see cref="XmlDocument"/>
        /// </summary>
        /// <param name="paths">The relative paths to the file, starting from the Frameworks callout folder</param>
        /// <returns>if the file exists, returns a <see cref="XmlDocument"/> instance. Returns null on failure</returns>
        protected static XmlDocument LoadCalloutXmlDocument(params string[] paths)
        {
            // Create full file path
            string path = Path.Combine(Main.FrameworkFolderPath, "Callouts");
            foreach (string p in paths)
            {
                path = Path.Combine(path, p);
            }

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

            return null;
        }

        /// <summary>
        /// Loads a callout file and returns the <see cref="FileStream"/>
        /// </summary>
        /// <param name="paths">The relative paths to the file, starting from the Frameworks callout folder</param>
        /// <returns>if the file exists, returns a <see cref="FileStream"/> instance. Returns null on failure</returns>
        protected static FileStream GetCalloutFile(params string[] paths)
        {
            // Create full file path
            string path = Path.Combine(Main.FrameworkFolderPath, "Callouts");
            foreach (string p in paths)
            {
                path = Path.Combine(path, p);
            }

            // Ensure file exists
            return (File.Exists(path)) ?  new FileStream(path, FileMode.Open) : null;
        }
    }
}