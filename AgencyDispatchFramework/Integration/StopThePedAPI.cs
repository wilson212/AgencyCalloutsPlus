using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using Rage;
using StopThePed.API;
using System;
using System.Collections.Generic;
using System.Text;

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
            if (IsRunning) return;
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

        /// <summary>
        /// Injects search items onto the ped that can be recognized by StopThePed, including the contraband
        /// added from <see cref="LSPD_First_Response.Mod.API.Functions.AddPedContraband"/> method.
        /// </summary>
        /// <remarks>
        /// This method can be called multiple times on the same ped, and items will only be added to
        /// the inventory, not overwite it.
        /// </remarks>
        /// <param name="ped">The <see cref="Ped"/> we are adding contraband to</param>
        /// <param name="items">The list of contraband items to add to the inventory</param>
        /// <param name="removeStpItems">
        /// If true, removes and items added by StopThePed. This must be set to true the very first this method is
        /// called on a <see cref="Ped"/>, or the STP items will not be removed.
        /// </param>
        public static void InjectContrabandItems(Ped ped, List<ContrabandItem> items, bool removeStpItems)
        {
            // Make sure we have items
            if (items.Count == 0)
                return;

            // Ensure Ped is not null
            if (ped == null) throw new ArgumentNullException(nameof(Ped));

            // Convert to dictionary
            var metaData = (MetadataObject)ped.Metadata;
            string stpItems = metaData.Contains("searchPed") ? ped.Metadata.searchPed : String.Empty;
            bool myItemsInjected = metaData.Contains("ContrabandInjected") ? ped.Metadata.ContrabandInjected : false;
            bool stpItemsInjected = !String.IsNullOrWhiteSpace(stpItems);

            // Check to see if search items are already injected by STP?
            if (IsRunning)
            {
                if (!myItemsInjected)
                {
                    if (removeStpItems)
                    {
                        // Clear STP added items
                        ped.Metadata.searchPed = String.Empty;
                    }
                    else if (!stpItemsInjected)
                    {
                        // Inject items
                        Functions.injectPedSearchItems(ped);
                    }
                }
            }
            else if (!myItemsInjected)
            {
                // Make sure if STP is not running, this property is set at least
                ped.Metadata.searchPed = String.Empty;
            }

            // Get a list of LSPDFR items
            bool isEmpty = String.IsNullOrWhiteSpace(stpItems);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var last = (i + 1 == items.Count);
                var appendAnd = (items.Count > 1 && last && isEmpty);

                // Add preceeding comma if this isnt the first item
                builder.AppendIf(i != 0 && !appendAnd, ", ");

                // Add "and" if this is the last item
                builder.AppendIf(appendAnd, " and ");

                // Add item
                switch (item.Type)
                {
                    case LSPD_First_Response.Engine.Scripting.Entities.ContrabandType.Contraband:
                        builder.Append($", ~y~{item.Name}~s~");
                        break;
                    case LSPD_First_Response.Engine.Scripting.Entities.ContrabandType.Identification:
                        builder.Append($", ~g~{item.Name}~s~");
                        break;
                    case LSPD_First_Response.Engine.Scripting.Entities.ContrabandType.Misc:
                        builder.Append($", ~g~{item.Name}~s~");
                        break;
                    case LSPD_First_Response.Engine.Scripting.Entities.ContrabandType.Narcotics:
                        builder.Append($", ~r~{item.Name}~s~");
                        break;
                    case LSPD_First_Response.Engine.Scripting.Entities.ContrabandType.Weapon:
                        builder.Append($", ~r~{item.Name}~s~");
                        break;
                }
            }

            // Insert existing string at the end
            string val = ped.Metadata.searchPed;
            builder.AppendIf(!isEmpty, val);

            // Save items
            ped.Metadata.searchPed = builder.ToString();
            ped.Metadata.ContrabandInjected = true;
        }

        /// <summary>
        /// Clears all the contraband items added by StopThePed
        /// </summary>
        /// <param name="ped"></param>
        public static void ClearPedContraband(Ped ped)
        {
            ped.Metadata.searchPed = String.Empty;
        }
    }
}
