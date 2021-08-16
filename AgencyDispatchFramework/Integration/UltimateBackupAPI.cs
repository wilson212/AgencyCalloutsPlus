using System;
using UltimateBackup.API;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Integration
{
    /// <summary>
    /// Provides API to access UltimateBackup if its running. If UltimateBackup is not running,
    /// calling method in this class is still safe and will not cause an exception
    /// </summary>
    internal static class UltimateBackupAPI
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
                IsRunning = Main.IsLSPDFRPluginRunning("UltimateBackup", new Version("1.8"), out Version ver);
                if (IsRunning)
                {
                    Log.Info($"Detected UltimateBackup v{ver} is running. Registering API functions");
                }
                else
                {
                    Log.Info("Determined that UltimateBackup is not running");
                }
            }
        }
    }
}
