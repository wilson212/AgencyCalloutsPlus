using AgencyCalloutsPlus.Integration;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus
{
    public abstract class AgencyCallout : Callout
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
        /// Plays the Audio scanner with the Division Unit Beat prefix
        /// </summary>
        /// <param name="scanner"></param>
        public void PlayScannerAudioUsingPrefix(string scanner)
        {
            // Pad zero
            var divString = Settings.AudioDivision.ToString("D2");
            var beatString = Settings.AudioBeat.ToString("D2");

            var prefix = $"DISP_ATTENTION_UNIT DIV_{divString} {Settings.AudioDivision} BEAT_{beatString} ";
            Functions.PlayScannerAudioUsingPosition(prefix + scanner, CalloutPosition);
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
                ComputerPlusAPI.AssignCallToAIUnit(CalloutID);
            }
        }
    }
}
