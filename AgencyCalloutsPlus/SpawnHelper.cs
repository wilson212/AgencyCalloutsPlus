using AgencyCalloutsPlus.API;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus
{
    public static class SpawnHelper
    {
        public static int[] BlackListedNodeTypes = new int[] { 0, 8, 9, 10, 12, 40, 42, 136 };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="spawnPoint"></param>
        /// <param name="delete"></param>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public static bool SpawnVehicleAtPosition(Model model, SpawnPoint spawnPoint, bool delete, out Vehicle vehicle)
        {
            var flags = GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ConsiderHumanPeds;
            var entities = World.GetEntities(spawnPoint.Position, 5f, flags);
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
        /// Gets the closest in game vehicle node to this <see cref="Vector3"/> position
        /// </summary>
        /// <param name="pos">Starting point</param>
        /// <returns></returns>
        public static Vector3 GetClosestMajorVehicleNode(Vector3 pos)
        {
            NativeFunction.Natives.GET_CLOSEST_MAJOR_VEHICLE_NODE<bool>(pos.X, pos.Y, pos.Z, out Vector3 node, 3.0f, 0f);
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
            if (!NativeFunction.Natives.GET_SAFE_COORD_FOR_PED<bool>(pos.X, pos.Y, pos.Z, true, out Vector3 tempSpawn, 0))
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
    }
}
