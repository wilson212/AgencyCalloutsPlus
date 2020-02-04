using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private bool HasModifier;

        private GameFiber ListenFiber { get; set; }

        private UIMenuItem CalloutMenuButton {get;set;}

        private UIMenuItem DispatchMenuButton { get; set; }

        private UIMenuListItem TeleportMenuButton { get; set; }

        private UIMenuItem CloseMenuButton { get; set; }

        public ADACMainMenu()
        {
            // Create main menu
            MainUIMenu = new UIMenu("ADAC+ Menu", "Main Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true
            };
            MainUIMenu.SetMenuWidthOffset(12);

            // Create dispatch menu
            DispatchUIMenu = new UIMenu("ADAC+ Menu", "Dispatch Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true
            };
            DispatchUIMenu.SetMenuWidthOffset(12);

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

            // Bind Menus
            MainUIMenu.BindMenuToItem(DispatchUIMenu, DispatchMenuButton);

            // Register for events
            CloseMenuButton.Activated += (s, e) => MainUIMenu.Visible = false;

            // Create menu pool
            AllMenus = new MenuPool
            {
                MainUIMenu,
                DispatchUIMenu
            };

            // Refresh indexes
            AllMenus.RefreshIndex();

            // Internal variables
            HasModifier = (Settings.OpenCalloutInteractionMenuModifierKey != Keys.None);
        }

        private void TeleportMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            switch (TeleportMenuButton.SelectedValue)
            {
                case "Sandy":
                    World.TeleportLocalPlayer(new Vector3(1848.73f, 3689.98f, 34.27f), true);
                    break;
                case "Paleto":
                    World.TeleportLocalPlayer(new Vector3(-448.22f, 6008.23f, 31.72f), true);
                    break;
                case "Vespucci":
                    World.TeleportLocalPlayer(new Vector3(-1108.18f, -845.18f, 19.32f), true);
                    break;
                case "Rockford":
                    World.TeleportLocalPlayer(new Vector3(-561.65f, -131.65f, 38.21f), true);
                    break;
                case "Downtown":
                    World.TeleportLocalPlayer(new Vector3(50.0654f, -993.0596f, 30f), true);
                    break;
                case "La Mesa":
                    World.TeleportLocalPlayer(new Vector3(826.8f, -1290f, 28.24f), true);
                    break;
                case "Vinewood":
                    World.TeleportLocalPlayer(new Vector3(638.5f, 1.75f, 82.8f), true);
                    break;
                case "Davis":
                    World.TeleportLocalPlayer(new Vector3(360.97f, -1584.70f, 29.29f), true);
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

                    // If player is close to and facing another ped, show press Y to open menu
                    if (!MainUIMenu.Visible)
                    {
                        // Is modifier key pressed
                        if (HasModifier)
                        {
                            if (!Game.IsKeyDown(Settings.OpenCalloutInteractionMenuModifierKey))
                            {
                                return;
                            }
                        }

                        // Wait for key press, then open menu
                        if (Game.IsKeyDown(Settings.OpenCalloutInteractionMenuKey))
                        {
                            MainUIMenu.Visible = true;
                        }
                    }
                }
            });
        }
    }
}
