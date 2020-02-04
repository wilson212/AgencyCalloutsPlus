using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.API
{
    public class PedInfo
    {
        /// <summary>
        /// Creates a random ped at the specified position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Ped CreateRandomPed(Vector3 position)
        {
            //return NativeFunction.Natives.CREATE_RANDOM_PED<Ped>(position.X, position.Y, position.Z);
            return new Ped(position);
        }
    }
}
