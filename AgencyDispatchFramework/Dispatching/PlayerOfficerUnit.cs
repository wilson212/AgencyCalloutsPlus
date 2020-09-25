﻿using Rage;
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
        internal Player Officer { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="OfficerUnit"/> for the player
        /// </summary>
        /// <param name="player"></param>
        internal PlayerOfficerUnit(Player player) : base(Settings.AudioDivision, Settings.AudioUnitType[0], Settings.AudioBeat)
        {
            Officer = player ?? throw new ArgumentNullException("player");
            LastStatusChange = World.DateTime;
            Status = OfficerStatus.Available;
        }

        internal override void AssignToCall(PriorityCall call, bool forcePrimary = false)
        {
            // === DO NOT CALL BASE === //

            // Set flags
            call.AssignOfficer(this, forcePrimary);

            // Is this call already dispatched?
            if (call.PrimaryOfficer == this)
            {
                call.CallStatus = CallStatus.Assigned;
            }

            CurrentCall = call;
            Status = OfficerStatus.Dispatched;
            LastStatusChange = World.DateTime;
        }

        /// <summary>
        /// Gets the position of this <see cref="OfficerUnit"/>
        /// </summary>
        /// <returns></returns>
        public override Vector3 GetPosition()
        {
            return Officer.Character.Position;
        }

        internal override void AssignToCallWithRandomCompletion(PriorityCall call)
        {
            
        }
    }
}
