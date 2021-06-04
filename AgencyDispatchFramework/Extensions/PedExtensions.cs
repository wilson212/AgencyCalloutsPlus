using Rage;
using AgencyDispatchFramework.Integration;
using static Rage.Native.NativeFunction;

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
        /// Sets whether the <see cref="Ped"/> is under the influence of alcohol
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="isDrunk"></param>
        public static void SetIsDrunk(this Ped ped, bool isDrunk)
        {
            if (StopThePedAPI.IsRunning)
            {
                StopThePedAPI.SetPedIsDrunk(ped, isDrunk);
            }
            else
            {
                // Determine a random alcohol level
                var level = new CryptoRandom().Next(800, 1700);
                float fl = level / 1000f;

                string GetAnimaionString()
                {
                    if (level > 1300)
                    {
                        return "MOVE_M@DRUNK@VERYDRUNK";
                    }
                    else if (level > 1100)
                    {
                        return "MOVE_M@DRUNK@MODERATEDRUNK";
                    }
                    else
                    {
                        return "MOVE_M@DRUNK@SLIGHTLYDRUNK";
                    }
                }

                var animation = GetAnimaionString();
                if (!Natives.HasAnimSetLoaded(animation))
                {
                    Natives.RequestAnimSet(animation);
                }

                // Play
                Natives.SetPedMovementClipset(ped, animation, 1f);
                ped.Metadata.DrunkLevel = fl;
            }
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
        /// Gets a value indicating whether this <see cref="Ped"/> is under the influence of alcohol
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static bool GetIsDrunk(this Ped ped)
        {
            return StopThePedAPI.IsPedDrunk(ped);
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
    }
}
