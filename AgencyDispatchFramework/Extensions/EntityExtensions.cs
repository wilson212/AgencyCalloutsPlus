﻿using Rage;
using System;

namespace AgencyDispatchFramework.Extensions
{
    /// <summary>
    /// Credits to Albo1125 for most of these methods
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Calculates a heading from one <see cref="Rage.Entity"/> towards another
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static float CalculateHeadingTowardsEntity(this Entity ent, Entity target)
        {
            Vector3 directionToTargetEnt = (target.Position - ent.Position);
            directionToTargetEnt.Normalize();
            return MathHelper.ConvertDirectionToHeading(directionToTargetEnt);
        }

        /// <summary>
        /// Cleans up the <see cref="Entity"/> but checking if valid, removing any
        /// persistance on the <see cref="Entity"/>, removing and deleting any
        /// <see cref="Blip"/>s attached to the <see cref="Entity"/>, and dismissing
        /// or deleting the <see cref="Entity"/> depending on if visible.
        /// </summary>
        /// <param name="entity"></param>
        public static void Cleanup(this Entity entity)
        {
            // Variables to mark an error step if we have one
            string name = "Unknown";
            string step = "None";

            // Quit here if null
            if (entity == null) return;

            // No exceptions
            try
            {
                // Ensure we have a valid entity!
                if (entity.Exists())
                {
                    step = "GetModelName";
                    name = entity.Model.Name ?? "NoNameSet";

                    // Remove any persistance. Always do this first
                    step = "RemovePersistance";
                    entity.IsPersistent = false;

                    // Delete any blips attached to this entity
                    step = "GetBlips";
                    var blips = entity.GetAttachedBlips();
                    if (blips != null && blips.Length > 0)
                    {
                        step = "RemoveBlips";
                        foreach (var blip in blips)
                        {
                            if (blip.Exists())
                            {
                                blip.Delete();
                            }
                        }
                    }

                    // Dismiss entity if visible, or delete
                    step = "GetVisible";
                    if (!entity.IsVisible)
                    {
                        step = "DeleteEntity";
                        entity.Delete();
                    }
                    else
                    {
                        step = "DismissEntity";
                        entity.Dismiss();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Error cleaning up entity with name of '{name}' on step '{step}'");
            }
        }
    }
}
