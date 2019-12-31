using Rage;
using System;

namespace AgencyCalloutsPlus.Extensions
{
    public static class VehicleExtensions
    {
        /// <summary>
        /// Applies random deformity to a vehicle
        /// </summary>
        /// <remarks>Credits to NoNameSet</remarks>
        /// <param name="vehicle"></param>
        /// <param name="radius"></param>
        /// <param name="amount"></param>
        public static void Damage(this Vehicle vehicle, float radius, float amount)
        {
            var model = vehicle.Model;
            model.GetDimensions(out var vector31, out var vector32);
            var num = new Random().Next(10, 45);
            for (var index = 0; index < num; ++index)
            {
                var randomInt1 = MathHelper.GetRandomSingle(vector31.X, vector32.X);
                var randomInt2 = MathHelper.GetRandomSingle(vector31.Y, vector32.Y);
                var randomInt3 = MathHelper.GetRandomSingle(vector31.Z, vector32.Z);
                vehicle.Deform(new Vector3(randomInt1, randomInt2, randomInt3), radius, amount);
            }
        }

        public static void DamageFront(this Vehicle vehicle, float radius, float amount)
        {
            vehicle.Deform(vehicle.FrontPosition, radius, amount);
        }

        public static void DamageRear(this Vehicle vehicle, float radius, float amount)
        {
            vehicle.Deform(vehicle.RearPosition, radius, amount);
        }
    }
}
