using LSPD_First_Response;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;

namespace AgencyCalloutsPlus.Mod
{
    /// <summary>
    /// A class that provides methods to better describe a <see cref="Rage.Ped"/>
    /// </summary>
    public class PedWrapper
    {
        public Ped Ped { get; set; }

        public Persona Persona { get; set; }

        public string GenderTitle => (Persona.Gender == Gender.Female) ? "Ms" : "Mr";

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
