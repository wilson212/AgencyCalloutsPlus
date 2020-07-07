using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Mod
{
    public class PedWrapper
    {
        public Ped Ped { get; set; }

        public Persona Persona { get; set; }

        public PedWrapper(Ped ped)
        {
            Ped = ped;
            Persona = Functions.GetPersonaForPed(ped);
        }

        public override string ToString()
        {
            return Persona.FullName;
        }
    }
}
