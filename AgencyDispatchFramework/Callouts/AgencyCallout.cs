using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Integration;
using LSPD_First_Response.Mod.Callouts;
using System;
using System.IO;
using System.Xml;

namespace AgencyDispatchFramework.Callouts
{
    internal abstract class AgencyCallout : Callout
    {
        /// <summary>
        /// Callout GUID for ComputerPlus
        /// </summary>
        public Guid CalloutID { get; protected set; } = Guid.Empty;

        /// <summary>
        /// Indicates whether Computer+ is running
        /// </summary>
        public bool ComputerPlusRunning => ComputerPlusAPI.IsRunning;

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
            string path = Main.ThisPluginFolderPath;
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

            // Update computer plus
            if (ComputerPlusRunning)
            {
                ComputerPlusAPI.SetCalloutStatusToUnitResponding(CalloutID);
                Rage.Game.DisplayHelp("Further details about this call can be checked using ~b~Computer+.");
            }
            
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();

            // Did the callout do thier ONE AND ONLY TASK???
            if (ActiveCall == null)
                throw new ArgumentNullException(nameof(ActiveCall));

            // Tell dispatch
            Dispatch.CalloutNotAccepted(ActiveCall);

            /*
            // Update computer plus!
            if (ComputerPlusRunning)
            {
                Functions.PlayScannerAudio("OTHER_UNIT_TAKING_CALL");
                ComputerPlusAPI.AssignCallToAIUnit(CalloutID);
            }
            */
        }
    }
}
