using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game.Locations;
using LSPD_First_Response.Engine.Scripting.Entities;
using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents an <see cref="OfficerUnit"/> that  exists in the <see cref="World"/>
    /// as a <see cref="Ped"/> with a <see cref="Vehicle"/>
    /// </summary>
    public class PersistentAIOfficerUnit : IDisposable
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
        /// The <see cref="GameFiber"/> that runs the logic for this AI unit
        /// </summary>
        private GameFiber AILogicFiber { get; set; }

        /// <summary>
        /// Gets the next task for this AI unit on <see cref="AILogicFiber"/> ticks
        /// </summary>
        private TaskSignal NextTask { get; set; }

        /// <summary>
        /// Gets the <see cref="Vehicle"/> this officer uses as his police cruiser
        /// </summary>
        public Vehicle PoliceCar { get; private set; }

        /// <summary>
        /// Gets the officer <see cref="Ped"/>
        /// </summary>
        public Ped Officer { get; private set; }

        /// <summary>
        /// Gets the hand gun that is in this units inventory
        /// </summary>
        public WeaponDescriptor HandGun { get; private set; }

        /// <summary>
        /// Gets the long gun that is in this units inventory
        /// </summary>
        public WeaponDescriptor LongGun { get; private set; }

        /// <summary>
        /// Gets the non-lethal weapons that are in this units inventory
        /// </summary>
        public Dictionary<string, WeaponDescriptor> NonLethalWeapons { get; private set; }

        /// <summary>
        /// Gets the <see cref="PoliceCar"/>'s <see cref="Blip"/>
        /// </summary>
        public Blip VehicleBlip { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public AIOfficerUnit VirtualUnit { get; private set; }

        /// <summary>
        /// Indicates whether this instance is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="PersistentAIOfficerUnit"/> from a <see cref="VirtualUnit"/>
        /// </summary>
        /// <param name="virtualAI"></param>
        internal PersistentAIOfficerUnit(AIOfficerUnit virtualAI)
        {
            // Get location @todo Make better
            var pos = virtualAI.GetPosition();

            // Spawn using persona
            Officer = Persona.CreatePed(virtualAI.Persona, pos);
            Officer.IsPersistent = true;
            Officer.BlockPermanentEvents = true;
            Officer.RelationshipGroup = RelationshipGroup.Cop;

            // Set component variations
            foreach (var comp in virtualAI.PedMeta.Components)
            {
                // Extract ids
                int id = (int)comp.Key;
                int drawId = comp.Value.Item1;
                int textId = comp.Value.Item2;

                // Set component variation
                Officer.SetVariation(id, drawId, textId);
            }

            // Randomize props?
            if (virtualAI.PedMeta.RandomizeProps)
            {
                Officer.RandomizeProps();
            }

            // Set props
            foreach (var prop in virtualAI.PedMeta.Props)
            {
                // Extract ids
                int id = (int)prop.Key;
                int drawId = prop.Value.Item1;
                int textId = prop.Value.Item2;

                // Set component variation
                Officer.SetPropIndex(id, drawId, textId, true);
            }

            // Give officer thier weapons!
            if (virtualAI.HandGun != null)
            {
                HandGun = GiveOfficerWeapon(virtualAI.HandGun);
            }

            if (virtualAI.LongGun != null)
            {
                LongGun = GiveOfficerWeapon(virtualAI.LongGun);
            }

            // Give non-lethals
            NonLethalWeapons = new Dictionary<string, WeaponDescriptor>();
            foreach (string weapon in virtualAI.NonLethalWeapons)
            {
                NonLethalWeapons.Add(weapon, GiveOfficerWeapon(weapon));
            }

            // Spawn vehicle @todo Make positioning better
            Vector3Extensions.GetVehicleSpawnPointTowardsStartPoint(pos, 50, false, out SpawnPoint sp);
            if (sp != null && sp != Vector3.Zero)
            {
                PoliceCar = new Vehicle(virtualAI.VehicleMeta.Model, sp.Position, sp.Heading);
            }
            else
            {
                PoliceCar = new Vehicle(virtualAI.VehicleMeta.Model, pos, 1);
            }

            // Setup vehicle
            PoliceCar.MakePersistent();
            PoliceCar.SetLivery(virtualAI.VehicleMeta.LiveryIndex);

            // Set Extras
            foreach (var extra in virtualAI.VehicleMeta.Extras)
            {
                PoliceCar.SetExtraEnabled(extra.Key, extra.Value);
            }

            // Place officer inside his vehicle and create blip
            Officer.WarpIntoVehicle(PoliceCar, -1);
            SetBlipColor(OfficerStatusColor.Available);
                
            // Save
            VirtualUnit = virtualAI;

            // Load model
            Officer.Model.Load();
            PoliceCar.Model.Load();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="meta"></param>
        /// <seealso cref="https://docs.ragepluginhook.net/html/M_Rage_PedInventory_AddComponentToWeapon.htm"/>
        /// <returns></returns>
        private WeaponDescriptor GiveOfficerWeapon(WeaponMeta meta)
        {
            var name = meta.Name;
            var desc = Officer.Inventory.GiveNewWeapon(name, 30, false);

            // Attach components
            foreach (var comp in meta.Components)
            {
                Officer.Inventory.AddComponentToWeapon(desc.Asset, comp);
            }

            return desc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <seealso cref="https://docs.ragepluginhook.net/html/M_Rage_PedInventory_AddComponentToWeapon.htm"/>
        /// <returns></returns>
        private WeaponDescriptor GiveOfficerWeapon(string name)
        {
            return Officer.Inventory.GiveNewWeapon(name, 30, false);
        }

        /// <summary>
        /// Gets the position of this <see cref="OfficerUnit"/>
        /// </summary>
        /// <returns></returns>
        public Vector3 GetPosition()
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
                    Log.Debug($"PoliceCar is invalid for unit {VirtualUnit.CallSign} of {VirtualUnit.Agency.FullName}... Creating a new one");

                    // Determine spawn point
                    var oldCar = PoliceCar;
                    var location = World.GetNextPositionOnStreet(Officer.Position.Around(25));
                    var sp = new SpawnPoint(location);

                    // Create car
                    PoliceCar = null; //Dispatch.PlayerAgency.GetRandomPoliceVehicle(PatrolVehicleType.Marked, sp);
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
                    Log.Debug($"Officer is invalid for unit {VirtualUnit.CallSign} of {VirtualUnit.Agency.FullName}... Creating a new one");

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
        /// Disposes and releases this <see cref="Ped"/>
        /// </summary>
        public void Dispose()
        {
            // Now Dispose
            if (!IsDisposed)
            {
                // Close fiber
                AILogicFiber?.Abort();

                if (VehicleBlip?.Exists() ?? false)
                    VehicleBlip?.Delete();

                if (PoliceCar?.Exists() ?? false)
                    PoliceCar?.Delete();
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
