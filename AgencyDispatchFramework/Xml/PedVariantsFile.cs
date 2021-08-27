using AgencyDispatchFramework.Game;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    public class PedVariantsFile : XmlFileBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public PedVariantsFile(string filePath) : base(filePath)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void Parse(out int pedsLoaded, out int variantsLoaded)
        {
            int psLoaded = 0;
            int vsLoaded = 0;

            // Load the ped model meta nodes
            foreach (XmlNode node in Document.SelectNodes("/PedVariants//PedVariantGroup"))
            {
                var name = node.Attributes["name"]?.Value;
                if (String.IsNullOrEmpty(name))
                {
                    Log.Warning("PedVariantGroup does not contain a 'name' addtribute in Peds.xml");
                    continue;
                }

                // Parse name
                if (!Enum.TryParse(name, out PedVariantGroup group))
                {
                    Log.Warning($"PedVariantGroup with name '{name}' is not a valid group in Peds.xml");
                    continue;
                }

                // Add the variant group
                GamePed.PedModelsByVariant.Add(group, new List<string>());
                vsLoaded++;

                // Load the ped model meta nodes
                foreach (XmlNode pedNode in node.SelectNodes("Ped"))
                {
                    var pedName = pedNode.InnerText;
                    if (String.IsNullOrWhiteSpace(name))
                    {
                        Log.Warning("PedVariantGroup -> Ped element does not contain a string value in Peds.xml");
                        continue;
                    }

                    // Ensure model name is valid
                    if (!Model.PedModels.Contains(pedName))
                    {
                        Log.Warning($"PedInfo.Initialize(): Ped model '{pedName}' is not valid within RagePluginHook");
                        continue;
                    }

                    // Add ped
                    GamePed.PedModelsByVariant[group].Add(pedName);            
                    psLoaded++;
                }
            }

            pedsLoaded = psLoaded;
            variantsLoaded = vsLoaded;
        }
    }
}
