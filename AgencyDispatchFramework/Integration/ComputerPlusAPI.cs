using ComputerPlus;
using ComputerPlus.API;
using Rage;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Integration
{
    /// <summary>
    /// Provides a nice API to access Computer+ if its running
    /// </summary>
    internal class ComputerPlusAPI
    {
        /// <summary>
        /// Indicates whether Computer+ is running
        /// </summary>
        public static bool IsRunning { get; private set; } = false;

        public static void Initialize()
        {
            if (!IsRunning)
            {
                IsRunning = Main.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.4.1.1"));
                if (IsRunning)
                {
                    Log.Info("Detected ComputerPlus is running. Registering CAD interfaces");
                    Functions.RegisterInterface("Computer Aided Dispatch", "Brexin212", () => new CADMainGwenForm());
                }
                else
                {
                    Log.Info("Determined that ComputerPlus is not running");
                }
            }
        }

        public static Guid CreateCallout(string CallName, string ShortName, Vector3 Location, int ResponseType, string Description = "", int CallStatus = 2, List<Ped> CallPeds = null, List<Vehicle> CallVehicles = null)
        {
            // Ensure we are running!
            if (!IsRunning) return Guid.Empty;

            // Create callout
            return Functions.CreateCallout(
                new CalloutData(CallName, ShortName, Location, (EResponseType)ResponseType, Description, (ECallStatus)CallStatus, CallPeds, CallVehicles)
            );
        }

        public static void UpdateCalloutStatus(Guid ID, int Status)
        {
            // Ensure we are running!
            if (!IsRunning) return;

            Functions.UpdateCalloutStatus(ID, (ECallStatus)Status);
        }

        public static void UpdateCalloutDescription(Guid ID, string Description)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.UpdateCalloutDescription(ID, Description);
        }

        public static void SetCalloutStatusToAtScene(Guid ID)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.SetCalloutStatusToAtScene(ID);
        }

        public static void ConcludeCallout(Guid ID)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.ConcludeCallout(ID);
        }

        public static void CancelCallout(Guid ID)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.CancelCallout(ID);
        }

        public static void SetCalloutStatusToUnitResponding(Guid ID)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.SetCalloutStatusToUnitResponding(ID);
        }

        public static void AddPedToCallout(Guid ID, Ped PedToAdd)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.AddPedToCallout(ID, PedToAdd);
        }

        public static void AddUpdateToCallout(Guid ID, string Update)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.AddUpdateToCallout(ID, Update);
        }

        public static void AddVehicleToCallout(Guid ID, Vehicle VehicleToAdd)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.AddVehicleToCallout(ID, VehicleToAdd);
        }

        public static void AssignCallToAIUnit(Guid ID)
        {
            // Ensure we are running!
            if (!IsRunning) return;
            Functions.AssignCallToAIUnit(ID);
        }
    }
}
