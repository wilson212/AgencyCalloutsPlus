using AgencyDispatchFramework.Dispatching;
using LSPD_First_Response.Mod.Callouts;
using System;
using System.IO;
using System.Xml;

namespace AgencyDispatchFramework.Scripting.Callouts
{
    /// <summary>
    /// Provides a base for all callouts withing ADF. This base class will process
    /// all the dispatch magic related to calls.
    /// </summary>
    internal abstract class AgencyCallout : Callout
    {
        /// <summary>
        /// Stores the current <see cref="PriorityCall"/>
        /// </summary>
        protected PriorityCall ActiveCall { get; set; }

        /// <summary>
        /// Loads an xml file and returns the XML document back as an object
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        protected static XmlDocument LoadScenarioFile(params string[] paths)
        {
            // Create file path
            string path = Main.FrameworkFolderPath;
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

            throw new Exception($"[ERROR] AgencyCalloutsPlus: Scenario file does not exist: '{path}'");
        }

        /// <summary>
        /// Attempts to spawn a <see cref="CalloutScenarioInfo"/> based on probability. If no
        /// <see cref="CalloutScenarioInfo"/> can be spawned, the error is logged automatically.
        /// </summary>
        /// <returns>returns a <see cref="CalloutScenarioInfo"/> on success, or null otherwise</returns>
        internal static XmlNode LoadScenarioNode(CalloutScenarioInfo info)
        {
            // Remove name prefix
            var folderName = info.CalloutName.Replace("AgencyCallout.", "");

            // Load the CalloutMeta
            var document = LoadScenarioFile("Callouts", folderName, "CalloutMeta.xml");

            // Return the Scenario node
            return document.DocumentElement.SelectSingleNode($"Scenarios/{info.Name}");
        }

        public override bool OnBeforeCalloutDisplayed()
        {
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            // Did the callout do thier ONE AND ONLY TASK???
            if (ActiveCall == null)
                throw new ArgumentNullException(nameof(ActiveCall));

            // Tell dispatch
            Dispatch.CalloutAccepted(ActiveCall, this);
            
            // Base must be called last!
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            // Did the callout do thier ONE AND ONLY TASK???
            // If not, its not the end of the world because Dispatch is keeping watch but... 
            // still alert the author
            if (ActiveCall == null)
            {
                Log.Error("AgencyCallout.OnCalloutNotAccepted: Unable to clear active call because ActiveCall is null!");
                return;
            }

            // Tell dispatch
            Dispatch.CalloutNotAccepted(ActiveCall);

            // Base must be called last!
            base.OnCalloutNotAccepted();
        }

        public override void End()
        {
            Dispatch.RegisterCallComplete(ActiveCall);
            base.End();
        }
    }
}
