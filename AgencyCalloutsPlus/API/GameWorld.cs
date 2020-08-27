using AgencyCalloutsPlus.Mod;
using Rage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Rage.Native.NativeFunction;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Provides methods to get and set information within the Game World
    /// </summary>
    /// <seealso cref="https://github.com/crosire/scripthookvdotnet/blob/main/source/scripting_v3/GTA/World.cs"/>
    public static class GameWorld
    {
        /// <summary>
        /// Our lock object to prevent threading issues
        /// </summary>
        private static System.Object _lock = new System.Object();

        /// <summary>
        /// Event called when the <see cref="CurrentTimeOfDay"/> has changed in game
        /// </summary>
        public static event EventHandler OnTimeOfDayChanged;

        /// <summary>
        /// An event fired when the <see cref="Weather"/> changes in game.
        /// </summary>
        /// <param name="oldWeather"></param>
        /// <param name="newWeather"></param>
        public delegate void WeatherChangedEventHandler(Weather oldWeather, Weather newWeather);

        /// <summary>
        /// Event called when the weather changes in game
        /// </summary>
        public static event WeatherChangedEventHandler OnWeatherChange;

        /// <summary>
        /// Runs every 2 seconds
        /// </summary>
        private static GameFiber WorldWatchingFiber { get; set; }

        /// <summary>
        /// Credits to Albo1125
        /// </summary>
        public static int[] BlackListedNodeTypes = new int[] { 0, 8, 9, 10, 12, 40, 42, 136 };

        #region Weather & Effects

        internal static readonly string[] WeatherNames = {
            "EXTRASUNNY",
            "CLEAR",
            "CLOUDS",
            "SMOG",
            "FOGGY",
            "OVERCAST",
            "RAIN",
            "THUNDER",
            "CLEARING",
            "NEUTRAL",
            "SNOW",
            "BLIZZARD",
            "SNOWLIGHT",
            "XMAS",
            "HALLOWEEN"
        };

        /// <summary>
        /// Gets or sets the weather in game
        /// </summary>
        private static Weather LastKnownWeather { get; set; }

        /// <summary>
        /// Gets or sets the weather in game
        /// </summary>
        public static TimeOfDay CurrentTimeOfDay { get; internal set; }

        /// <summary>
        /// Gets or sets the weather in game
        /// </summary>
        public static Weather CurrentWeather
        {
            get
            {
                lock (_lock)
                {
                    return LastKnownWeather;
                }
            }
            set
            {
                if (Enum.IsDefined(typeof(Weather), value) && value != Weather.Unknown)
                {
                    lock (_lock)
                    {
                        Natives.SetWeatherTypeNow(WeatherNames[(int)value]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the next weather to happen in game
        /// </summary>
        public static Weather NextWeather
        {
            get
            {
                var weatherHash = Natives.GetNextWeatherTypeHashName<uint>();
                for (int i = 0; i < WeatherNames.Length; i++)
                {
                    if (weatherHash == Game.GetHashKey(WeatherNames[i]))
                    {
                        return (Weather)i;
                    }
                }

                return Weather.Unknown;
            }
        }

        #endregion

        /// <summary>
        /// Sets the initial weather and Time of day without firing events
        /// </summary>
        internal static void Initialize()
        {
            // Set current stuff
            CurrentTimeOfDay = GetCurrentWorldTimeOfDay();
            LastKnownWeather = GetCurrentWeather();
        }

        /// <summary>
        /// Begins all internal <see cref="GameFiber"/> instances
        /// </summary>
        internal static void BeginFibers()
        {
            WorldWatchingFiber = GameFiber.StartNew(UpdateWorldState);
        }

        /// <summary>
        /// 
        /// </summary>
        private static void UpdateWorldState()
        {
            // Wait
            GameFiber.Wait(3000);

            // While we are on duty accept calls
            while (Main.OnDuty)
            {
                try
                {
                    // Do police checks
                    Dispatch.ProcessDispatchLogic();

                    // Update weather
                    var currentWeather = GetCurrentWeather();
                    if (currentWeather != CurrentWeather)
                    {
                        Weather lastWeather = Weather.Unknown;
                        lock (_lock)
                        {
                            lastWeather = LastKnownWeather;
                            LastKnownWeather = currentWeather;
                        }

                        // Fire event
                        OnWeatherChange?.Invoke(lastWeather, currentWeather);
                        continue;
                    }

                    // Get current Time of Day and check for changes
                    var currentTimeOfDay = GetCurrentWorldTimeOfDay();
                    if (currentTimeOfDay != CurrentTimeOfDay)
                    {
                        // Set
                        CurrentTimeOfDay = currentTimeOfDay;

                        // Fire event
                        OnTimeOfDayChanged?.Invoke(null, EventArgs.Empty);
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                // Wait
                GameFiber.Wait(2000);
            }
        }

        /// <summary>
        /// Gets the <see cref="CurrentTimeOfDay"/>
        /// </summary>
        /// <returns></returns>
        private static TimeOfDay GetCurrentWorldTimeOfDay()
        {
            var currentHour = World.TimeOfDay.Hours;
            var currentTimeOfDay = Parse(currentHour);
            return currentTimeOfDay;

            // Local function
            TimeOfDay Parse(int hour)
            {
                if (hour < 6) return TimeOfDay.Night;
                else if (hour < 12) return TimeOfDay.Morning;
                else if (hour < 18) return TimeOfDay.Day;
                else return TimeOfDay.Evening;
            }
        }

        /// <summary>
        /// Gets the current in game weather using Natives
        /// </summary>
        /// <returns></returns>
        private static Weather GetCurrentWeather()
        {
            var weatherHash = Natives.GetPrevWeatherTypeHashName<uint>();
            for (int i = 0; i < WeatherNames.Length; i++)
            {
                if (weatherHash == Game.GetHashKey(WeatherNames[i]))
                {
                    return (Weather)i;
                }
            }

            return Weather.Unknown;
        }

        /// <summary>
		/// Transitions to the specified weather.
		/// </summary>
		/// <param name="weather">The weather to transition to</param>
		/// <param name="duration">The duration of the transition. If set to zero, the weather 
        /// will transition immediately</param>
		public static void TransitionToWeather(Weather weather, float duration)
        {
            if (Enum.IsDefined(typeof(Weather), weather) && weather != Weather.Unknown)
            {
                Natives.SetWeatherTypeOvertimePersist(WeatherNames[(int)weather], duration);
            }
        }

        /// <summary>
        /// Gets the current world <see cref="WeatherInfo" />
        /// </summary>
        /// <returns></returns>
        public static WeatherInfo GetWeatherInfo()
        {
            return new WeatherInfo();
        }

        #region Spawning Entity Methods 

        /// <summary>
        /// Attempts to spawn a <see cref="Rage.Vehicle"/> at the specified location safely, without collision of another
        /// <see cref="Entity"/> at the specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="model">The name of the <see cref="Model"/> to spawn</param>
        /// <param name="spawnPoint">The location to spawn the <see cref="Ped"/> at</param>
        /// <param name="delete">If true, checks for any other <see cref="Ped"/> or <see cref="Vehicle"/> entities at this location
        /// and deletes them if they are. If false, and there is an <see cref="Entity"/> at this location, this method will fail and return false</param>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public static bool TrySpawnVehicleAtPosition(Model model, SpawnPoint spawnPoint, bool delete, out Vehicle vehicle)
        {
            var flags = GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ConsiderHumanPeds;
            var entities = World.GetEntities(spawnPoint.Position, 3f, flags);
            if (entities.Length > 0)
            {
                if (delete)
                {
                    // Delete each entity
                    foreach (var ent in entities)
                        ent.Delete();
                }
                else
                {
                    vehicle = default(Vehicle);
                    return false;
                }
            }

            // Create vehicle
            vehicle = new Vehicle(model, spawnPoint.Position, spawnPoint.Heading);
            return true;
        }

        /// <summary>
        /// Attempts to spawn a <see cref="Rage.Ped"/> at the specified location safely, without collision of another
        /// <see cref="Entity"/> at the specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="spawnPoint">The location to spawn the <see cref="Ped"/> at</param>
        /// <param name="group">The <see cref="PedVariantGroup"/> to select the ped model from</param>
        /// <param name="gender">The <see cref="Gender"/> of the ped to spawn. If <see cref="Gender.Unknown"/> is passed, it will be randomized.</param>
        /// <param name="delete">If true, checks for any other <see cref="Ped"/> or <see cref="Vehicle"/> entities at this location
        /// and deletes them if they are. If false, and there is an <see cref="Entity"/> at this location, this method will fail and return false</param>
        /// <param name="ped">if successful, containts the <see cref="Ped"/> spawned</param>
        /// <returns></returns>
        public static bool TrySpawnRandomPedAtPosition(SpawnPoint spawnPoint, PedVariantGroup group, Gender gender, bool delete, out Ped ped)
        {
            // Randmonize gender
            var random = new CryptoRandom();
            if (gender == Gender.Unknown)
            {
                gender = random.PickOne(Gender.Male, Gender.Female);
            }

            // Grab all peds in the specified variant group
            ped = default(Ped);
            var groupPeds = PedInfo.PedModelsByVariant[group];
            if (groupPeds.Count == 0)
            {
                Log.Warning($"GameWorld.SpawnPedAtPosition(): PedVariantGroup named {group} has no peds in it");
                return false;
            }

            // Pull gender
            string pull = (gender == Gender.Male) ? "_m_" : "_f_";
            var items = groupPeds.Where(x => x.Contains(pull)).ToArray();

            // Criteria cant be met
            if (items.Length == 0) return false;

            // Grab random name and attempt to spawn
            var name = items[random.Next(0, items.Length - 1)];
            return TrySpawnPedAtPosition(name, spawnPoint, delete, out ped);
        }

        /// <summary>
        /// Attempts to spawn a <see cref="Rage.Ped"/> at the specified location safely, without collision of another
        /// <see cref="Entity"/> at the specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="spawnPoint">The location to spawn the <see cref="Ped"/> at</param>
        /// <param name="gender">The <see cref="Gender"/> of the ped to spawn. If <see cref="Gender.Unknown"/> is passed, it will be randomized.</param>
        /// <param name="delete">If true, checks for any other <see cref="Ped"/> or <see cref="Vehicle"/> entities at this location
        /// and deletes them if they are. If false, and there is an <see cref="Entity"/> at this location, this method will fail and return false</param>
        /// <param name="ped">if successful, containts the <see cref="Ped"/> spawned</param>
        /// <returns></returns>
        public static bool TrySpawnRandomPedAtPosition(SpawnPoint spawnPoint, Gender gender, bool delete, out Ped ped)
        {
            // Randmonize gender
            var random = new CryptoRandom();
            if (gender == Gender.Unknown)
            {
                gender = random.PickOne(Gender.Male, Gender.Female);
            }

            // Pull gender
            string pull = (gender == Gender.Male) ? "_m_" : "_f_";
            var items = Model.PedModels.Where(x => x.Name.Contains(pull)).ToArray();

            // Criteria cant be met
            if (items.Length == 0)
            {
                ped = default(Ped);
                return false;
            }

            // Grab random name and attempt to spawn
            var name = items[random.Next(0, items.Length - 1)];
            return TrySpawnPedAtPosition(name, spawnPoint, delete, out ped);
        }

        /// <summary>
        /// Attempts to spawn a <see cref="Rage.Ped"/> at the specified location safely, without collision of another
        /// <see cref="Entity"/> at the specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="pedName"></param>
        /// <param name="spawnPoint"></param>
        /// <param name="delete"></param>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static bool TrySpawnPedAtPosition(Model pedName, SpawnPoint spawnPoint, bool delete, out Ped ped)
        {
            // Ensure no other entities are there
            var flags = GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ConsiderHumanPeds;
            var entities = World.GetEntities(spawnPoint.Position, 1f, flags);
            if (entities.Length > 0)
            {
                if (delete)
                {
                    // Delete each entity
                    foreach (var ent in entities)
                        ent.Delete();
                }
                else
                {
                    ped = default(Ped);
                    return false;
                }
            }

            // Spawn ped
            ped = new Ped(pedName, spawnPoint.Position, spawnPoint.Heading);
            return true;
        }

        /// <summary>
        /// Creates a random ped at the specified position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Ped SpawnRandomPedAtPosition(Vector3 position)
        {
            return new Ped(position);
        }

        /// <summary>
        /// Creates a random ped at the specified position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Ped SpawnRandomPedAtPosition(SpawnPoint position)
        {
            return new Ped(position.Position, position.Heading);
        }

        #endregion Spawning Entity Methods 

        /// <summary>
        /// Gets the closest in game vehicle node to this <see cref="Vector3"/> position
        /// </summary>
        /// <param name="pos">Starting point</param>
        /// <returns></returns>
        public static Vector3 GetClosestMajorVehicleNode(Vector3 pos)
        {
            Natives.GetClosestMajorVehicleNode<bool>(pos.X, pos.Y, pos.Z, out Vector3 node, 3.0f, 0f);
            return node;
        }

        /// <summary>
        /// Gets a safe <see cref="Vector3"/> position for a <see cref="Ped"/> using natives
        /// </summary>
        /// <param name="pos">Starting point to spawn a <see cref="Ped"/></param>
        /// <param name="safePedPoint"></param>
        /// <returns></returns>
        /// <seealso cref="http://www.dev-c.com/nativedb/func/info/b61c8e878a4199ca"/>
        public static unsafe bool GetSafeVector3ForPedNear(Vector3 pos, out Vector3 safePedPoint)
        {
            if (!Natives.GetSafeCoordForPed<bool>(pos.X, pos.Y, pos.Z, true, out Vector3 tempSpawn, 0))
            {
                tempSpawn = World.GetNextPositionOnStreet(pos);
                Entity nearbyentity = World.GetClosestEntity(tempSpawn, 25f, GetEntitiesFlags.ConsiderHumanPeds);
                if (nearbyentity.Exists())
                {
                    tempSpawn = nearbyentity.Position;
                    safePedPoint = tempSpawn;
                    return true;
                }
                else
                {
                    safePedPoint = tempSpawn;
                    return false;
                }
            }
            safePedPoint = tempSpawn;
            return true;
        }

        /// <summary>
        /// Creates a checkpoint at the specified location, and returns the handle
        /// </summary>
        /// <remarks>
        /// Checkpoints are already handled by the game itself, so you must not loop it like markers.
        /// </remarks>
        /// <param name="pos">The position of the checkpoint</param>
        /// <param name="radius">The radius of the checkpoint cylinder</param>
        /// <param name="color">The color of the checkpoint</param>
        /// <returns>returns the handle of the checkpoint</returns>
        public static int CreateCheckpoint(Vector3 pos, Color color, float radius = 5f, float nearHeight = 4f, float farHeight = 4f, bool forceGround = false)
        {
            if (forceGround)
            {
                var level = World.GetGroundZ(pos, true, false);
                if (level.HasValue)
                    pos.Z = level.Value;
            }

            // Create checkpoint
            int handle = Natives.CreateCheckpoint<int>(47, pos.X, pos.Y, pos.Z, pos.X, pos.Y, pos.Z, 1f, color.R, color.G, color.B, color.A, 0);

            // Set hieght
            Natives.SetCheckpointCylinderHeight(handle, nearHeight, farHeight, radius);

            // return handle
            return handle;
        }

        /// <summary>
        /// Deletes a checkpoint with the specified handle
        /// </summary>
        /// <param name="handle"></param>
        public static void DeleteCheckpoint(int handle)
        {
            Natives.DeleteCheckpoint(handle);
        }
    }
}
