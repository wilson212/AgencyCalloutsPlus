using AgencyCalloutsPlus.API;
using Rage;
using System;
using System.Linq;
using static Rage.Native.NativeFunction;

namespace AgencyCalloutsPlus.Extensions
{
    /// <summary>
    /// Credits to Albo1125 and the scripthookvdotnet team for most of these methods
    /// </summary>
    /// <seealso cref="https://github.com/crosire/scripthookvdotnet/blob/main/source/scripting_v3/GTA/World.cs"/>
    /// <seealso cref="https://github.com/Albo1125/Albo1125-Common/blob/master/Albo1125.Common/CommonLibrary/SpawnPoint.cs"/>
    /// <seealso cref="https://gtaforums.com/topic/843561-pathfind-node-types/"/>
    /// <seealso cref="https://gta.fandom.com/wiki/Paths_(GTA_V)"/>
    public static class Vector3Extensions
    {
        public static int[] BlackListedNodeTypes = new int[] { 0, 8, 9, 10, 12, 40, 42, 136 };

        /// <summary>	
        /// Gets the nearest safe coordinate to position a <see cref="Ped"/> from this <see cref="Vector3"/>
        /// </summary>	
        /// <param name="pos">The position to check around</param>	
        /// <param name="safePedPoint"></param>	
        /// <param name="sidewalk">if set to true, Only find positions on the sidewalk</param>
        /// <param name="flags">In the scripts these flags are used: 0, 14, 12, 16, 20, 21, 28. 0 is most commonly used, then 16.</param>
        /// <returns>true if a successfull vector3 is found, false otherwise</returns>	
        /// <seealso cref="http://www.dev-c.com/nativedb/func/info/b61c8e878a4199ca"/>	
        /// <seealso cref="https://runtime.fivem.net/doc/natives/?_0xB61C8E878A4199CA"/>
        public static unsafe bool GetSafeVector3ForPed(this Vector3 pos, out Vector3 safePedPoint, bool sidewalk = true, int flags = 0)
        {
            // Try and have the game grab a safe Ped vector3
            if (!Natives.GetSafeCoordForPed<bool>(pos.X, pos.Y, pos.Z, sidewalk, out Vector3 tempSpawn, flags))
            {
                // Use a generic next postition on the street. Then we will try and get the closest
                // Ped entity from there, and use that as a safe Vector3
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
        /// Gets the closest in game vehicle node to this <see cref="Vector3"/> position
        /// </summary>
        /// <param name="pos">The position to check around</param>
        /// <returns></returns>
        public static Vector3 GetClosestMajorVehicleNode(this Vector3 pos)
        {
            Natives.GET_CLOSEST_MAJOR_VEHICLE_NODE<bool>(pos.X, pos.Y, pos.Z, out Vector3 node, 3.0f, 0f);
            return node;
        }

        /// <summary>
        /// Gets the closest <see cref="SpawnPoint"/> for a vehicle with heading around this
        /// <see cref="Vector3"/> position
        /// </summary>
        /// <param name="pos">The position to check around</param>
        /// <param name="sp"></param>
        /// <returns></returns>
        /// <seealso cref="https://gtaforums.com/topic/843561-pathfind-node-types/"/>
        public static bool GetClosestVehicleSpawnPoint(this Vector3 pos, out SpawnPoint sp)
        {
            Vector3 tempSpawnPoint;
            float tempHeading;
            bool guaranteedSpawnPointFound = true;
            unsafe
            {
                bool found = Natives.GetClosestVehicleNodeWithHeading<bool>(
                    pos.X, 
                    pos.Y, 
                    pos.Z, 
                    out tempSpawnPoint, 
                    out tempHeading, 
                    1, 
                    0x40400000, 
                    0
                );

                // If not found, or blacklisted node, attempt to grab a position manually
                if (!found || !IsNodeSafe(tempSpawnPoint))
                {
                    tempSpawnPoint = World.GetNextPositionOnStreet(pos);
                    var flags = GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ExcludeEmptyVehicles | GetEntitiesFlags.ExcludePlayerVehicle;
                    Entity entity = World.GetClosestEntity(tempSpawnPoint, 30f, flags);

                    if (entity.Exists())
                    {
                        tempSpawnPoint = entity.Position;
                        tempHeading = entity.Heading;
                        entity.Delete();
                    }
                    else
                    {
                        Vector3 directionFromSpawnToPlayer = (Game.LocalPlayer.Character.Position - tempSpawnPoint);
                        directionFromSpawnToPlayer.Normalize();

                        tempHeading = MathHelper.ConvertDirectionToHeading(directionFromSpawnToPlayer) + 180f;
                        guaranteedSpawnPointFound = false;
                    }
                }
            }

            sp = new SpawnPoint(tempSpawnPoint, tempHeading);
            return guaranteedSpawnPointFound;
        }

        /// <summary>
        /// Gets the next position on the street where a <see cref="Vehicle"/> can be placed.
        /// </summary>
        /// <param name="startPoint">The position to check around</param>
        /// <param name="spawnDistance"></param>
        /// <param name="useSpecialID"></param>
        /// <param name="sp"></param>
        /// <returns></returns>
        /// <seealso cref="https://gtaforums.com/topic/843561-pathfind-node-types/"/>
        public static bool GetVehicleSpawnPointTowardsStartPoint(this Vector3 startPoint, float spawnDistance, bool useSpecialID, out SpawnPoint sp)
        {
            Vector3 fromPoint = World.GetNextPositionOnStreet(startPoint.Around2D(spawnDistance + 5f));
            Vector3 spawnPoint = Vector3.Zero;
            bool specialIDused = true;

            // Attempt to find using natives
            bool found = Natives.GetNthClosestVehicleNodeFavourDirection<bool>(
                fromPoint.X, 
                fromPoint.Y, 
                fromPoint.Z, 
                startPoint.X, 
                startPoint.Y, 
                startPoint.Z, 
                0, 
                out spawnPoint, 
                out float heading, 
                0, 
                0x40400000, 
                0
            );

            if (!useSpecialID || !found || !IsNodeSafe(spawnPoint))
            {
                spawnPoint = World.GetNextPositionOnStreet(startPoint.Around2D(spawnDistance + 5f));
                Vector3 directionFromVehicleToPed1 = (startPoint - spawnPoint);
                directionFromVehicleToPed1.Normalize();
                heading = MathHelper.ConvertDirectionToHeading(directionFromVehicleToPed1);
                specialIDused = false;
            }

            sp = new SpawnPoint(spawnPoint, heading);
            return specialIDused;
        }

        public static SpawnPoint GetVehicleSpawnPointTowardsPositionWithChecks(this Vector3 startPoint, float SpawnDistance)
        {
            SpawnPoint sp = new SpawnPoint(startPoint);
            bool useSpecialID = true;
            float travelDistance;
            int waitCount = 0;

            while (true)
            {
                waitCount++;
                GetVehicleSpawnPointTowardsStartPoint(startPoint, SpawnDistance, useSpecialID, out sp);

                if (Vector3.Distance(startPoint, sp.Position) > SpawnDistance - 15f)
                {
                    travelDistance = startPoint.TravelDistanceTo(sp.Position);
                    if (travelDistance < (SpawnDistance * 4.5f))
                    {
                        Vector3 directionFromVehicleToPed1 = (startPoint - sp.Position);
                        directionFromVehicleToPed1.Normalize();

                        float headingToPlayer = MathHelper.ConvertDirectionToHeading(directionFromVehicleToPed1);
                        if (Math.Abs(MathHelper.NormalizeHeading(sp.Heading) - MathHelper.NormalizeHeading(headingToPlayer)) < 150f)
                        {
                            break;
                        }
                    }
                }

                if (waitCount >= 400)
                {
                    useSpecialID = false;
                }

                GameFiber.Yield();
            }

            return sp;
        }

        public static int GetNearestNodeType(this Vector3 pos)
        {
            if (Natives.GET_VEHICLE_NODE_PROPERTIES<bool>(pos.X, pos.Y, pos.Z, out uint density, out int nodeType))
            {
                return nodeType;
            }
            else
            {
                return -1;
            }
        }

        public static bool IsNodeSafe(this Vector3 pos)
        {
            return !BlackListedNodeTypes.Contains(GetNearestNodeType(pos));
        }

        /// <summary>
        /// Returns wether this <see cref="Vector3"/> position us on water
        /// </summary>
        /// <param name="pos">The position to check</param>
        /// <returns></returns>
        public static bool IsPointOnWater(this Vector3 pos)
        {
            return Natives.GET_WATER_HEIGHT<bool>(pos.X, pos.Y, pos.Z, out float height);
        }

        /// <summary>
        /// Calculates and returns the heading between this position and the specified
        /// <see cref="Vector3"/> position
        /// </summary>
        /// <param name="start">The position to start from</param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static float CalculateHeadingTowardsPosition(this Vector3 start, Vector3 position)
        {
            Vector3 directionToTargetEnt = (position - start);
            directionToTargetEnt.Normalize();
            return MathHelper.ConvertDirectionToHeading(directionToTargetEnt);
        }

        /// <summary>
        /// Converts the comma-seperated string representation of a vector3 to its <see cref="Vector3"/> equivalent.
        /// </summary>
        /// <param name="v">A string containing the values to convert.</param>
        /// <param name="vector"></param>
        /// <returns>true if <paramref name="v"/> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string v, out Vector3 vector)
        {
            // Ensure we have 3 vector coordinates
            string[] parts = v.Split(',');
            if (parts.Length != 3)
            {
                vector = Vector3.Zero;
                return false;
            }

            // Parse each coordinate from string to float
            float[] coords = new float[3] { 0f, 0f, 0f };
            for (int i = 0; i < 3; i++)
            {
                // try and parse string value
                if (!float.TryParse(parts[i].Trim(), out float val))
                {
                    vector = Vector3.Zero;
                    return false;
                }

                coords[i] = val;
            }

            // If we are here, we parsed good
            vector = new Vector3(coords[0], coords[1], coords[2]);
            return true;
        }
    }
}
