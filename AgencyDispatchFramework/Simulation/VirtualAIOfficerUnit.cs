using AgencyDispatchFramework.Dispatching;
using Rage;
using System;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents an <see cref="OfficerUnit"/> that is simulated virtually in memory
    /// and does not actually exist in the <see cref="Rage.World"/>
    /// </summary>
    public class VirtualAIOfficerUnit : OfficerUnit
    {
        /// <summary>
        /// Indicates whether this is an AI player
        /// </summary>
        public override bool IsAIUnit => true;

        /// <summary>
        /// Creates a new instance of <see cref="VirtualAIOfficerUnit"/>
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="unitString"></param>
        public VirtualAIOfficerUnit(Agency agency, int division, char unit, int beat) : base(agency, division, unit, beat)
        {

        }

        /// <summary>
        /// Method to be called on every tick of the <see cref="Dispatch.AISimulationFiber"/>
        /// </summary>
        /// <param name="gameTime">The current game <see cref="World.DateTime"/></param>
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
            if (CurrentCall.PrimaryOfficer == this)
            {
                Log.Debug($"OfficerUnit {CallSign} of {Agency.FriendlyName} completed call '{CurrentCall.ScenarioInfo.Name}' with flag: {flag}");
                Dispatch.RegisterCallComplete(CurrentCall);
            }

            // Call base
            base.CompleteCall(flag);
            Assignment = null;

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
            Log.Debug($"OfficerUnit {CallSign} of {Agency.FriendlyName} responding to call '{CurrentCall.ScenarioInfo.Name}'");
            int mins = 30;

            // Repond code 3?
            if (CurrentCall.ScenarioInfo.ResponseCode == ResponseCode.Code3)
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
            Log.Debug($"OfficerUnit {CallSign} of {Agency.FriendlyName} arrived on scene");

            // Telll dispatch we are on scene
            Dispatch.RegisterOnScene(this, CurrentCall);

            // Set updated location
            Position = CurrentCall.Location.Position;

            // Set status
            Status = OfficerStatus.OnScene;
            LastStatusChange = World.DateTime;

            // Determine how long we will be on scene
            var random = new CryptoRandom();
            var callTime = random.Next(CurrentCall.ScenarioInfo.SimulationTime);
            NextStatusChange = LastStatusChange.AddMinutes(callTime);
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

        internal override void AssignToCallWithRandomCompletion(PriorityCall call)
        {
            // Assign ourselves
            base.AssignToCall(call, true);

            // Determine if we are on scene, and for how long we have been
            var random = new CryptoRandom();
            var onScene = random.Next(1, 100) > 20;

            if (onScene)
            {
                // Random time to completion
                var callTime = random.Next(CurrentCall.ScenarioInfo.SimulationTime);
                double percent = random.Next(10, 80);
                var timeToComplete = Convert.ToInt32(callTime / percent);
                var timeOnScene = callTime - timeToComplete;

                // Set updated location
                Position = CurrentCall.Location.Position;

                // Set status
                call.CallStatus = CallStatus.OnScene;
                Status = OfficerStatus.OnScene;
                LastStatusChange = World.DateTime.AddMinutes(-timeOnScene);
                NextStatusChange = LastStatusChange.AddMinutes(timeToComplete);
            }
            else
            {
                int arrivalTime = random.Next(5, 30);

                // 30 in game minutes
                Status = OfficerStatus.Dispatched;
                LastStatusChange = World.DateTime;
                NextStatusChange = LastStatusChange.AddMinutes(arrivalTime);
            }
        }
    }
}
