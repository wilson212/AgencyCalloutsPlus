using Rage;
using StopThePed.API;
using System;

namespace AgencyCalloutsPlus.Integration
{
    /// <summary>
    /// Provides a nice API to access StopThePed if its running
    /// </summary>
    internal static class StopThePedAPI
    {
        /// <summary>
        /// Indicates whether Stop The Ped is running
        /// </summary>
        public static bool IsRunning { get; private set; } = false;

        public static void Initialize()
        {
            if (!IsRunning)
            {
                IsRunning = Globals.IsLSPDFRPluginRunning("StopThePed", new Version("1.9.2.5"));
                if (IsRunning)
                {
                    Log.Info("Detected StopThePed is running. Registering API functions");
                }
                else
                {
                    Log.Info("Determined that StopThePed is not running");
                }
            }
        }

        public static void SetPedIsDrunk(Ped ped, bool value)
        {
            // Ensure we are running!
            if (!IsRunning) return;

            Functions.setPedAlcoholOverLimit(ped, value);
        }

        public static bool IsPedDrunk(Ped ped)
        {
            // Ensure we are running!
            if (!IsRunning) return false;
            
            return Functions.isPedAlcoholOverLimit(ped);
        }

        public static void SetPedIsDrugInfluenced(Ped ped, bool value)
        {
            // Ensure we are running!
            if (!IsRunning) return;

            Functions.setPedUnderDrugsInfluence(ped, true);
        }

        public static bool IsPedUnderDrugInfluence(Ped ped)
        {
            // Ensure we are running!
            if (!IsRunning) return false;

            return Functions.isPedUnderDrugsInfluence(ped);
        }
    }
}
