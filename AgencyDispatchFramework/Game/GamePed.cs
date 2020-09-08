using AgencyDispatchFramework.Extensions;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;
using System;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// A class that provides methods to better describe a <see cref="Rage.Ped"/>
    /// </summary>
    public class GamePed
    {
        /// <summary>
        /// Gets the <see cref="Rage.Ped"/> this object references
        /// </summary>
        public Ped Ped { get; private set; }

        /// <summary>
        /// Gets the <see cref="LSPD_First_Response.Engine.Scripting.Entities.Persona"/> for this <see cref="Rage.Ped"/>
        /// </summary>
        public Persona Persona { get; private set; }

        /// <summary>
        /// Gets the title of this <see cref="Rage.Ped"/>
        /// </summary>
        public string GenderTitle => (Persona.Gender == LSPD_First_Response.Gender.Female) ? "Ms" : "Mr";

        /// <summary>
        /// Gets or sets the <see cref="Rage.Ped"/>s demeanor
        /// </summary>
        public PedDemeanor Demeanor { get; set; } = PedDemeanor.Happy;

        /// <summary>
        /// Gets or sets whether this <see cref="Rage.Ped"/> is drunk
        /// </summary>
        public bool IsDrunk
        {
            get => Ped.GetIsDrunk();
            set => Ped.SetIsDrunk(value);
        }

        /// <summary>
        /// Gets or sets whether this <see cref="Rage.Ped"/> is high on drugs
        /// </summary>
        public bool IsHigh
        {
            get => Ped.GetIsUnderDrugInfluence();
            set => Ped.SetIsUnderDrugInfluence(value);
        }

        public GamePed(Ped ped)
        {
            Ped = ped ?? throw new ArgumentNullException(nameof(ped));
            Persona = Functions.GetPersonaForPed(ped);
        }

        public bool IsValid()
        {
            return (Ped != null && Ped.Exists() && Ped.IsValid());
        }

        public override string ToString()
        {
            return Persona.FullName;
        }
    }
}
