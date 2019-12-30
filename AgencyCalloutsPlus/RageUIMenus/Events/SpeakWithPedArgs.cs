using Rage;
using System;

namespace AgencyCalloutsPlus.RageUIMenus.Events
{
    public class SpeakWithPedArgs : EventArgs
    {
        public Ped Ped { get; set; }

        public SpeakWithPedArgs(Ped ped)
        {
            Ped = ped;
        }
    }
}
