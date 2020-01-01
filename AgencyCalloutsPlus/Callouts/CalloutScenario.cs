using AgencyCalloutsPlus.API;
using Rage;
using System;
using System.Xml;

namespace AgencyCalloutsPlus.Callouts
{
    /// <summary>
    /// An object base that represents a callout scenario.
    /// </summary>
    internal abstract class CalloutScenario
    {
        /// <summary>
        /// Gets the SpawnPoint location of this <see cref="CalloutScenario"/>
        /// </summary>
        public LocationInfo SpawnPoint { get; protected set; }

        /// <summary>
        /// Sets up the current CalloutScene vehicles and peds. This method must be called
        /// in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.OnCalloutAccepted()"/> method
        /// </summary>
        public abstract void Setup();

        /// <summary>
        /// Processes the current <see cref="CalloutScenario"/>. This method must be called
        /// in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.Process()"/> method
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// This method is responsible for cleaning up all of the objects in this <see cref="CalloutScenario"/>.
        /// This method must be called in the <see cref="LSPD_First_Response.Mod.Callouts.Callout.End()"/> 
        /// method
        /// </summary>
        public abstract void Cleanup();

        /// <summary>
        /// Fethes a random <see cref="VehicleClass"/> using the probabilites set in the
        /// CalloutMeta.xml
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        protected VehicleClass GetRandomCarTypeFromScenarioNodeList(XmlNodeList nodes)
        {
            // Create a new spawn generator
            var generator = new SpawnGenerator<VehicleSpawn>();

            // Add each item
            foreach (XmlNode n in nodes)
            {
                // Ensure we have attributes
                if (n.Attributes == null)
                {
                    Game.LogTrivial(
                        $"[WARN] AgencyCalloutsPlus: Scenario VehicleTypes item has no attributes in 'CalloutMeta.xml->Sceanrios'"
                    );
                    continue;
                }

                // Try and extract type value
                if (!Enum.TryParse(n.InnerText, out VehicleClass vehicleType))
                {
                    Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract VehicleType value in 'CalloutMeta.xml'");
                    continue;
                }

                // Try and extract probability value
                if (n.Attributes["probability"]?.Value == null || !int.TryParse(n.Attributes["probability"].Value, out int probability))
                {
                    Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract VehicleType probability value in 'CalloutMeta.xml'");
                    continue;
                }

                // Add vehicle type
                generator.Add(new VehicleSpawn() { Probability = probability, Type = vehicleType });
            }

            return generator.Spawn().Type;
        }

        private class VehicleSpawn : ISpawnable
        {
            public int Probability { get; set; }

            public VehicleClass Type { get; set; }
        }
    }
}