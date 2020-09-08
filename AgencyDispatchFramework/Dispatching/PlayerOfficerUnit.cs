using Rage;
using System;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Represents the <see cref="Rage.Game.LocalPlayer"/> as an <see cref="OfficerUnit"/>
    /// </summary>
    public class PlayerOfficerUnit : OfficerUnit
    {
        /// <summary>
        /// Indicates whether this is an AI player
        /// </summary>
        public override bool IsAIUnit => false;

        /// <summary>
        /// Gets the officer <see cref="Ped"/>
        /// </summary>
        internal Ped Officer { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="OfficerUnit"/> for the player
        /// </summary>
        /// <param name="player"></param>
        internal PlayerOfficerUnit(Player player, string unitString) : base(unitString)
        {
            Officer = player.Character ?? throw new ArgumentNullException("player");
            LastStatusChange = World.DateTime;
            Status = OfficerStatus.Available;
        }

        /// <summary>
        /// Gets the position of this <see cref="OfficerUnit"/>
        /// </summary>
        /// <returns></returns>
        public override Vector3 GetPosition()
        {
            return Officer.Position;
        }
    }
}
