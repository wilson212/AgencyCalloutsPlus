using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
