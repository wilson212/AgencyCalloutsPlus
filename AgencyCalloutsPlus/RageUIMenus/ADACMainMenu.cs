using AgencyCalloutsPlus.API;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AgencyCalloutsPlus.RageUIMenus
{
    /// <summary>
    /// Represents a basic <see cref="RAGENativeUI.MenuPool"/> for questioning peds during a callout
    /// </summary>
    internal class ADACMainMenu
    {
        private UIMenu MainUIMenu;
        private UIMenu DispatchUIMenu;

        private MenuPool AllMenus;

        private GameFiber ListenFiber { get; set; }

        #region Main Menu Buttons

        private UIMenuItem CalloutMenuButton {get;set;}

        private UIMenuItem DispatchMenuButton { get; set; }

        private UIMenuListItem TeleportMenuButton { get; set; }

        private UIMenuItem CloseMenuButton { get; set; }

        #endregion Main Menu Buttons

        #region Dispatch Menu Buttons

        private UIMenuItem RequestCallMenuButton { get; set; }

        private UIMenuListItem SetRoleMenuButton { get; set; }

        private UIMenuListItem PatrolAreaMenuButton { get; set; }

        private UIMenuListItem ApplyMenuButton { get; set; }

        #endregion Dispatch Menu Buttons

        public ADACMainMenu()
        {
            // Create main menu
            MainUIMenu = new UIMenu("AD&C+", "~b~Main Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true
            };
            MainUIMenu.SetMenuWidthOffset(12);

            // Create menu buttons
            CalloutMenuButton = new UIMenuItem("Callout Menu", "Opens the current callout menu");
            DispatchMenuButton = new UIMenuItem("Dispatch Menu", "Opens the dispatch menu");
            CloseMenuButton = new UIMenuItem("Close", "Closes the main menu");

            // Cheater menu
            List<dynamic> places = new List<dynamic>()
            {
                "Sandy", "Paleto", "Vespucci", "Rockford", "Downtown", "La Mesa", "Vinewood", "Davis"
            };
            TeleportMenuButton = new UIMenuListItem("Teleport To", "Select police station to teleport to", places);
            TeleportMenuButton.Activated += TeleportMenuButton_Activated;
            
            // Add menu buttons
            MainUIMenu.AddItem(CalloutMenuButton);
            MainUIMenu.AddItem(DispatchMenuButton);
            MainUIMenu.AddItem(TeleportMenuButton);
            MainUIMenu.AddItem(CloseMenuButton);

            // Register for events
            CloseMenuButton.Activated += (s, e) => MainUIMenu.Visible = false;

            // Create dispatch menu
            DispatchUIMenu = new UIMenu("AD&C+", "~b~Dispatch Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true
            };
            DispatchUIMenu.SetMenuWidthOffset(12);

            // Bind Menus
            MainUIMenu.BindMenuToItem(DispatchUIMenu, DispatchMenuButton);

            // Create Dispatch Buttons
            ApplyMenuButton = new UIMenuListItem("Apply", "Appies the changes in this menu");
            RequestCallMenuButton = new UIMenuItem("Request Call", "Requests a nearby call from dispatch");
            SetRoleMenuButton = new UIMenuListItem("Set Role", "Sets the current player role in the department");
            foreach (var role in Enum.GetValues(typeof(OfficerRole)))
            {
                SetRoleMenuButton.Collection.Add(role, role.ToString());
            }

            PatrolAreaMenuButton = new UIMenuListItem("Patrol Area", "Sets the desired patrol area");
            foreach (string value in ZoneInfo.GetRegions())
            {
                PatrolAreaMenuButton.Collection.Add(value, value);
            }

            // Create button events
            RequestCallMenuButton.Activated += RequestCallMenuButton_Activated;

            // Add dispatch menu buttons
            DispatchUIMenu.AddItem(RequestCallMenuButton);
            DispatchUIMenu.AddItem(SetRoleMenuButton);
            DispatchUIMenu.AddItem(PatrolAreaMenuButton);
            DispatchUIMenu.AddItem(ApplyMenuButton);

            // Create menu pool
            AllMenus = new MenuPool
            {
                MainUIMenu,
                DispatchUIMenu
            };

            // Refresh indexes
            AllMenus.RefreshIndex();
        }

        private void RequestCallMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            RequestCallMenuButton.Enabled = false;
            if (!Dispatch.InvokeCalloutForPlayer())
            {
                Game.DisplayNotification("~r~There are no calls available");
            }
        }

        private void TeleportMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            switch (TeleportMenuButton.SelectedValue)
            {
                case "Sandy":
                    World.TeleportLocalPlayer(new Vector3(1848.73f, 3689.98f, 34.27f), false);
                    break;
                case "Paleto":
                    World.TeleportLocalPlayer(new Vector3(-448.22f, 6008.23f, 31.72f), false);
                    break;
                case "Vespucci":
                    World.TeleportLocalPlayer(new Vector3(-1108.18f, -845.18f, 19.32f), false);
                    break;
                case "Rockford":
                    World.TeleportLocalPlayer(new Vector3(-561.65f, -131.65f, 38.21f), false);
                    break;
                case "Downtown":
                    World.TeleportLocalPlayer(new Vector3(50.0654f, -993.0596f, 30f), false);
                    break;
                case "La Mesa":
                    World.TeleportLocalPlayer(new Vector3(826.8f, -1290f, 28.24f), false);
                    break;
                case "Vinewood":
                    World.TeleportLocalPlayer(new Vector3(638.5f, 1.75f, 82.8f), false);
                    break;
                case "Davis":
                    World.TeleportLocalPlayer(new Vector3(360.97f, -1584.70f, 29.29f), false);
                    break;
            }
        }

        internal void BeginListening()
        {
            ListenFiber = GameFiber.StartNew(delegate 
            {
                while (true)
                {
                    // Let other fibers do stuff
                    GameFiber.Yield();

                    // Process menus
                    AllMenus.ProcessMenus();

                    // If player is closed, Wait for key press, then open menu
                    if (!AllMenus.IsAnyMenuOpen() && Globals.IsKeyDownWithModifier(Settings.OpenMenuKey, Settings.OpenMenuModifierKey))
                    {
                        MainUIMenu.Visible = true;
                    }

                    // Disable patrol area selection if not highway patrol
                    if (DispatchUIMenu.Visible)
                    {
                        PatrolAreaMenuButton.Enabled = (Dispatch.PlayerAgency.AgencyType == AgencyType.HighwayPatrol);
                        RequestCallMenuButton.Enabled = Dispatch.CanInvokeCalloutForPlayer();
                    }
                }
            });
        }
    }
}
