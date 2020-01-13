using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Integration;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        protected XmlDocument LoadScenarioFile(params string[] paths)
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
        internal static CalloutScenarioInfo LoadRandomScenario(string calloutName)
        {
            // Try and fetch the SpawnGenerator
            if (Scenarios.TryGetValue(calloutName, out SpawnGenerator<CalloutScenarioInfo> spawner))
            {
                // Can we spawn a scenario?
                if (!spawner.TrySpawn(out CalloutScenarioInfo scenario))
                {
                    Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Callout '{calloutName}' does not have any Scenarios registered");
                    return null;
                }

                return scenario;
            }

            return null;
        }

        /*
        /// <summary>
        /// Calculates a range based on zone factors such as population density,
        /// and jurisdiction type of the players agency
        /// </summary>
        /// <param name="IsPriority">Is this a priority call</param>
        protected virtual Range<float> CalculateBestTravelDisatnceForCallout(bool IsPriority)
        {
            // Get player agency
            var agency = Agency.GetCurrentPlayerAgency();
            if (agency == null)
            {
                // Default of 1 mile
                return new Range<float>(200, 1610);
            }

            // Define our long distance types, with broad jurisdictions
            AgencyType[] longDistanceTypes = { AgencyType.HighwayPatrol, AgencyType.SpecialAgent };
            AgencyType[] midDistanceTypes = { AgencyType.CountySheriff, AgencyType.ParkRanger };

            // Create modifiers
            float modifier = (IsPriority) ? 800 : 0;
            if (longDistanceTypes.Contains(agency.AgencyType))
                modifier += 800;
            else if (midDistanceTypes.Contains(agency.AgencyType))
                modifier += 400;

            // Return calculated distance
            switch (agency.StaffingLevel)
            {
                // Poor funding - 1.5 mile base
                case FundingLevel.Poor: return new Range<float>(200, 2400 + modifier);

                // Fair funding - 1 mile base
                case FundingLevel.Fair: return new Range<float>(200, 1600 + modifier);

                // Good funding - 0.5 mile base
                default: return new Range<float>(200, 800 + modifier);
            }
        }*/

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
    }
}
