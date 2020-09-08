using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using Rage;
using System;
using System.Drawing;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents an <see cref="OfficerUnit"/> that is simulated exists in the <see cref="Game"/>
    /// as a <see cref="Ped"/> with a <see cref="Vehicle"/>
    /// </summary>
    public class PersistentAIOfficerUnit : OfficerUnit
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
        /// Indicates whether this is an AI player
        /// </summary>
        public override bool IsAIUnit => true;

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
        /// The <see cref="GameFiber"/> that runs the logic for this AI unit
        /// </summary>
        private GameFiber AILogicFiber { get; set; }

        /// <summary>
        /// Gets the next task for this AI unit on <see cref="AILogicFiber"/> ticks
        /// </summary>
        private TaskSignal NextTask { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="OfficerUnit"/> for an AI unit
        /// </summary>
        /// <param name="isAiUnit"></param>
        /// <param name="unitString"></param>
        /// <param name="vehicle"></param>
        internal PersistentAIOfficerUnit(Ped officer, Vehicle vehicle, string unitString) : base(unitString)
        {
            PoliceCar = vehicle;
            Officer = officer ?? throw new ArgumentNullException("officer");
            NextTask = TaskSignal.Cruise;
        }

        /// <summary>
        /// Starts the Task unit fiber for this AI Unit
        /// </summary>
        internal override void StartDuty()
        {
            // Call base
            base.StartDuty();

            // Create AI thread
            AILogicFiber = GameFiber.ExecuteNewWhile(delegate
            {
                // Check first if officer has died
                if (Officer.IsDead)
                {
                    Dispatch.RegisterDeadOfficer(this);
                }
                else
                {
                    // Do we have a new task?
                    switch (NextTask)
                    {
                        case TaskSignal.None:
                            break;
                        case TaskSignal.TakeABreak:
                            TakeBreak();
                            break;
                        case TaskSignal.Cruise:
                            Cruise();
                            break;
                        case TaskSignal.DriveToCall:
                            DriveToCall();
                            break;
                        case TaskSignal.DoScene:
                            break;
                    }
                }
            }, $"AgencyCallouts+ Unit {CallSign} AI Thread", () => !IsDisposed);

            // Set blip
            SetBlipColor(OfficerStatusColor.Available);
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
            NextTask = TaskSignal.DriveToCall;
        }

        public override void Dispose()
        {
            // Now Dispose
            if (!IsDisposed)
            {
                // ALWAYS CALL BASE FIRST
                base.Dispose();

                // Close fiber
                AILogicFiber?.Abort();

                if (VehicleBlip?.IsValid() ?? false)
                    VehicleBlip?.Delete();

                if (PoliceCar?.IsValid() ?? false)
                    PoliceCar?.Delete();
            }
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
            if (flag == CallCloseFlag.Completed)
            {
                Status = OfficerStatus.MealBreak;
                NextTask = TaskSignal.TakeABreak;
            }
            else
            {
                Status = OfficerStatus.Available;
                NextTask = TaskSignal.Cruise;
                SetBlipColor(OfficerStatusColor.Available);
            }

            // Ensure siren is off
            PoliceCar.IsSirenOn = false;

            // Tell dispatch we are done here
            Dispatch.RegisterCallComplete(CurrentCall);
            Log.Debug($"OfficerUnit {CallSign} completed call with flag: {flag}");

            // Call base
            base.CompleteCall(flag);
        }

        /// <summary>
        /// Gets the position of this <see cref="OfficerUnit"/>
        /// </summary>
        /// <returns></returns>
        public override Vector3 GetPosition()
        {
            return Officer.Position;
        }

        /// <summary>
        /// Ensures the Officer is in his police car. If not,
        /// the officer will be allowed 15 seconds to walk into
        /// his patrol car before being teleported in.
        /// </summary>
        private void EnsureInPoliceCar()
        {
            // Is officer dead?
            if (Officer.IsDead) return;

            // If police car doesnt exist anymore...
            SanityCheck();

            // If officer is out of car, get back in
            if (!Officer.IsInVehicle(PoliceCar, true))
            {
                // Is officer visible? If so, animate
                if (Officer.IsVisible)
                {
                    // Exit your current vehicle DUDE
                    if (Officer.IsInAnyVehicle(true))
                    {
                        Officer.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                    }

                    Officer.Tasks.FollowNavigationMeshToPosition(
                        PoliceCar.GetOffsetPosition(Vector3.RelativeLeft * 2f), PoliceCar.Heading, 1.6f
                    ).WaitForCompletion(6000);
                    Officer.Tasks.EnterVehicle(PoliceCar, 7000, -1).WaitForCompletion();
                }
                else
                {
                    Officer.WarpIntoVehicle(PoliceCar, -1);
                }
            }
        }

        /// <summary>
        /// Sets the <see cref="Blip"/> color of the <see cref="PoliceCar"/>
        /// </summary>
        /// <param name="color"></param>
        private void SetBlipColor(Color color)
        {
            // Always do this
            SanityCheck();

            // Current blip is good?
            if (VehicleBlip.Exists())
            {
                VehicleBlip.Color = color;
            }
            else
            {
                VehicleBlip = PoliceCar.AttachBlip();
                VehicleBlip.Color = color;
                VehicleBlip.Sprite = BlipSprite.PolicePatrol;
            }
        }

        /// <summary>
        /// Sometimes, even though the PED and Vehicle is set to persitent,
        /// the game disposes of them. This method ensures they exist
        /// </summary>
        private void SanityCheck()
        {
            if (Officer.IsDead) return;
            bool checkAgain = false;

            // Ensure Police vehicle is good
            if (!PoliceCar.Exists())
            {
                if (!Officer.Exists())
                {
                    checkAgain = true;
                    goto OfficerCheck;
                }
                else
                {
                    // Log
                    Log.Debug($"PoliceCar is invalid for unit {CallSign}... Creating a new one");

                    // Determine spawn point
                    var oldCar = PoliceCar;
                    var location = World.GetNextPositionOnStreet(Officer.Position.Around(25));
                    var sp = new SpawnPoint(location);

                    // Create car
                    PoliceCar = Dispatch.ActiveAgency.GetRandomPoliceVehicle(PatrolType.Marked, sp);
                    PoliceCar.Model.LoadAndWait();
                    PoliceCar.MakePersistent();

                    // Set blip color
                    SetBlipColor(VehicleBlip?.Color ?? Color.White);

                    // Warp into new car
                    Officer.WarpIntoVehicle(PoliceCar, -1);

                    // Dismiss old car
                    oldCar.Dismiss();

                    // Done here
                    return;
                }
            }

            // Ensure officer is good
            OfficerCheck:
            {
                // New officer
                if (!Officer.Exists())
                {
                    // Log
                    Log.Debug($"Officer is invalid for unit {CallSign}... Creating a new one");

                    // Tell the old Ped to piss off
                    Officer.Dismiss();

                    // Create new ped
                    Officer = PoliceCar.CreateRandomDriver();
                    Officer.MakePersistent();
                }

                // Another check?
                if (checkAgain) SanityCheck();
            }
        }

        /// <summary>
        /// Sets the position of the <see cref="PoliceCar"/>, ensuring validness
        /// </summary>
        /// <param name="position"></param>
        /// <param name="heading"></param>
        private void SetPoliceCarPosition(Vector3 position, float heading)
        {
            // Ensure is valid
            SanityCheck();

            // Set Position
            PoliceCar.Position = position;
            PoliceCar.Heading = heading;
        }

        /// <summary>
        /// Drives the <see cref="Officer"/> to the current <see cref="PriorityCall"/>
        /// assigned. Must be used in a <see cref="GameFiber"/> as this method is a
        /// blocking method
        /// </summary>
        private void DriveToCall()
        {
            // Close this task
            NextTask = TaskSignal.None;
            Log.Debug($"OfficerUnit {CallSign} driving to call");

            // Ensure officer is in police cruiser
            EnsureInPoliceCar();

            // Assign vars
            var task = PoliceCar.Driver.Tasks;
            Task drivingTask = default(Task);
            var sp = CurrentCall.Location as SpawnPoint;
            bool parkingNicely = (sp != null);
            bool isParking = false;

            // Repond code 3?
            if (CurrentCall.ScenarioInfo.ResponseCode == 3)
            {
                // Turn on sirens
                SetBlipColor(OfficerStatusColor.DispatchedCode3);
                PoliceCar.IsSirenOn = true;
                PoliceCar.IsSirenSilent = false;

                // Find close location
                parkingNicely = false;
                var loc = World.GetNextPositionOnStreet(CurrentCall.Location.Position.Around(15));
                drivingTask = task.DriveToPosition(loc, Code3TravelSpeed, VehicleDrivingFlags.Emergency);
            }
            else
            {
                // Find close location
                SetBlipColor(OfficerStatusColor.DispatchedCode2);
                var loc = World.GetNextPositionOnStreet(CurrentCall.Location.Position.Around(15));
                drivingTask = task.DriveToPosition(loc, Code2TravelSpeed, VehicleDrivingFlags.Normal);
            }

            // Run checks while officer is enroute
            while (drivingTask.IsActive)
            {
                // Check for assignment changes
                if (NextTask != TaskSignal.None)
                    return; // Return

                // Are we still alive?
                if (Officer.IsDead)
                    return;

                // Distance check
                /*
                if (parkingNicely && !isParking)
                {
                    if (Officer.Position.TravelDistanceTo(CurrentCall.Location.Position) < 75)
                    {
                        task.Clear();
                        isParking = true;
                        drivingTask = task.ParkVehicle(CurrentCall.Location.Position, sp.Heading);
                    }
                }
                */

                // Allow other threads to do something
                GameFiber.Yield();
            }

            // Teleport car to parking spot because AI sucks at parking
            SetPoliceCarPosition(CurrentCall.Location.Position, sp.Heading);

            // Signal
            NextTask = TaskSignal.DoScene;
        }

        /// <summary>
        /// Processes the officer on scene
        /// </summary>
        private void ProcessScene()
        {
            // Debug
            var span = World.DateTime - LastStatusChange;
            var mins = Math.Round(span.TotalMinutes, 0);
            Log.Debug($"OfficerUnit {CallSign} arrived on scene after {mins} game minutes");

            // Telll dispatch we are on scene
            Dispatch.RegisterOnScene(this, CurrentCall);
            PoliceCar.IsSirenSilent = true;
            SetBlipColor(OfficerStatusColor.OnScene);

            // Tell officer to get out of patrol car after 1 second
            GameFiber.Wait(1000);

            // Do another sanity check
            SanityCheck();
            PoliceCar.Driver.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
            PoliceCar.IsSirenSilent = true;
            SetBlipColor(OfficerStatusColor.OnScene);

            var random = new CryptoRandom();
            int timeToComplete = random.Next(CurrentCall.ScenarioInfo.SimulationTime);

            var gameSpan = TimeSpan.FromMinutes(timeToComplete);
            var realSeconds = (int)TimeScale.ToRealTime(gameSpan).TotalSeconds;
            GameFiber.Wait(1000 * realSeconds);

            // Complete call
            CompleteCall(CallCloseFlag.Completed);
        }

        /// <summary>
        /// Drives the <see cref="Officer"/> around aimlessly. 
        /// Must be used in a <see cref="GameFiber"/> as this method is a
        /// blocking method
        /// </summary>
        private void Cruise()
        {
            NextTask = TaskSignal.None;
            EnsureInPoliceCar();
            Officer.Tasks.CruiseWithVehicle(PoliceCar, 10, VehicleDrivingFlags.Normal);
        }

        /// <summary>
        /// Makes the <see cref="OfficerUnit"/> take a 30 minute break
        /// </summary>
        private void TakeBreak()
        {
            NextTask = TaskSignal.None;
            Status = OfficerStatus.MealBreak;
            SetBlipColor(OfficerStatusColor.OnBreak);
            Officer.Tasks.CruiseWithVehicle(PoliceCar, 10, VehicleDrivingFlags.Normal);

            // 30 in game minutes
            var time = (int)TimeScale.RealSecondsFromGameSeconds(30 * 60);
            GameFiber.Wait(1000 * time);

            // Make available again
            SetBlipColor(OfficerStatusColor.Available);
            Status = OfficerStatus.Available;
        }

        internal override void AssignToCallWithRandomCompletion(PriorityCall call)
        {
            // Assign ourselves
            base.AssignToCall(call, true);

            var random = new CryptoRandom();
            var onScene = random.Next(1, 100) > 20;

            if (onScene)
            {
                call.CallStatus = CallStatus.OnScene;

                // Signal
                NextTask = TaskSignal.DoScene;
            }
            else
            {
                NextTask = TaskSignal.DriveToCall;
            }
        }

        /// <summary>
        /// A Task enumeration used to Signal the AI's thread into the
        /// next task.
        /// </summary>
        private enum TaskSignal
        {
            None,

            TakeABreak,

            Cruise,

            DriveToCall,

            DoScene
        }
    }
}
