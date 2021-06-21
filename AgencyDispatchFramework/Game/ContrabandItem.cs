using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// Represents a contraband item on a <see cref="Rage.Ped"/> or in a <see cref="Rage.Vehicle"/>
    /// </summary>
    /// <remarks>
    /// LSPDFR's PedContraband class is readonly, so we use this instead
    /// </remarks>
    public class ContrabandItem
    {
        /// <summary>
        /// The name of the item when shown on the search results window
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of contraband, which determines the color of the item name
        /// when displayed on the search results window
        /// </summary>
        public ContrabandType Type { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ContrabandItem"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public ContrabandItem(string name, ContrabandType type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Enables casting to a <see cref="ContrabandItem"/>
        /// </summary>
        /// <param name="p">The <see cref="PedContraband"/> instance</param>
        public static implicit operator ContrabandItem(PedContraband p)
        {
            return new ContrabandItem(p.Name, p.Type);
        }
    }
}
