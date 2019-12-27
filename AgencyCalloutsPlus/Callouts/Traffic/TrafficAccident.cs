﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;

namespace AgencyCalloutsPlus.Callouts.Traffic
{
    [CalloutInfo("TrafficAccident", CalloutProbability.Never)]
    public class TrafficAccident : Callout
    {
        private Blip Blip;
        private Vehicle Vehicle;
        private bool OnScene;
        private Vector3 SpawnPoint;
        private Ped Victim;

        public override bool OnBeforeCalloutDisplayed()
        {
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
        }

        public override void End()
        {
            base.End();
        }
    }
}
