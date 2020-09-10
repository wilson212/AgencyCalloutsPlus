using Rage;
using System;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Represents an Officer unit that can respond to <see cref="PriorityCall"/>(s)
    /// </summary>
    public abstract class OfficerUnit : IDisposable
    {
        /// <summary>
        /// Indicates whether this instance is disposed
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        /// Indicates whether this <see cref="OfficerUnit"/> is an AI
        /// ped or the <see cref="Rage.Game.LocalPlayer"/>
        /// </summary>
        public abstract bool IsAIUnit { get; }

        /// <summary>
        /// Gets the Division-UnitType-Beat for this unit
        /// </summary>
        public string CallSign { get; internal set; }

        /// <summary>
        /// Gets the officers current <see cref="OfficerStatus"/>
        /// </summary>
        public OfficerStatus Status { get; internal set; }

        /// <summary>
        /// Gets the last <see cref="Game.DateTime"/> this officer was tasked with something
        /// </summary>
        public DateTime LastStatusChange { get; internal set; }

        /// <summary>
        /// Gets the current <see cref="PriorityCall"/> if any this unit is assigned to
        /// </summary>
        public PriorityCall CurrentCall { get; private set; }

        /// <summary>
        /// Temporary
        /// </summary>
        internal DateTime NextStatusChange { get; set; }

        /// <summary>
        /// Contains the Shift hours for this unit
        /// </summary>
        internal TimeSpan ShiftHours { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="OfficerUnit"/> for an AI unit
        /// </summary>
        /// <param name="unitString"></param>
        internal OfficerUnit(string unitString)
        {
            CallSign = unitString;
        }

        /// <summary>
        /// Gets the position of this <see cref="OfficerUnit"/>
        /// </summary>
        public abstract Vector3 GetPosition();

        /// <summary>
        /// Starts the Task unit fiber for this AI Unit
        /// </summary>
        internal virtual void StartDuty()
        {
            LastStatusChange = World.DateTime;
            Status = OfficerStatus.Available;
        }

        /// <summary>
        /// Method called every tick on the AI Fiber Thread
        /// </summary>
        /// <param name="gameTime"></param>
        internal virtual void OnTick(DateTime gameTime)
        {

        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~OfficerUnit()
        {
            Dispose();
        }

        /// <summary>
        /// Our Dispose method
        /// </summary>
        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
            }
        }

        /// <summary>
        /// Assigns this officer to the specified call
        /// </summary>
        /// <param name="call"></param>
        internal virtual void AssignToCall(PriorityCall call, bool forcePrimary = false)
        {
            // Did we get called on for a more important assignment?
            if (CurrentCall != null)
            {
                // Put this here to remove a potential null problem later
                var currentCall = CurrentCall;

                // Is this unit the primary on a lesser important call?
                if (currentCall.PrimaryOfficer == this && currentCall.Priority > 2)
                {
                    if (currentCall.CallStatus == CallStatus.OnScene)
                    {
                        // @todo : If more than 50% complete, close call
                        var flag = (call.Priority < currentCall.Priority) ? CallCloseFlag.Emergency : CallCloseFlag.Forced;
                        CompleteCall(flag);
                    }
                }

                // Back out of call
                currentCall.RemoveOfficer(this);
            }

            // Set flags
            call.AssignOfficer(this, forcePrimary);
            call.CallStatus = CallStatus.Dispatched;

            CurrentCall = call;
            Status = OfficerStatus.Dispatched;
            LastStatusChange = World.DateTime;
        }

        /// <summary>
        /// Clears the current call, DOES NOT SIGNAL DISPATCH
        /// </summary>
        /// <remarks>
        /// This method is called by <see cref="Dispatch"/> for the Player ONLY.
        /// AI units call it themselves
        /// </remarks>
        internal virtual void CompleteCall(CallCloseFlag flag)
        {
            // Clear last call
            CurrentCall = null;
            LastStatusChange = World.DateTime;
        }

        internal abstract void AssignToCallWithRandomCompletion(PriorityCall call);
    }
}
