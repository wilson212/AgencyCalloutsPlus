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
        public void PlayScannerAudioUsingPrefix(string scanner, Vector3 position)
        {
            // Pad zero
            var divString = Settings.AudioDivision.ToString();
            var beatString = Settings.AudioBeat.ToString();

            if (divString.Length == 1)
            {
                divString = divString.PadLeft(2, '0');
            }

            if (beatString.Length == 1)
            {
                beatString = beatString.PadLeft(2, '0');
            }

            var prefix = $"DISP_ATTENTION_UNIT_01 DIV_{divString} {Settings.AudioDivision} BEAT_{beatString} ";
            Functions.PlayScannerAudioUsingPosition(prefix + scanner, position);
        }
    }
}
