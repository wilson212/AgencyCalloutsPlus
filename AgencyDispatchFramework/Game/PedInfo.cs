/*
 * PUMA - PeterU Metadata API

Copyright (c) 2018 LSPDFR-PeterU, 
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of the copyright holder nor the names of its
  contributors may be used to endorse or promote products derived from
  this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// Allows for the retrieval of metadata from <see cref="Rage.Ped"/> objects, such as text and audio
    /// descriptions of the Ped model.
    /// </summary>
    public static class PedInfo
    {
        /// <summary>
        /// Indicates whether the the Agency data has been loaded into memory
        /// </summary>
        private static bool IsInitialized { get; set; } = false;

        /// <summary>
        /// The highest component index to search for drawables and textures. Typically we do not currently see components
        /// we are interested in above 9
        /// </summary>
        private const int MaxComponentIndex = 9;

        /// <summary>
        /// Lookup dictionary for <see cref="Rage.Ped"/> Model names to all possible associated Meta description properties.
        /// </summary>
        public static Dictionary<string, PedModelMeta> PedModelMetaLookup { get; set; }

        /// <summary>
        /// Lookup dictionary for <see cref="Rage.Ped"/> Model names to all possible associated by <see cref="PedVariantGroup"/>.
        /// </summary>
        public static Dictionary<PedVariantGroup, List<string>> PedModelsByVariant { get; set; }

        /// <summary>
        /// Return a string, in LSPDFR Police Scanner Audio file basename format, that describes the passed <paramref name="ped"/>.
        /// </summary>
        /// <param name="ped">The ped to describe</param>
        /// <param name="desiredProperties">
        /// Bitwise mask of the desired PedDescriptionPropertyTypes you wish to have described. 
        /// You may wish to pass fewer than normal if a suspect description is vague, for example
        /// </param>
        /// <returns>Space-separated LSDPFR Police Scanner audio file basenames (e.g. "CLOTHING_LIGHT_SNEAKERS")</returns>
        public static string GetAudioDescription(Ped ped, PedDescriptionPropertyType desiredProperties)
        {
            if (!ped)
            {
                //Log.Warning($"Cannot operate on a null or invalid Ped");
                return null;
            }

            StringBuilder output = new StringBuilder();
            IEnumerable<PedDescriptionProperty> allDescriptionProperties = GetPropertiesToMatch(ped);

            IEnumerable<PedDescriptionProperty> pedDescriptionProperties = allDescriptionProperties as IList<PedDescriptionProperty> ?? allDescriptionProperties.ToList();
            if ((desiredProperties & PedDescriptionPropertyType.RaceSex) != 0)
            {
                IEnumerable<PedDescriptionProperty> raceSexProperties = pedDescriptionProperties.Where(x => x.Type == PedDescriptionPropertyType.RaceSex);
                // we only expect a single RaceSex property, so we will not loop
                output.Append($"A_WITHOUT_HESITATION {raceSexProperties.FirstOrDefault()?.Audio} ");
            }

            if ((desiredProperties & PedDescriptionPropertyType.Build) != 0)
            {
                IEnumerable<PedDescriptionProperty> buildProperties = pedDescriptionProperties.Where(x => x.Type == PedDescriptionPropertyType.Build);
                foreach(PedDescriptionProperty property in buildProperties)
                {
                    output.Append($"200MS_SILENCE {property.Audio} ");
                }
            }

            if ((desiredProperties & PedDescriptionPropertyType.Hair) != 0)
            {
                IEnumerable<PedDescriptionProperty> hairProperties = pedDescriptionProperties.Where(x => x.Type == PedDescriptionPropertyType.Hair);
                foreach (PedDescriptionProperty property in hairProperties)
                {
                    output.Append($"200MS_SILENCE WITH {property.Audio} ");
                }
            }

            if ((desiredProperties & PedDescriptionPropertyType.Clothing) != 0)
            {
                int clothingPropertyCount = pedDescriptionProperties.Count(x => x.Type == PedDescriptionPropertyType.Clothing);
                
                if (clothingPropertyCount > 0)
                {
                    IEnumerable<PedDescriptionProperty> clothingProperties = pedDescriptionProperties.Where(x => x.Type == PedDescriptionPropertyType.Clothing);

                    output.Append("200MS_SILENCE WEARING ");

                    foreach (PedDescriptionProperty property in clothingProperties)
                    {
                        output.Append($" {property.Audio} 200MS_SILENCE ");
                    }
                }
                
            }

            return output.ToString().TrimEnd();

        }

        /// <summary>
        /// Return a string that describes the passed <paramref name="ped"/>.
        /// </summary>
        /// <param name="ped">The ped to describe</param>
        /// <param name="desiredProperties">Bitwise mask of the desired PedDescriptionPropertyTypes you wish to have described. You may wish to pass fewer than normal if a suspect description is vague, for example</param>
        /// <returns>human-readable description of ped</returns>
        public static string GetTextDescription(Ped ped, PedDescriptionPropertyType desiredProperties)
        {
            if (!ped)
            {
                //Log.Warning($"Cannot operate on a null or invalid Ped");
                return null;
            }

            StringBuilder output = new StringBuilder();
            IEnumerable<PedDescriptionProperty> allDescriptionProperties = GetPropertiesToMatch(ped);

            IEnumerable<PedDescriptionProperty> pedDescriptionProperties = allDescriptionProperties as IList<PedDescriptionProperty> ?? allDescriptionProperties.ToList();
            if ((desiredProperties & PedDescriptionPropertyType.RaceSex) != 0)
            {
                IEnumerable<PedDescriptionProperty> raceSexProperties = pedDescriptionProperties.Where(x => x.Type == PedDescriptionPropertyType.RaceSex);
                // we only expect a single RaceSex property, so we will not loop
                if (raceSexProperties.FirstOrDefault() != null)
                {
                    if (raceSexProperties.FirstOrDefault().Text.Contains("hispanic") || raceSexProperties.FirstOrDefault().Text.Contains("asian"))
                    {
                        // title case
                        output.Append(raceSexProperties.FirstOrDefault().Text.Substring(0, 1).ToUpperInvariant() + raceSexProperties.FirstOrDefault().Text.Substring(1) + " ");
                    }
                    else
                    {
                        output.Append(raceSexProperties.FirstOrDefault().Text + " ");
                    }
                }

            }

            if ((desiredProperties & PedDescriptionPropertyType.Build) != 0)
            {
                IEnumerable<PedDescriptionProperty> buildProperties = pedDescriptionProperties.Where(x => x.Type == PedDescriptionPropertyType.Build);
                foreach (PedDescriptionProperty property in buildProperties)
                {
                    // trim last space
                    if (output.Length > 0 && output.ToString().Substring(output.Length - 1, 1) == " ")
                    {
                        output.Remove(output.Length - 1, 1);
                    }

                    output.Append($", {property.Text}, ");
                }
            }

            if ((desiredProperties & PedDescriptionPropertyType.Hair) != 0)
            {
                IEnumerable<PedDescriptionProperty> hairProperties = pedDescriptionProperties.Where(x => x.Type == PedDescriptionPropertyType.Hair);
                foreach (PedDescriptionProperty property in hairProperties)
                {
                    output.Append($"with {property.Text} ");
                }
            }

            if ((desiredProperties & PedDescriptionPropertyType.Clothing) != 0)
            {
                int clothingPropertyCount = pedDescriptionProperties.Count(x => x.Type == PedDescriptionPropertyType.Clothing);

                if (clothingPropertyCount > 0)
                {
                    IEnumerable<PedDescriptionProperty> clothingProperties = pedDescriptionProperties.Where(x => x.Type == PedDescriptionPropertyType.Clothing);

                    output.Append("wearing ");

                    foreach (PedDescriptionProperty property in clothingProperties)
                    {
                        if (!property.Text.ToLowerInvariant().Contains("pants") &&
                            !property.Text.ToLowerInvariant().Contains("shorts") &&
                            !property.Text.ToLowerInvariant().Contains("jeans") &&
                            !property.Text.ToLowerInvariant().Contains("sneakers") &&
                            !property.Text.ToLowerInvariant().Contains("shoes") &&
                            !property.Text.ToLowerInvariant().StartsWith("no") &&
                            !property.Text.ToLowerInvariant().Contains("boots") &&
                            !property.Text.ToLowerInvariant().Contains("tracksuit"))
                        {
                            output.Append("a ");
                        }
                        output.Append($"{property.Text}, ");
                    }
                }

            }

            return output.ToString().TrimEnd(',', ' ');

        }

        /// <summary>
        /// Return all <see cref="PedDescriptionProperty"/> objects that match the variation of the passed <see cref="Rage.Ped"/>
        /// </summary>
        /// <returns>An IEnumerable of PedDescriptionProperty objects that match this variation, or null</returns>
        public static IEnumerable<PedDescriptionProperty> GetPropertiesToMatch(Ped ped)
        {
            if (!ped)
            {
                //Log.Warning($"Cannot operate on a null or invalid Ped");
                return null;
            }

            List<PedDescriptionProperty> output = new List<PedDescriptionProperty>();

            string modelNameCache = ped.Model.Name; // this becomes unavailable if the Ped becomes invalid between check above and time of use

            if (PedModelMetaLookup.Count < 1)
            {
                try
                {
                    Initialize();
                }
                catch (Exception e)
                {
                    Log.Error("Failed to build the lookup dictionary. We will not be able to match properties for this Ped");
                    Log.Exception(e);
                    return null;
                }
            }

            if (!PedModelMetaLookup.ContainsKey(modelNameCache))
            {
                Log.Warning($"{modelNameCache} does not appear in the lookup dictionary");
                return null;
            }

            // get always applicable properties that do not specify an override component
            IEnumerable<PedDescriptionProperty> alwaysApplicableProperties = PedModelMetaLookup[modelNameCache].Properties.Where((x => x.Component == -1));
            output.AddRange(alwaysApplicableProperties);

            // loop over components to get Properties specific to our current drawables and textures for those components
            for (int i = 0; i < MaxComponentIndex; i++)
            {
                if (!ped)
                {
                    Log.Warning($"Ped became invalid while we were trying to get state of component {i} -- results incomplete");
                    return output;
                }

                ped.GetVariation(i, out int drawable, out int drawableTextureIndex);

                output.AddRange(PedModelMetaLookup[modelNameCache].Properties.Where(
                    x => x.Component == i && x.Drawable == drawable && x.Texture == drawableTextureIndex
                ));
            }

            return output;
        }

        /// <summary>
        /// Build the lookup dictionary for Model names to their Meta from the XML files specified in <see cref="Configuration.Directories"/>
        /// </summary>
        public static void Initialize()
        {
            // Set internal flag to initialize just once
            if (IsInitialized) return;
            IsInitialized = true;

            // Initialize dictionaries
            PedModelsByVariant = new Dictionary<PedVariantGroup, List<string>>();
            PedModelMetaLookup = new Dictionary<string, PedModelMeta>();

            // variables for debugging
            int variantsLoaded = 0;
            int metasLoaded = 0;
            int pedsLoaded = 0;

            // Ensure file exists
            var filePath = Path.Combine(Main.ThisPluginFolderPath, "PedModelMeta.xml");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"PedModelMeta.xml does not exist on the filesystem.");
            }

            // Load the ped model meta document
            XmlDocument document = new XmlDocument();
            document.Load(filePath);

            // Load the ped model meta nodes
            foreach (XmlNode node in document.SelectNodes("/PedModelMeta//Ped"))
            {
                PedModelMeta newMeta = null;
                try
                {
                    newMeta = new PedModelMeta(node);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                if (newMeta == null)
                {
                    continue;
                }

                // get the new meta model name and use that as the key in the Dictionary
                string newKey = newMeta.Model.ToUpperInvariant();
                if (PedModelMetaLookup.ContainsKey(newKey))
                {
                    //Configuration.Log($"Lookup dict already contains a key for {newKey}");
                    continue;
                }

                try
                {
                    PedModelMetaLookup.Add(newKey, newMeta);
                    metasLoaded++;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to add created PedModelMeta for {newKey} to the lookup dict. Skipping this one.");
                    Log.Exception(e);
                }
            }

            // Log to make the developers happy
            Log.Info($"Loaded {metasLoaded} PedModelMeta elements into memory");

            // Load the peds.xml
            filePath = Path.Combine(Main.ThisPluginFolderPath, "Peds.xml");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Peds.xml does not exist on the filesystem.");
            }

            // Load document
            document = new XmlDocument();
            document.Load(filePath);

            // Load the ped model meta nodes
            foreach (XmlNode node in document.SelectNodes("/PedVariants//PedVariantGroup"))
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
                PedModelsByVariant.Add(group, new List<string>());
                variantsLoaded++;

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
                    PedModelsByVariant[group].Add(pedName);
                    pedsLoaded++;
                }
            }

            // Log to make the developers happy
            Log.Info($"Loaded {variantsLoaded} PedVariantGroup elements with {pedsLoaded} total Ped elements into memory");
        }
    }
}
