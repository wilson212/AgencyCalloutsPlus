using Rage;
using Rage.Native;
using AgencyCalloutsPlus.Integration;

namespace AgencyCalloutsPlus.Extensions
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
            return NativeFunction.Natives.IS_PED_FACING_PED<bool>(ped, otherPed, angle);
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
            StopThePedAPI.SetPedIsDrunk(ped, isDrunk);
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
    }
}
