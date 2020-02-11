using Rage;
using Rage.Native;

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
        public static void MakeMissionPed(this Ped ped)
        {
            ped.BlockPermanentEvents = true;
            ped.IsPersistent = true;
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
    }
}
