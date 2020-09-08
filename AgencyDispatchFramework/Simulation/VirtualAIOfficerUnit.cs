using AgencyDispatchFramework.Dispatching;
using Rage;
using System;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents an <see cref="OfficerUnit"/> that is simulated virtually in memory
    /// and does not actually exist in the <see cref="Game"/>
    /// </summary>
    public class VirtualAIOfficerUnit : OfficerUnit
    {
        /// <summary>
        /// Indicates whether this is an AI player
        /// </summary>
        public override bool IsAIUnit => true;

        /// <summary>
        /// Gets the position of this <see cref="OfficerUnit"/>
        /// </summary>
        protected Vector3 Position { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="VirtualAIOfficerUnit"/>
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="unitString"></param>
        public VirtualAIOfficerUnit(Vector3 startPosition, string unitString) : base(unitString)
        {
            Position = startPosition;
        }

        internal override void OnTick(DateTime gameTime)
        {
            // Time for a new task?
            if (gameTime > NextStatusChange && Status != OfficerStatus.Available)
            {
                switch (Status)
                {
                    case OfficerStatus.OnScene:
                        CompleteCall(CallCloseFlag.Completed);
                        break;
                    case OfficerStatus.Dispatched:
                        OnScene();
                        break;
                    case OfficerStatus.MealBreak:
                        Cruise();
                        break;
                }
            }
        }

        /// <summary>
        /// Assigns this officer to the specified call
        /// </summary>
        /// <param name="call"></param>
        internal override void AssignToCall(PriorityCall call, bool forcePrimary = false)
        {
            // Call base first
            base.AssignToCall(call, forcePrimary);

            // Signal our thread to do something
            DriveToCall();
        }

        /// <summary>
        /// Clears the current call, DOES NOT SIGNAL DISPATCH
        /// </summary>
        /// <remarks>
        /// This method is called by <see cref="Dispatch"/> for the Player ONLY.
        /// AI units call it themselves
        /// </remarks>
        internal override void CompleteCall(CallCloseFlag flag)
        {
            // Tell dispatch we are done here
            Dispatch.RegisterCallComplete(CurrentCall);
            Log.Debug($"OfficerUnit {CallSign} completed call with flag: {flag}");

            // Call base
            base.CompleteCall(flag);

            // Next task
            if (flag == CallCloseFlag.Completed)
            {
                TakeBreak();
            }
            else
            {
                Cruise();
            }
        }

        /// <summary>
        /// Drives the <see cref="Officer"/> to the current <see cref="PriorityCall"/>
        /// assigned.
        /// </summary>
        private void DriveToCall()
        {
            // Close this task
            Log.Debug($"OfficerUnit {CallSign} driving to call");
            int mins = 30;

            // Repond code 3?
            if (CurrentCall.ScenarioInfo.ResponseCode == 3)
            {
                // Calculate drive times
            }
            else
            {
                // Calculate drive times
            }

            // 30 in game minutes
            Status = OfficerStatus.Dispatched;
            LastStatusChange = World.DateTime;
            NextStatusChange = LastStatusChange.AddMinutes(mins);
        }

        /// <summary>
        /// Flags that the AI officer is on scene
        /// </summary>
        private void OnScene()
        {
            // Debug
            Log.Debug($"OfficerUnit {CallSign} arrived on scene");

            // Telll dispatch we are on scene
            Dispatch.RegisterOnScene(this, CurrentCall);

            // Set updated location
            Position = CurrentCall.Location.Position;

            // 30 in game minutes
            Status = OfficerStatus.OnScene;
            LastStatusChange = World.DateTime;
            NextStatusChange = LastStatusChange.AddMinutes(120);
        }

        /// <summary>
        /// Drives the <see cref="Officer"/> around aimlessly. 
        /// </summary>
        private void Cruise()
        {
            // Set Status
            LastStatusChange = World.DateTime;
            Status = OfficerStatus.Available;
        }

        /// <summary>
        /// Makes the <see cref="OfficerUnit"/> take a 30 minute break
        /// </summary>
        private void TakeBreak()
        {
            // 30 in game minutes
            Status = OfficerStatus.MealBreak;
            LastStatusChange = World.DateTime;
            NextStatusChange = LastStatusChange.AddMinutes(30);
        }

        /// <summary>
        /// Gets the last known position of this <see cref="OfficerUnit"/>
        /// </summary>
        /// <returns></returns>
        public override Vector3 GetPosition()
        {
            return Position;
        }
    }
}
