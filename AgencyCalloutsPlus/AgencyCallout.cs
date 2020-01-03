using AgencyCalloutsPlus.Integration;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace AgencyCalloutsPlus
{
    public abstract class AgencyCallout : Callout
    {
        private static Dictionary<string, SpawnGenerator<CalloutScenarioInfo>> Scenarios { get; set; }

        /// <summary>
        /// Callout GUID for ComputerPlus
        /// </summary>
        public Guid CalloutID { get; protected set; } = Guid.Empty;

        /// <summary>
        /// Indicates whether Computer+ is running
        /// </summary>
        public bool ComputerPlusRunning => ComputerPlusAPI.IsRunning;

        /// <summary>
        /// Plays the Audio scanner with the Division Unit Beat prefix
        /// </summary>
        /// <param name="scanner"></param>
        public void PlayScannerAudioUsingPrefix(string scanner)
        {
            // Pad zero
            var divString = Settings.AudioDivision.ToString("D2");
            var beatString = Settings.AudioBeat.ToString("D2");

            var prefix = $"DISP_ATTENTION_UNIT DIV_{divString} {Settings.AudioUnitType} BEAT_{beatString} ";
            Functions.PlayScannerAudioUsingPosition(String.Concat(prefix, scanner), CalloutPosition);
        }

        /// <summary>
        /// Loads an xml file and returns the XML document back as an object
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public XmlDocument LoadScenarioFile(params string[] paths)
        {
            // Create file path
            string path = Main.LSPDFRPluginPath;
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
        /// <param name="calloutName">The name of the callout in the <see cref="CalloutInfoAttribute"/></param>
        /// <returns>returns a <see cref="CalloutScenarioInfo"/> on success, or null otherwise</returns>
        internal CalloutScenarioInfo LoadRandomScenario(string calloutName)
        {
            // Try and fetch the SpawnGenerator
            if (Scenarios.TryGetValue(calloutName, out SpawnGenerator<CalloutScenarioInfo> spawner))
            {
                // Can we spawn a scenario?
                if (!spawner.TrySpawn(out CalloutScenarioInfo scene))
                {
                    Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Callout '{calloutName}' does not have any Scenarios registered");
                    return null;
                }

                return scene;
            }

            return null;
        }

        public override bool OnBeforeCalloutDisplayed()
        {
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            if (ComputerPlusRunning)
            {
                ComputerPlusAPI.SetCalloutStatusToUnitResponding(CalloutID);
                Game.DisplayHelp("Further details about this call can be checked using ~b~Computer+.");
            }

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();

            // Update computer plus!
            if (ComputerPlusRunning)
            {
                Functions.PlayScannerAudio("OTHER_UNIT_TAKING_CALL");
                ComputerPlusAPI.AssignCallToAIUnit(CalloutID);
            }
        }

        /// <summary>
        /// Registers and caches all the scenarios for a callout using the CalloutMeta.xml
        /// </summary>
        /// <param name="calloutName"></param>
        /// <param name="doc"></param>
        internal static void CacheScenarios(string calloutName, XmlDocument doc)
        {
            // Ensure dictionary is created
            if (Scenarios == null)
                Scenarios = new Dictionary<string, SpawnGenerator<CalloutScenarioInfo>>();

            // Ensure callout is registed in dictionary
            if (!Scenarios.ContainsKey(calloutName))
                Scenarios.Add(calloutName, new SpawnGenerator<CalloutScenarioInfo>());

            // Process the XML scenarios
            foreach (XmlNode n in doc.DocumentElement.SelectSingleNode("Scenarios").ChildNodes)
            {
                // Ensure we have attributes
                if (n.Attributes == null)
                {
                    Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Scenario item has no attributes in '{calloutName}->Scenarios->{n.Name}'");
                    continue;
                }

                // Try and extract probability value
                if (n.Attributes["probability"]?.Value == null || !int.TryParse(n.Attributes["probability"].Value, out int probability))
                {
                    Game.LogTrivial(
                        $"[WARN] AgencyCalloutsPlus: Unable to extract scenario probability value for '{calloutName}->Scenarios->{n.Name}'"
                    );
                    continue;
                }

                // Try and extract Code value
                if (n.Attributes["respond"]?.Value == null)
                {
                    Game.LogTrivial(
                        $"[WARN] AgencyCalloutsPlus: Unable to extract scenario respond value for '{calloutName}->Scenarios->{n.Name}'"
                    );
                    continue;
                }


                // Create scenario node
                var scene = new CalloutScenarioInfo()
                {
                    Name = n.Name,
                    Probability = probability,
                    RespondCode3 = (n.Attributes["respond"].Value.Contains("3"))
                };
                Scenarios[calloutName].Add(scene);
            }
        }
    }
}
