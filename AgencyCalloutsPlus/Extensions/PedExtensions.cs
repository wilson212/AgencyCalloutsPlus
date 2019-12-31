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
        public static bool IsFacingPed(this Ped ped, Ped otherPed, float angle)
        {
            return NativeFunction.Natives.IS_PED_FACING_PED<bool>(ped, otherPed, angle);
        }
    }
}
