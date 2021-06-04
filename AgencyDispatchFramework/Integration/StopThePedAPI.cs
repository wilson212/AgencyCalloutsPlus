using Rage;
using StopThePed.API;
using System;

namespace AgencyDispatchFramework.Integration
{
    /// <summary>
    /// Provides API to access StopThePed if its running. If StopThePed is not running,
    /// calling method in this class is still safe and will not cause an exception
    /// </summary>
    internal static class StopThePedAPI
    {
        /// <summary>
        /// Indicates whether Stop The Ped is running
        /// </summary>
        public static bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Checks to see if StopThePed is running
        /// </summary>
        public static void Initialize()
        {
            if (!IsRunning)
            {
                IsRunning = Main.IsLSPDFRPluginRunning("StopThePed", new Version("1.9.2.5"), out Version ver);
                if (IsRunning)
                {
                    Log.Info($"Detected StopThePed v{ver} is running. Registering API functions");
                }
                else
                {
                    Log.Info("Determined that StopThePed is not running");
                }
            }
        }
        
        /// <summary>
        /// Sets whether the specified <see cref="Ped" /> is drunk or not
        /// </summary>
        /// <param name="ped">The subject ped</param>
        /// <param name="value">A value indicating whether the ped is drunk</param>
        public static void SetPedIsDrunk(Ped ped, bool value)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.setPedAlcoholOverLimit(ped, value);
        }

        /// <summary>
        /// Gets a value indicating whether a <see cref="Ped"/> is drunk per StopThePed
        /// </summary>
        /// <param name="ped">The subject Ped</param>
        /// <returns></returns>
        public static bool IsPedDrunk(Ped ped)
        {
            // Ensure we are running!
            if (!IsRunning) return false;
            
            return Functions.isPedAlcoholOverLimit(ped);
        }

        /// <summary>
        /// Sets whether the specified <see cref="Ped" /> is under the influence of drugs
        /// </summary>
        /// <param name="ped">The subject ped</param>
        /// <param name="value">A value indicating whether the ped is under the influence</param>
        public static void SetPedIsDrugInfluenced(Ped ped, bool value)
        {
            // Ensure we are running!
            if (!IsRunning) return;

            Functions.setPedUnderDrugsInfluence(ped, true);
        }

        /// <summary>
        /// ets a value indicating whether a <see cref="Ped"/> is under the influence of drugs per StopThePed
        /// </summary>
        /// <param name="ped">The subject ped</param>
        /// <returns></returns>
        public static bool IsPedUnderDrugInfluence(Ped ped)
        {
            // Ensure we are running!
            if (!IsRunning) return false;

            return Functions.isPedUnderDrugsInfluence(ped);
        }
    }
}
