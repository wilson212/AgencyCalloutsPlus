using Rage;
using AgencyDispatchFramework.Integration;
using static Rage.Native.NativeFunction;
using System;

namespace AgencyDispatchFramework.Extensions
{
    public static class PedExtensions
    {
        /// <summary>
        /// Indicates whether this <see cref="Ped"/> instance if facing another
        /// <see cref="Ped"/> within the specified angle
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="otherPed"></param>
        /// <param name="angle">the view angle</param>
        /// <returns></returns>
        public static bool IsFacingPed(this Ped ped, Ped otherPed, float angle)
        {
            return Natives.IsPedFacingPed<bool>(ped, otherPed, angle);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ped"></param>
        /// <seealso cref="https://github.com/Albo1125/Albo1125-Common/blob/master/Albo1125.Common/CommonLibrary/ExtensionMethods.cs#L298"/>
        public static void MakeMissionPed(this Ped ped, bool value)
        {
            ped.BlockPermanentEvents = value;
            ped.IsPersistent = value;
        }

        /// <summary>
        /// Returns whether this ped is a police ped
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        /// <seealso cref="https://github.com/Albo1125/Albo1125-Common/blob/master/Albo1125.Common/CommonLibrary/ExtensionMethods.cs#L153"/>
        public static bool IsPolicePed(this Ped ped)
        {
            return ped.RelationshipGroup == "COP";
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Ped"/> is under the influence of alcohol
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static bool GetIsDrunk(this Ped ped)
        {
            return StopThePedAPI.IsPedDrunk(ped);
        }

        /// <summary>
        /// Sets whether the <see cref="Ped"/> is under the influence of alcohol
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="isDrunk">if true, the <see cref="Ped"/> will be marked as Drunk, and given a new movement clipset.</param>
        public static void SetIsDrunk(this Ped ped, bool isDrunk)
        {
            if (isDrunk)
            {
                // Determine a random alcohol level
                var level = new CryptoRandom().Next(60, 200);
                float bac = level / 1000f;
                SetAlcoholLevel(ped, bac);
            }
            else
            {
                // Reset
                Natives.ResetPedMovementClipset(ped, 0.0f);
                ped.Metadata.stpAlcoholDetected = false;
                ped.Metadata.stpAlcoholLevel = 0f;
                ped.Metadata.BAC = 0f;
            }
        }

        /// <summary>
        /// Sets whether the <see cref="Ped"/> is under the influence of alcohol
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="isDrunk"></param>
        /// <param name="bacLevel">Sets a real BAC level (0.08 is the legal limit) on the <see cref="Ped"/>. Value should be between 0.00 and 0.20</param>
        public static void SetAlcoholLevel(this Ped ped, float bacLevel)
        {
            // Alert stop the ped
            bool wasDrunk = GetIsDrunk(ped);
            bool isDrunk = bacLevel > 0.059;
            StopThePedAPI.SetPedIsDrunk(ped, isDrunk);
            
            // Apply movement sets and BAC reading
            if (isDrunk)
            {
                string GetAnimaionString()
                {
                    if (bacLevel > 0.139)
                    {
                        return "MOVE_M@DRUNK@VERYDRUNK";
                    }
                    else if (bacLevel > 0.109)
                    {
                        return "MOVE_M@DRUNK@MODERATEDRUNK_HEAD_UP";
                    }
                    else if (bacLevel > 0.059)
                    {
                        return "MOVE_M@DRUNK@SLIGHTLYDRUNK";
                    }
                    else return null;
                }

                // Get movement animation set based on drunk level
                var animation = GetAnimaionString();
                if (!String.IsNullOrEmpty(animation))
                {
                    // This was returning null, so added null opperator
                    if (!(Natives.HasAnimSetLoaded<bool>(animation) ?? false))
                    {
                        Natives.RequestAnimSet(animation);
                    }

                    // Play movement animation set
                    Natives.SetPedMovementClipset(ped, animation, 1f);
                }

                // Set metadata
                ped.Metadata.stpAlcoholLevel = bacLevel;
                ped.Metadata.BAC = bacLevel;
            }
            else if (wasDrunk)
            {
                // Reset
                Natives.ResetPedMovementClipset(ped, 0.0f);
                ped.Metadata.stpAlcoholDetected = false;
                ped.Metadata.stpAlcoholLevel = 0f;
                ped.Metadata.BAC = 0f;
            }
        }

        /// <summary>
        /// Gets a value indicating the BAC level of this <see cref="Ped"/>
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static float GetAlcoholLevel(this Ped ped)
        {
            var metaObject = (MetadataObject)ped.Metadata;
            return (metaObject.Contains("stpAlcoholLevel")) ? ped.Metadata.stpAlcoholLevel : 0.00f;
        }

        /// <summary>
        /// Sets whether the <see cref="Ped"/> is under the influence of drugs
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="isDrunk"></param>
        public static void SetIsUnderDrugInfluence(this Ped ped, bool isInfluenced)
        {
            StopThePedAPI.SetPedIsDrugInfluenced(ped, isInfluenced);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Ped"/> is under the influence of drugs
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static bool GetIsUnderDrugInfluence(this Ped ped)
        {
            return StopThePedAPI.IsPedUnderDrugInfluence(ped);
        }

        /// <summary>
        /// Plays a scenario on a Ped at their current location.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="scenarioName"></param>
        /// <seealso cref="https://github.com/DurtyFree/gta-v-data-dumps/blob/master/scenariosCompact.json"/>
        public static void StartScenario(this Ped ped, string scenarioName)
        {
            Natives.TaskStartScenarioInPlace(ped, scenarioName, 0, true);
        }

        /// <summary>
        /// Returns whether the Ped is currently preforming a scenario
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static bool HasScenario(this Ped ped)
        {
            return Natives.PedHasUseScenarioTask<bool>(ped);
        }

        /// <summary>
        /// Sets random props on this <see cref="Rage.Ped"/>
        /// </summary>
        /// <param name="ped">The ped handle.</param>
        /// <seealso cref="https://docs.fivem.net/natives/?_0xC44AA05345C992C6"/>
        public static void RandomizeProps(this Ped ped)
        {
            Natives.SetPedRandomProps(ped);
        }

        /// <summary>
        /// Sets the prop variation on a ped. Components, drawables and textures IDs are related to the ped model.
        /// </summary>
        /// <param name="ped">The ped handle.</param>
        /// <param name="componentId">The component that you want to set.</param>
        /// <param name="drawableId">The drawable id that is going to be set.</param>
        /// <param name="textureId">The texture id of the drawable.</param>
        /// <param name="attach">Attached or not.</param>
        /// <seealso cref="https://docs.fivem.net/natives/?_0x93376B65A266EB5F"/>
        public static void SetPropIndex(this Ped ped, int componentId, int drawableId, int textureId, bool attach)
        {
            Natives.SetPedPropIndex(ped, componentId, drawableId, textureId, attach);
        }

        /// <summary>
        /// Returns the number of drawable prop variations for this <see cref="Rage.Ped"/>
        /// </summary>
        /// <param name="ped">The ped handle.</param>
        /// <returns></returns>
        /// <seealso cref="https://docs.fivem.net/natives/?_0x5FAF9754E789FB47"/>
        public static int GetNumberOfPropDrawableVariations(this Ped ped, int propId)
        {
            return Natives.GetNumberOfPedPropDrawableVariations<int>(ped, propId);
        }

        /// <summary>
        /// Returns the number of texture variations of a drawable prop for this <see cref="Rage.Ped"/>
        /// </summary>
        /// <param name="ped">The ped handle.</param>
        /// <returns></returns>
        /// <seealso cref="https://docs.fivem.net/natives/?_0xA6E7F1CEB523E171"/>
        public static int GetNumberOfPropTextureVariations(this Ped ped, int propId, int drawableId)
        {
            return Natives.GetNumberOfPedPropTextureVariations<int>(ped, propId, drawableId);
        }
    }
}
