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
        private static Dictionary<string, SpawnGenerator<CalloutScenario>> Scenarios { get; set; }

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
        /// Attempts to spawn a <see cref="CalloutScenario"/> based on probability. If no
        /// <see cref="CalloutScenario"/> can be spawned, the error is logged automatically.
        /// </summary>
        /// <param name="calloutName">The name of the callout in the <see cref="CalloutInfoAttribute"/></param>
        /// <returns>returns a <see cref="CalloutScenario"/> on success, or null otherwise</returns>
        internal CalloutScenario LoadRandomScenario(string calloutName)
        {
            // Try and fetch the SpawnGenerator
            if (Scenarios.TryGetValue(calloutName, out SpawnGenerator<CalloutScenario> spawner))
            {
                // Can we spawn a scenario?
                if (!spawner.TrySpawn(out CalloutScenario scene))
                {
                    Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Callout does not have any Scenarios registered '{calloutName}'");
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
                Scenarios = new Dictionary<string, SpawnGenerator<CalloutScenario>>();

            // Ensure callout is registed in dictionary
            if (!Scenarios.ContainsKey(calloutName))
                Scenarios.Add(calloutName, new SpawnGenerator<CalloutScenario>());

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
                var scene = new CalloutScenario()
                {
                    Name = n.Name,
                    Probability = probability,
                    RespondCode3 = (n.Attributes["respond"].Value.Contains("3"))
                };
                Scenarios[calloutName].Add(scene);
            }
        }

        internal static API.VehicleType GetRandomCarTypeFromScenarioNodeList(XmlNodeList nodes)
        {
            // Create a new spawn generator
            var generator = new SpawnGenerator<VehicleSpawn>();

            // Add each item
            foreach (XmlNode n in nodes)
            {
                // Ensure we have attributes
                if (n.Attributes == null)
                {
                    Game.LogTrivial(
                        $"[WARN] AgencyCalloutsPlus: Scenario VehicleTypes item has no attributes in 'CalloutMeta.xml->Sceanrios'"
                    );
                    continue;
                }

                // Try and extract type value
                if (!Enum.TryParse(n.InnerText, out API.VehicleType vehicleType))
                {
                    Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract VehicleType value in 'CalloutMeta.xml'");
                    continue;
                }

                // Try and extract probability value
                if (n.Attributes["probability"]?.Value == null || !int.TryParse(n.Attributes["probability"].Value, out int probability))
                {
                    Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract VehicleType probability value in 'CalloutMeta.xml'");
                    continue;
                }

                // Add vehicle type
                generator.Add(new VehicleSpawn() { Probability = probability, Type = vehicleType });
            }

            return generator.Spawn().Type;
        }

        private class VehicleSpawn : ISpawnable
        {
            public int Probability { get; set; }

            public API.VehicleType Type { get; set; }
        }
    }
}
