using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game.Locations;
using LSPD_First_Response;
using LSPD_First_Response.Engine.Scripting.Entities;
using Rage;
using System;
using System.Linq;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents an <see cref="OfficerUnit"/> that is simulated virtually in memory
    /// and does not actually exist in the <see cref="Rage.World"/>
    /// </summary>
    public class AIOfficerUnit : OfficerUnit
    {
        /// <summary>
        /// Indicates whether this is an AI player
        /// </summary>
        public override bool IsAIUnit => true;

        /// <summary>
        /// Gets a bool indicating whether this unit is existing in game
        /// </summary>
        private bool IsSpawned { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private Vehicle SpawnedVehicle { get; set; }
        /// <summary>
        /// 
        /// </summary>
        private Ped SpawnedPed { get; set; }

        /// <summary>
        /// Gets the meta data required to spawn this officer <see cref="Ped"/> in the game world.
        /// </summary>
        public OfficerModelMeta PedMeta { get; internal set; }

        /// <summary>
        /// Gets the meta data required to spawn this officer's vehicle in the game world.
        /// </summary>
        public VehicleModelMeta VehicleMeta { get; internal set; }

        /// <summary>
        /// Gets the handgun metadata this officer unit will spawn with in thier inventory.
        /// </summary>
        public string[] NonLethalWeapons { get; internal set; }

        /// <summary>
        /// Gets the handgun metadata this officer unit will spawn with in thier inventory.
        /// </summary>
        public WeaponMeta HandGun { get; internal set; }

        /// <summary>
        /// Gets the longgun metadata this officer unit will spawn with in thier inventory. 
        /// </summary>
        public WeaponMeta LongGun { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="AIOfficerUnit"/>
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="unitString"></param>
        internal AIOfficerUnit(VehicleSet vehicleSet, Agency agency, CallSign callSign) : base(agency, callSign)
        {
            // Grab officer Meta
            if (!vehicleSet.OfficerMetas.TrySpawn(out OfficerModelMeta meta))
            {
                throw new Exception($"Unable to spawn an OfficerModelMeta for this AIOfficerUnit instance from agency {agency.ScriptName}");
            }

            // Grab vehicle Meta
            if (!vehicleSet.VehicleMetas.TrySpawn(out VehicleModelMeta vehicle))
            {
                throw new Exception($"Unable to spawn an VehicleModelMeta for this AIOfficerUnit instance from agency {agency.ScriptName}");
            }

            // Grab HandGun Meta
            if (!vehicleSet.HandGunMetas.TrySpawn(out WeaponMeta handGunMeta) || !vehicleSet.LongGunMetas.TrySpawn(out WeaponMeta longGunMeta))
            {
                throw new Exception($"Unable to spawn an WeaponMeta for this AIOfficerUnit instance from agency {agency.ScriptName}");
            }

            // Create a randon
            var rnd = new CryptoRandom();
            var now = World.DateTime;

            // Create a persona
            string model = meta.Model.ToString();
            Gender gender = model.Contains("_f_") ? Gender.Female : Gender.Male;
            var birthday = rnd.NextDateTime(now.AddYears(-50), now.AddYears(-22));
            var name = RandomNameGenerator.Generate(gender);

            // Set meta properties
            PedMeta = meta;
            VehicleMeta = vehicle;
            HandGun = handGunMeta;
            LongGun = longGunMeta;
            NonLethalWeapons = vehicleSet.NonLethalWeapons.ToArray();

            // Create persona and set callsign
            Persona = new Persona(name.Forename, name.Surname, gender, birthday, model)
            {
                Wanted = false // Ensure this is always false
            };
        }

        /// <summary>
        /// Spawns this <see cref="AIOfficerUnit"/> into the <see cref="World"/> if not already, 
        /// and returns the <see cref="Ped"/> and <see cref="Vehicle"/> locations
        /// </summary>
        public void Spawn()
        {
            // @todo 
            if (IsSpawned)
            {

            }
            else
            {
                // Get location @todo Make better
                var pos = Position;

                // Spawn from persona
                SpawnedPed = Persona.CreatePed(Persona, pos);

                // Set component variations
                foreach (var comp in PedMeta.Components)
                {
                    // Extract ids
                    int id = (int)comp.Key;
                    int drawId = comp.Value.Item1;
                    int textId = comp.Value.Item2;

                    // Set component variation
                    SpawnedPed.SetVariation(id, drawId, textId);
                }

                // Set props or randomize
                foreach (var prop in PedMeta.Props)
                {
                    // Extract ids
                    int id = (int)prop.Key;
                    int drawId = prop.Value.Item1;
                    int textId = prop.Value.Item2;

                    // Set component variation
                    SpawnedPed.SetPropIndex(id, drawId, textId, true);
                }

                // @todo Spawn vehicle
            }
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

            // @todo Proactive Policing
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
                Log.Debug($"OfficerUnit {CallSign} of {Agency.FullName} completed call '{CurrentCall.ScenarioInfo.Name}' with flag: {flag}");
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
            Log.Debug($"OfficerUnit {CallSign} of {Agency.FullName} responding to call '{CurrentCall.ScenarioInfo.Name}'");
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
            Log.Debug($"OfficerUnit {CallSign} of {Agency.FullName} arrived on scene");

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

        /// <summary>
        /// Assigns this <see cref="AIOfficerUnit"/> to a <see cref="PriorityCall"/> with
        /// a random completion percentage. This method is to be called when the player goes
        /// on duty, and only on officers that were part of the previous <see cref="Game.TimePeriod"/>
        /// </summary>
        /// <remarks>
        /// This method helps going on shift feel more realistic
        /// </remarks>
        /// <param name="call">The call to assign to this instance</param>
        internal void AssignToCallWithRandomCompletion(PriorityCall call)
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
