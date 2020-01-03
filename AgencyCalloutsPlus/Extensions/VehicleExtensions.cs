using Rage;
using Rage.Native;
using System;

namespace AgencyCalloutsPlus.Extensions
{
    public static class VehicleExtensions
    {
        /// <summary>
        /// Applies random deformity all around a vehicle
        /// </summary>
        /// <remarks>Credits to NoNameSet</remarks>
        /// <param name="vehicle"></param>
        /// <param name="radius"></param>
        /// <param name="amount"></param>
        public static void Damage(this Vehicle vehicle, float radius, float amount)
        {
            var model = vehicle.Model;
            model.GetDimensions(out var vector31, out var vector32);
            var num = new CryptoRandom().Next(10, 45);
            for (var index = 0; index < num; ++index)
            {
                var randomInt1 = MathHelper.GetRandomSingle(vector31.X, vector32.X);
                var randomInt2 = MathHelper.GetRandomSingle(vector31.Y, vector32.Y);
                var randomInt3 = MathHelper.GetRandomSingle(vector31.Z, vector32.Z);
                vehicle.Deform(new Vector3(randomInt1, randomInt2, randomInt3), radius, amount);
            }
        }

        public static void DeformFront(this Vehicle vehicle, float radius, float amount)
        {
            // Get dimensions we want to deform
            var dimensions = vehicle.Model.Dimensions;
            var halfWidth = (dimensions.X / 2) * 0.6f;
            var halfLength = dimensions.Y / 2;
            var halfHeight = (dimensions.Z / 2) * 0.7f;

            // Apply random deformities within the ranges of the model
            var num = new CryptoRandom().Next(15, 45);
            for (var index = 0; index < num; ++index)
            {
                // We use half values here, since this is an OFFSET from center
                var randomInt1 = MathHelper.GetRandomSingle(-halfWidth, halfWidth); // Full width
                var randomInt2 = MathHelper.GetRandomSingle(halfLength * 0.85f, halfLength); // Front end
                var randomInt3 = MathHelper.GetRandomSingle(-halfHeight, 0); // Lower half height
                vehicle.Deform(new Vector3(randomInt1, randomInt2, randomInt3), radius, amount);
            }
        }

        public static void DeformRear(this Vehicle vehicle, float radius, float amount)
        {
            // Get dimensions we want to deform
            var dimensions = vehicle.Model.Dimensions;
            var halfWidth = (dimensions.X / 2) * 0.6f; // Ignore far left and right of dimensions
            var halfLength = dimensions.Y / 2;
            var halfHeight = (dimensions.Z / 2) * 0.7f; // Ignore roof and gound of dimensions

            // Apply random deformities within the ranges of the model
            var num = new CryptoRandom().Next(15, 45);
            for (var index = 0; index < num; ++index)
            {
                // We use negative values here, since this is an OFFSET from center
                var randomInt1 = MathHelper.GetRandomSingle(-halfWidth, halfWidth); // Full width
                var randomInt2 = MathHelper.GetRandomSingle(-halfLength, -halfLength + (halfLength * 0.07f)); // Rear end
                var randomInt3 = MathHelper.GetRandomSingle(-halfHeight, 0); // Lower half height
                vehicle.Deform(new Vector3(randomInt1, randomInt2, randomInt3), radius, amount);
            }
        }

        /// <summary>
        /// Checks via CVehicleModelInfo
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="extraId"></param>
        /// <returns></returns>
        public static bool DoesExtraExist(this Vehicle vehicle, int extraId)
        {
            return NativeFunction.Natives.DOES_EXTRA_EXIST<bool>(vehicle, extraId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="extraId"></param>
        /// <param name="enabled"></param>
        public static void SetExtraEnabled(this Vehicle vehicle, int extraId, bool enabled)
        {
            NativeFunction.Natives.SET_VEHICLE_EXTRA(vehicle, extraId, !enabled);
        }

        /// <summary>
        /// Sets the livery index for this vehicle
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="liveryIndex"></param>
        public static void SetLivery(this Vehicle vehicle, int liveryIndex)
        {
            NativeFunction.Natives.SET_VEHICLE_LIVERY(vehicle, liveryIndex);
        }
    }
}
