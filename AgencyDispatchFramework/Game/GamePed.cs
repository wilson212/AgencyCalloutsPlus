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
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Integration;
using AgencyDispatchFramework.Xml;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// A class that provides useful methods and properties to better manage a <see cref="Rage.Ped"/>
    /// while using this framework.
    /// </summary>
    public class GamePed
    {
        /// <summary>
        /// Gets the player <see cref="Rage.Ped"/> instance
        /// </summary>
        public static Ped Player => Rage.Game.LocalPlayer.Character;

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
        internal static Dictionary<string, PedModelMeta> PedModelMetaLookup { get; set; }

        /// <summary>
        /// Lookup dictionary for <see cref="Rage.Ped"/> Model names to all possible associated by <see cref="PedVariantGroup"/>.
        /// </summary>
        public static Dictionary<PedVariantGroup, List<string>> PedModelsByVariant { get; set; }

        /// <summary>
        /// Gets the <see cref="Rage.Ped"/> this object references
        /// </summary>
        public Ped Ped { get; private set; }

        /// <summary>
        /// Gets the <see cref="LSPD_First_Response.Engine.Scripting.Entities.Persona"/> for this <see cref="Rage.Ped"/>
        /// </summary>
        public Persona Persona { get; private set; }

        /// <summary>
        /// Gets the title of this <see cref="Rage.Ped"/>
        /// </summary>
        public string GenderTitle => (Persona.Gender == LSPD_First_Response.Gender.Female) ? "Ms" : "Mr";

        /// <summary>
        /// Gets or sets the <see cref="Rage.Ped"/>s demeanor
        /// </summary>
        public PedDemeanor Demeanor { get; set; } = PedDemeanor.Happy;

        /// <summary>
        /// Gets a list of <see cref="PedContraband"/> items added to this instance.
        /// </summary>
        private List<ContrabandItem> ContrabandItems { get; set; }

        /// <summary>
        /// Gets a list of <see cref="PedContraband"/> items added to this instance, waiting to be sync'd with StopThePed
        /// This list is cleared each time <see cref="SaveContraband"/> is called, preventing duplicate items being added.
        /// </summary>
        private List<ContrabandItem> TemporaryItems { get; set; }

        /// <summary>
        /// Gets or sets whether this <see cref="Rage.Ped"/> is drunk
        /// </summary>
        public bool IsDrunk
        {
            get => Ped.GetIsDrunk();
            set => Ped.SetIsDrunk(value);
        }

        /// <summary>
        /// Gets or sets the BAC level of this <see cref="Rage.Ped"/>. Value should range from
        /// 0.00 to 0.20.
        /// </summary>
        public float AlcoholLevel
        {
            get => Ped.GetAlcoholLevel();
            set => Ped.SetAlcoholLevel(value);
        }

        /// <summary>
        /// Gets or sets whether this <see cref="Rage.Ped"/> is high on drugs
        /// </summary>
        public bool IsHigh
        {
            get => Ped.GetIsUnderDrugInfluence();
            set => Ped.SetIsUnderDrugInfluence(value);
        }

        /// <summary>
        /// Gets whether the <see cref="Rage.Ped"/> is armed with a loaded weapon
        /// </summary>
        public bool HasLoadedWeapon
        {
            get => Ped.Inventory.HasLoadedWeapon;
        }

        /// <summary>
        /// Indicates whether the <see cref="Rage.Ped"/> has any items in thier inventory marked
        /// as <see cref="ContrabandType.Contraband"/>. This does not consider any contraband added
        /// by StopThePed nor LSPDFR if StopThePed is running.
        /// </summary>
        public bool HasContraband
        {
            get => (StopThePedAPI.IsRunning)
                ? ContrabandItems.Any(x => x.Type == ContrabandType.Contraband)
                : Functions.GetPedContraband(Ped).Any(x => x.Type == ContrabandType.Contraband);
        }

        /// <summary>
        /// Indicates whether the <see cref="Rage.Ped"/> has any items in thier inventory marked
        /// as <see cref="ContrabandType.Narcotics"/>. This does not consider any contraband added
        /// by StopThePed nor LSPDFR if StopThePed is running.
        /// </summary>
        public bool HasNarcotics
        {
            get => (StopThePedAPI.IsRunning)
                ? ContrabandItems.Any(x => x.Type == ContrabandType.Narcotics)
                : Functions.GetPedContraband(Ped).Any(x => x.Type == ContrabandType.Narcotics);
        }

        /// <summary>
        /// Indicates whether the <see cref="Rage.Ped"/> has any items in thier inventory marked
        /// as <see cref="ContrabandType.Weapon"/>. This does not consider any contraband added
        /// by StopThePed nor LSPDFR if StopThePed is running. Unlike <see cref="HasLoadedWeapon"/>, 
        /// This propert does not consider any weapons in the <see cref="Rage.Ped.Inventory"/>.
        /// </summary>
        public bool HasWeaponContraband
        {
            get => (StopThePedAPI.IsRunning)
                ? ContrabandItems.Any(x => x.Type == ContrabandType.Weapon)
                : Functions.GetPedContraband(Ped).Any(x => x.Type == ContrabandType.Weapon);
        }

        /// <summary>
        /// Gets a valid indicating whether a <see cref="Rage.Ped"/> is valid in the <see cref="Rage.World"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid => Ped?.Exists() ?? false;

        /// <summary>
        /// Creates a new instance of <see cref="GamePed"/>
        /// </summary>
        /// <remarks>
        /// Private constructor to allow once instance of each <see cref="Rage.Ped"/>
        /// </remarks>
        /// <param name="ped"></param>
        private GamePed(Ped ped)
        {
            Ped = ped ?? throw new ArgumentNullException(nameof(ped));
            Persona = Functions.GetPersonaForPed(ped);
            ContrabandItems = new List<ContrabandItem>();
            TemporaryItems = new List<ContrabandItem>();

            // Set default meta data
            Ped.Metadata.GamePedInstance = this;
            Ped.Metadata.Contraband = ContrabandItems;
            Ped.Metadata.ContrabandInjected = false;
            ped.Metadata.BAC = 0f;
        }

        /// <summary>
        /// Adds a weapon item to be found when searching the <see cref="Rage.Ped"/>.
        /// This item will show in red text. Note that this does not add the actual
        /// weapon in the Peds inventory <see cref="Rage.Ped.Inventory"/>
        /// </summary>
        /// <param name="name">The name of the item, as displayed in game</param>
        public void AddWeapon(string name)
        {
            // Inject also into the peds inventory
            Functions.AddPedContraband(Ped, ContrabandType.Weapon, name);

            // Create item
            var item = new ContrabandItem(name, ContrabandType.Weapon);
            ContrabandItems.Add(item);
            TemporaryItems.Add(item);
        }

        /// <summary>
        /// Adds a weapon item to be found when searching the <see cref="Rage.Ped"/>, 
        /// and adds the weapon to thier inventory also. This item will show in red text when 
        /// searching the ped.
        /// </summary>
        /// <param name="name">The name of the item, as displayed in game in the search results window</param>
        /// <param name="weaponId">The name of the weapon</param>
        /// <param name="ammoCount">The amount of ammo to give the <see cref="Ped"/> for this weapon</param>
        /// <param name="equipNow">If true, the <see cref="Ped"/> will equip the weapon immediatly</param>
        public WeaponDescriptor AddWeapon(string name, WeaponDescriptor weaponId, short ammoCount, bool equipNow = false)
        {
            // Inject also into the peds inventory
            Functions.AddPedContraband(Ped, ContrabandType.Weapon, name);

            // Create item
            var item = new ContrabandItem(name, ContrabandType.Weapon);
            ContrabandItems.Add(item);
            TemporaryItems.Add(item);

            // Add weapon to ped inventory
            return Ped.Inventory.GiveNewWeapon(weaponId, ammoCount, equipNow);
        }

        /// <summary>
        /// Adds a narcotic item to be found when searching the <see cref="Rage.Ped"/>.
        /// This item will show in red text.
        /// </summary>
        /// <param name="name">The name of the item, as displayed in game</param>
        public void AddNarcotics(string name)
        {
            // Inject also into the peds inventory
            Functions.AddPedContraband(Ped, ContrabandType.Narcotics, name);

            // Create item
            var item = new ContrabandItem(name, ContrabandType.Narcotics);
            ContrabandItems.Add(item);
            TemporaryItems.Add(item);
        }

        /// <summary>
        /// Adds a contraband item to be found when searching the <see cref="Rage.Ped"/>.
        /// This item will show in yellow text.
        /// </summary>
        /// <param name="name">The name of the item, as displayed in game</param>
        public void AddContraband(string name)
        {
            // Inject also into the peds inventory
            Functions.AddPedContraband(Ped, ContrabandType.Contraband, name);

            // Create item
            var item = new ContrabandItem(name, ContrabandType.Contraband);
            ContrabandItems.Add(item);
            TemporaryItems.Add(item);
        }

        /// <summary>
        /// Adds a misc item to be found when searching the <see cref="Rage.Ped"/>
        /// </summary>
        /// <param name="name">The name of the item, as displayed in game</param>
        public void AddMiscItem(string name)
        {
            // Inject also into the peds inventory
            Functions.AddPedContraband(Ped, ContrabandType.Misc, name);

            // Create item
            var item = new ContrabandItem(name, ContrabandType.Misc);
            ContrabandItems.Add(item);
            TemporaryItems.Add(item);
        }

        /// <summary>
        /// Adds a contraband item to be found when searching the <see cref="Rage.Ped"/>
        /// </summary>
        /// <param name="name">The name of the item, as displayed in game</param>
        /// <param name="type">The contraband type</param>
        public void AddContrabandByType(string name, ContrabandType type)
        {
            // Inject also into the peds inventory
            Functions.AddPedContraband(Ped, type, name);

            // Create item
            var item = new ContrabandItem(name, type);
            ContrabandItems.Add(item);
            TemporaryItems.Add(item);
        }

        /// <summary>
        /// Gets a list of contraband items added within this instance
        /// </summary>
        /// <returns></returns>
        public List<ContrabandItem> GetContrabandItems()
        {
            return ContrabandItems;
        }

        /// <summary>
        /// Syncs the contraband items added with StopThePed's search plugin. This method
        /// can be called multiple times and will not duplicate items added.
        /// </summary>
        public void SaveContraband()
        {
            if (TemporaryItems.Count > 0)
            {
                // Inject
                StopThePedAPI.InjectContrabandItems(Ped, TemporaryItems, false);

                // Clear old
                TemporaryItems.Clear();
            }
        }

        /// <summary>
        /// Clears all items added to the <see cref="Rage.Ped"/>s based on the parameters set below.
        /// This method should be called before adding any items to the <see cref="Rage.Ped"/>
        /// </summary>
        /// <param name="thisInstance">if true, clears all contraband added by the <see cref="GamePed"/> class on this <see cref="Rage.Ped"/></param>
        /// <param name="lspdfr">if true, clears all contraband added by LSPDFR also</param>
        /// <param name="stopThePed">if true, clears all contraband added by StopThePed also</param>
        public void ClearContraband(bool thisInstance, bool lspdfr, bool stopThePed)
        {
            if (lspdfr)
            {
                Functions.ClearPedContraband(Ped);
            }

            if (stopThePed)
            {
                StopThePedAPI.ClearPedContraband(Ped);
            }

            if (thisInstance)
            {
                ContrabandItems.Clear();
                TemporaryItems.Clear();
                Ped.Metadata.ContrabandInjected = false;
            }
        }

        /// <summary>
        /// Return a string, in LSPDFR Police Scanner Audio file basename format, that describes the passed <paramref name="ped"/>.
        /// </summary>
        /// <param name="desiredProperties">
        /// Bitwise mask of the desired PedDescriptionPropertyTypes you wish to have described. 
        /// You may wish to pass fewer than normal if a suspect description is vague, for example
        /// </param>
        /// <returns>Space-separated LSDPFR Police Scanner audio file basenames (e.g. "CLOTHING_LIGHT_SNEAKERS")</returns>
        public string GetAudioDescription(PedDescriptionPropertyType desiredProperties)
        {
            StringBuilder output = new StringBuilder();
            IEnumerable<PedDescriptionProperty> allDescriptionProperties = GetDescriptionProperties();

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
                foreach (PedDescriptionProperty property in buildProperties)
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
        /// <param name="desiredProperties">Bitwise mask of the desired PedDescriptionPropertyTypes you wish to have described. You may wish to pass fewer than normal if a suspect description is vague, for example</param>
        /// <returns>human-readable description of ped</returns>
        public string GetTextDescription(PedDescriptionPropertyType desiredProperties)
        {
            StringBuilder output = new StringBuilder();
            IEnumerable<PedDescriptionProperty> allDescriptionProperties = GetDescriptionProperties();

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
        public IEnumerable<PedDescriptionProperty> GetDescriptionProperties()
        {
            List<PedDescriptionProperty> output = new List<PedDescriptionProperty>();

            string modelNameCache = Ped.Model.Name; // this becomes unavailable if the Ped becomes invalid between check above and time of use

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
                if (!Ped)
                {
                    Log.Warning($"Ped became invalid while we were trying to get state of component {i} -- results incomplete");
                    return output;
                }

                Ped.GetVariation(i, out int drawable, out int drawableTextureIndex);

                output.AddRange(PedModelMetaLookup[modelNameCache].Properties.Where(
                    x => x.Component == i && x.Drawable == drawable && x.Texture == drawableTextureIndex
                ));
            }

            return output;
        }

        /// <summary>
        /// Returns the full name of the <see cref="Rage.Ped"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Persona.FullName;
        }

        #region Static Methods

        /// <summary>
        /// Static constructor
        /// </summary>
        static GamePed()
        {
            // Initialize dictionaries
            PedModelsByVariant = new Dictionary<PedVariantGroup, List<string>>();
            PedModelMetaLookup = new Dictionary<string, PedModelMeta>();
        }


        /// <summary>
        /// Build the lookup dictionary for Model names to their Meta from the XML files specified in <see cref="Configuration.Directories"/>
        /// </summary>
        public static void Initialize()
        {
            // Set internal flag to initialize just once
            if (IsInitialized) return;
            IsInitialized = true;

            // Load the PedModelMeta.xml
            string filePath = Path.Combine(Main.FrameworkFolderPath, "PedModelMeta.xml");
            using (var file = new PedModelMetaFile(filePath))
            {
                // Parse the file
                int metasLoaded = file.Parse();

                // Log to make the developers happy
                Log.Info($"Loaded {metasLoaded} PedModelMeta elements into memory");
            }

            // Load the Peds.xml
            filePath = Path.Combine(Main.FrameworkFolderPath, "Peds.xml");
            using (var file = new PedVariantsFile(filePath))
            {
                // Parse the file
                file.Parse(out int pedsLoaded, out int variantsLoaded);

                // Log to make the developers happy
                Log.Info($"Loaded {variantsLoaded} PedVariantGroup elements with {pedsLoaded} total Ped elements into memory");
            }
        }

        #endregion Static Methods

        /// <summary>
        /// Enables casting to a <see cref="GamePed"/> from a <see cref="Rage.Ped"/>
        /// </summary>
        /// <param name="p">The <see cref="Rage.Ped"/> instance</param>
        public static implicit operator GamePed(Ped p)
        {
            var metaData = (MetadataObject)p.Metadata;
            return metaData.Contains("GamePedInstance") ? p.Metadata.GamePedInstance : new GamePed(p);
        }

        /// <summary>
        /// Enables casting to a <see cref="Rage.Ped"/>
        /// </summary>
        /// <param name="p">The <see cref="GamePed"/> instance</param>
        public static implicit operator Ped(GamePed p)
        {
            return p.Ped;
        }
    }
}
