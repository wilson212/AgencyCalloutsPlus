using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyCalloutsPlus.API
{
    public class VehicleInfo : ISpawnable
    {
        #region Static Methods

        /// <summary>
        /// Indicates whether the the Agency data has been loaded into memory
        /// </summary>
        private static bool IsInitialized { get; set; } = false;

        /// <summary>
        /// Contains a list of Spawnable vehicles
        /// </summary>
        private static Dictionary<VehicleClass, ProbabilityGenerator<VehicleInfo>> Vehicles { get; set; }

        /// <summary>
        /// Loads the vehicle data from the Vehicles.xml
        /// </summary>
        public static void Initialize()
        {
            // Set internal flag to initialize just once
            if (IsInitialized) return;

            // Set internals
            IsInitialized = true;
            int itemsAdded = 0;

            // Initialize vehicle types
            Vehicles = new Dictionary<VehicleClass, ProbabilityGenerator<VehicleInfo>>(8);
            foreach (var type in Enum.GetValues(typeof(VehicleClass)))
            {
                Vehicles.Add((VehicleClass)type, new ProbabilityGenerator<VehicleInfo>());
            }

            // Load XML document
            string path = Path.Combine(Main.PluginFolderPath, "Vehicles.xml");
            XmlDocument document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // Add vehicles
            foreach (VehicleClass category in Enum.GetValues(typeof(VehicleClass)))
            {
                // Grab node
                XmlNode categoryNode = document.DocumentElement.SelectSingleNode(category.ToString("F"));

                // Skip and log errors
                if (categoryNode == null)
                {
                    Log.Debug($"VehicleInfo.Initialize(): Unable to load vehicle data for vehicle type '{category}'");
                    continue;
                }

                // No data?
                if (!categoryNode.HasChildNodes) continue;

                // Itterate through items
                foreach (XmlNode n in categoryNode.SelectNodes("Vehicle"))
                {
                    // Fetch model name
                    string modelName = n.InnerText;
                    if (String.IsNullOrWhiteSpace(modelName))
                    {
                        Log.Warning("VehicleInfo.Initialize(): Vehicle item has no model name in Vehicles.xml");
                        continue;
                    }

                    // Ensure we have attributes
                    if (n.Attributes == null)
                    {
                        Log.Warning($"VehicleInfo.Initialize(): Vehicle item has no attributes '{modelName}'");
                        continue;
                    }

                    // Try and extract probability value
                    if (n.Attributes["probability"]?.Value == null || !int.TryParse(n.Attributes["probability"].Value, out int probability))
                    {
                        Log.Warning($"VehicleInfo.Initialize(): Unable to extract vehicle probability value for '{modelName}'");
                        continue;
                    }

                    // Create the vehicle
                    var item = new VehicleInfo(modelName, category, probability);

                    // Fetch the vehicle seat count
                    if (n.Attributes["seats"]?.Value != null && int.TryParse(n.Attributes["seats"].Value, out int seats))
                    {
                        item.NumberOfSeats = seats;
                    }

                    // Fetch the vehicle manufacturer
                    if (!String.IsNullOrWhiteSpace(n.Attributes["manufacturer"]?.Value))
                    {
                        item.Manufacturer = n.Attributes["manufacturer"].Value;
                    }

                    // Fetch the vehicle name
                    if (!String.IsNullOrWhiteSpace(n.Attributes["name"]?.Value))
                    {
                        item.FriendlyName = n.Attributes["name"].Value;
                    }

                    // Add vehicle
                    Vehicles[category].Add(item);
                    itemsAdded++;
                }
            }

            // Clean up
            document = null;

            // Log
            Log.Info($"Loaded {itemsAdded} vehicles into memory'");
        }

        /// <summary>
        /// Gets a random vehicle indicated by type
        /// </summary>
        /// <param name="type"></param>
        /// <returns>returns a <see cref="VehicleInfo"/> on success, or null on failure</returns>
        public static VehicleInfo GetRandomVehicleByType(VehicleClass type)
        {
            // Try and spawn a vehicle
            if (Vehicles[type].TrySpawn(out VehicleInfo vehicle))
            {
                // Ensure vehicle model exists within Rage
                if (Model.VehicleModels.Contains(vehicle.ModelName))
                {
                    //Log.Debug($"VehicleInfo.GetRandomVehicleByType(): returning {vehicle.ModelName}");
                    return vehicle;
                }

                // Log this as an error
                Log.Error($"VehicleInfo.GetRandomVehicleByType(): Vehicle model '{vehicle.ModelName}' does not exist in Rage.Model.VehicleModels");

                // Attempt 10 times to find another vehicle
                for (int i = 0; i < 10; i++)
                {
                    // Spawn another
                    Vehicles[type].TrySpawn(out vehicle);
                    if (Model.VehicleModels.Contains(vehicle.ModelName))
                    {
                        //Log.Debug($"VehicleInfo.GetRandomVehicleByType(): returning {vehicle.ModelName}");
                        return vehicle;
                    }
                }

                // 
                Log.Error($"VehicleInfo.GetRandomVehicleByType(): Unable to find valid vehicle model for class {type} in Model.VehicleModels after 10 attempts");
                return null;
            }
            else
            {
                Log.Warning($"VehicleInfo.GetRandomVehicleByType(): Unable to find any vehicle model of class '{type}'");
                return null;
            }
        }

        /// <summary>
        /// Gets a random vehicle indicated by using a random type provided
        /// </summary>
        /// <param name="types">An array of acceptable <see cref="VehicleType"/>s</param>
        /// <returns>returns a <see cref="VehicleInfo"/> on success, or null on failure</returns>
        public static VehicleInfo GetRandomVehicleByTypes(VehicleClass[] types)
        {
            var rando = new CryptoRandom();
            var type = types[rando.Next(types.Length - 1)];

            // Try and spawn a vehicle
            if (Vehicles[type].TrySpawn(out VehicleInfo vehicle))
            {
                return GetRandomVehicleByType(type);
            }

            return null;
        }

        /// <summary>
        /// Gets a vehicle type description by <see cref="VehicleClass"/>
        /// </summary>
        /// <param name="vehicleType"></param>
        /// <returns></returns>
        public static string GetVehicleTypeDescription(VehicleClass vehicleType)
        {
            switch (vehicleType)
            {
                case VehicleClass.Compact:
                case VehicleClass.Coupe:
                case VehicleClass.Muscle:
                case VehicleClass.Sedan:
                case VehicleClass.Sport:
                case VehicleClass.SportClassic:
                    return "car";
                case VehicleClass.Emergency:
                    return "truck";
                case VehicleClass.SUV:
                case VehicleClass.Van:
                    return "suv";
                case VehicleClass.Boat:
                    return "boat";
                case VehicleClass.Cycle:
                    return "bike";
                case VehicleClass.Motorcycle:
                    return "motorcycle";
                default:
                    return "vehicle";
            }
        }

        #endregion Static Methods

        #region Instance methods

        /// <summary>
        /// Gets the model name of the vehicle
        /// </summary>
        public string ModelName { get; private set; }

        /// <summary>
        /// Gets the <see cref="API.VehicleClass"/> of the vehicle
        /// </summary>
        public VehicleClass VehicleType { get; internal set; }

        /// <summary>
        /// The probability of this <see cref="VehicleInfo"/> being spawned in comparison to other vehicles
        /// </summary>
        public int Probability { get; internal set; }

        /// <summary>
        /// Gets the number of seats in the vehicle
        /// </summary>
        public int NumberOfSeats { get; internal set; } = 2;

        /// <summary>
        /// Gets the manufacturer name
        /// </summary>
        public string Manufacturer { get; internal set; } = String.Empty;

        /// <summary>
        /// Gets the friendly name of the vehicle
        /// </summary>
        public string FriendlyName { get; internal set; } = String.Empty;

        /// <summary>
        /// Creates a new instance of <see cref="VehicleInfo"/>
        /// </summary>
        /// <param name="modelName"></param>
        internal VehicleInfo(string modelName, VehicleClass type, int probability)
        {
            this.ModelName = modelName.TrimStart('-');
            this.VehicleType = type;
            this.Probability = probability;
        }

        #endregion
    }
}
