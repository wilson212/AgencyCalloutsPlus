using Rage;
using System;
using System.Drawing;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents an Officer unit that can respond to <see cref="PriorityCall"/>(s)
    /// </summary>
    public class OfficerUnit : IDisposable
    {
        /// <summary>
        /// Travel speed as Code 2 in Meters per second
        /// </summary>
        private static readonly int Code2TravelSpeed = 15;

        /// <summary>
        /// Travel speed as Code 3 in Meters per second
        /// </summary>
        private static readonly int Code3TravelSpeed = 20;

        /// <summary>
        /// Indicates whether this instance is disposed
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        /// Indicates whether this <see cref="OfficerUnit"/> is an AI
        /// ped or the <see cref="Game.LocalPlayer"/>
        /// </summary>
        public bool IsAIUnit { get; protected set; }

        /// <summary>
        /// Gets the Division-UnitType-Beat for this unit
        /// </summary>
        public string UnitString { get; protected set; }

        /// <summary>
        /// Gets the officers current <see cref="OfficerStatus"/>
        /// </summary>
        public OfficerStatus Status { get; internal set; }

        /// <summary>
        /// Gets the last <see cref="World.DateTime"/> this officer was tasked with something
        /// </summary>
        public DateTime LastStatusChange { get; protected set; }

        /// <summary>
        /// Gets the current <see cref="PriorityCall"/> if any this unit is assigned to
        /// </summary>
        public PriorityCall CurrentCall { get; private set; }

        /// <summary>
        /// Temporary
        /// </summary>
        internal DateTime NextStatusChange { get; private set; }

        /// <summary>
        /// Gets the <see cref="Vehicle"/> this officer uses as his police cruiser
        /// </summary>
        internal Vehicle PoliceCar { get; set; }

        /// <summary>
        /// Gets the officer <see cref="Ped"/>
        /// </summary>
        internal Ped Officer { get; set; }

        /// <summary>
        /// Gets the <see cref="PoliceCar"/>'s <see cref="Blip"/>
        /// </summary>
        internal Blip VehicleBlip { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private GameFiber UnitFiber { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private TaskSignal NextTask { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="OfficerUnit"/>
        /// </summary>
        /// <param name="isAiUnit"></param>
        /// <param name="unitString"></param>
        /// <param name="vehicle"></param>
        internal OfficerUnit(bool isAiUnit, string unitString, Vehicle vehicle)
        {
            IsAIUnit = isAiUnit;
            UnitString = unitString;
            PoliceCar = vehicle;
            Officer = vehicle?.Driver;
            LastStatusChange = World.DateTime;
            Status = OfficerStatus.Available;
        }

        /// <summary>
        /// Starts the Task unit fiber for this AI Unit
        /// </summary>
        internal void StartDuty()
        {
            UnitFiber = GameFiber.StartNew(delegate
            {
                while (!IsDisposed)
                {
                    switch (NextTask)
                    {
                        case TaskSignal.None:
                            break;
                        case TaskSignal.Cruise:
                            Cruise();
                            break;
                        case TaskSignal.DriveToCall:
                            DriveToCall();
                            break;
                    }

                    GameFiber.Yield();
                }
            });

        }

        ~OfficerUnit()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                UnitFiber?.Abort();

                if (VehicleBlip?.IsValid() ?? false)
                    VehicleBlip?.Delete();

                if (PoliceCar?.IsValid() ?? false)
                    PoliceCar?.Delete();

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Drives the <see cref="Officer"/> to the current <see cref="PriorityCall"/>
        /// assigned. Must be used in a <see cref="GameFiber"/> as this method is a
        /// blocking method
        /// </summary>
        private void DriveToCall()
        {
            // Debug
            Log.Debug($"OfficerUnit {UnitString} driving to call");

            // Ensure officer is in police cruiser
            EnsureInPoliceCar();

            // Repond code 3?
            var task = PoliceCar.Driver.Tasks;
            if (!CurrentCall.ScenarioInfo.RespondCode3)
            {
                PoliceCar.IsSirenOn = true;
                PoliceCar.IsSirenSilent = false;
                var loc = World.GetNextPositionOnStreet(CurrentCall.Location.Position.Around(15));
                task.DriveToPosition(loc, Code3TravelSpeed, VehicleDrivingFlags.Emergency).WaitForCompletion();
            }
            else
            {
                var loc = World.GetNextPositionOnStreet(CurrentCall.Location.Position.Around(15));
                task.DriveToPosition(loc, Code2TravelSpeed, VehicleDrivingFlags.Normal).WaitForCompletion();
                /*
                var sp = CurrentCall.Location as SpawnPoint;
                if (sp == null)
                    task.ParkVehicle(CurrentCall.Location.Position, Vehicle.Heading).WaitForCompletion();
                else
                    task.ParkVehicle(CurrentCall.Location.Position, sp.Heading).WaitForCompletion();
                */
            }

            // Debug
            Log.Debug($"OfficerUnit {UnitString} arrived on scene");

            // Sit here
            PoliceCar.IsSirenSilent = true;
            SetBlipColor(Color.Green);
            LastStatusChange = World.DateTime;
            Status = OfficerStatus.OnScene;
            GameFiber.Sleep(5000);

            // Tell dispatch we are done here
            CurrentCall.CallStatus = CallStatus.Completed;
            Dispatch.RegisterCallComplete(CurrentCall);
            CurrentCall = null;
            PoliceCar.IsSirenOn = false;
            LastStatusChange = World.DateTime;
            NextTask = TaskSignal.Cruise;
            Status = OfficerStatus.Available;

            Log.Debug($"OfficerUnit {UnitString} completed call");
            SetBlipColor(Color.White);
        }

        /// <summary>
        /// Drives the <see cref="Officer"/> around aimlessly. 
        /// Must be used in a <see cref="GameFiber"/> as this method is a
        /// blocking method
        /// </summary>
        private void Cruise()
        {
            NextTask = TaskSignal.None;
            Officer.Tasks.CruiseWithVehicle(PoliceCar, 18, VehicleDrivingFlags.Normal);
        }

        /// <summary>
        /// Ensures the Officer is in his police car. If not,
        /// the officer will be allowed 15 seconds to walk into
        /// his patrol car before being teleported in.
        /// </summary>
        private void EnsureInPoliceCar()
        {
            // If officer is out of car, get back in
            if (!Officer.IsInVehicle(PoliceCar, true))
            {
                Officer.Tasks.FollowNavigationMeshToPosition(
                    PoliceCar.GetOffsetPosition(Vector3.RelativeLeft * 2f), PoliceCar.Heading, 1.6f
                ).WaitForCompletion(6000);
                Officer.Tasks.EnterVehicle(PoliceCar, 7000, -1).WaitForCompletion();
            }
        }

        /// <summary>
        /// Assigns this officer to the specified call
        /// </summary>
        /// <param name="call"></param>
        internal void AssignToCall(PriorityCall call)
        {
            call.OfficerUnit = this;
            call.CallStatus = CallStatus.Dispatched;

            CurrentCall = call;
            Status = OfficerStatus.Dispatched;
            LastStatusChange = World.DateTime;
            SetBlipColor(Color.Red);

            // Signal our thread to do something
            NextTask = TaskSignal.DriveToCall;
        }

        /// <summary>
        /// Sets the <see cref="Blip"/> color of the <see cref="PoliceCar"/>
        /// </summary>
        /// <param name="color"></param>
        private void SetBlipColor(Color color)
        {
            if (VehicleBlip != null && VehicleBlip.IsValid())
            {
                VehicleBlip.Color = color;
            }
            else
            {
                VehicleBlip = PoliceCar.AttachBlip();
                VehicleBlip.Color = color;
            }
        }

        private enum TaskSignal
        {
            None,

            Cruise,

            DriveToCall
        }
    }
}
