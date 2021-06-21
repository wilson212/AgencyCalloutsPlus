using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Integration;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Returns the full name of the <see cref="Rage.Ped"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Persona.FullName;
        }

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
