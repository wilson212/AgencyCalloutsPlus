using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Location;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// Represents a basic <see cref="RAGENativeUI.MenuPool"/> for questioning peds during a callout
    /// </summary>
    internal class ADACMainMenu
    {
        private UIMenu MainUIMenu;
        private UIMenu DispatchUIMenu;
        private UIMenu PatrolUIMenu;
        private UIMenu SettingsUIMenu;

        private MenuPool AllMenus;

        private GameFiber ListenFiber { get; set; }

        #region Main Menu Buttons

        private UIMenuItem DispatchMenuButton { get; set; }

        private UIMenuItem PatrolSettingsMenuButton { get; set; }

        private UIMenuItem ModSettingsMenuButton { get; set; }

        private UIMenuListItem TeleportMenuButton { get; set; }

        private UIMenuItem CloseMenuButton { get; set; }

        #endregion Main Menu Buttons

        #region Dispatch Menu Buttons

        private UIMenuListItem OfficerStatusMenuButton { get; set; }

        private UIMenuItem RequestQueueMenuButton { get; set; }

        private UIMenuItem RequestCallMenuButton { get; set; }

        private UIMenuItem EndCallMenuButton { get; set; }

        #endregion Dispatch Menu Buttons

        #region Patrol Menu Buttons

        private UIMenuListItem SetRoleMenuButton { get; set; }

        private UIMenuListItem PatrolAreaMenuButton { get; set; }

        private UIMenuListItem DivisionMenuButton { get; set; }

        private UIMenuListItem UnitTypeMenuButton { get; set; }

        private UIMenuListItem BeatMenuButton { get; set; }

        #endregion Patrol Menu Buttons

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
            DispatchMenuButton = new UIMenuItem("Dispatch Menu", "Opens the dispatch menu");
            PatrolSettingsMenuButton = new UIMenuItem("Patrol Settings", "Opens the patrol settings menu");
            ModSettingsMenuButton = new UIMenuItem("Mod Settings", "Opens the patrol settings menu");
            CloseMenuButton = new UIMenuItem("Close", "Closes the main menu");

            // Cheater menu
            List<dynamic> places = new List<dynamic>()
            {
                "Sandy", "Paleto", "Vespucci", "Rockford", "Downtown", "La Mesa", "Vinewood", "Davis"
            };
            TeleportMenuButton = new UIMenuListItem("Teleport To", "Select police station to teleport to", places);
            TeleportMenuButton.Activated += TeleportMenuButton_Activated;
            
            // Add menu buttons
            MainUIMenu.AddItem(DispatchMenuButton);
            MainUIMenu.AddItem(PatrolSettingsMenuButton);
            MainUIMenu.AddItem(ModSettingsMenuButton);
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

            // Create Dispatch Buttons
            OfficerStatusMenuButton = new UIMenuListItem("Status", "Alerts dispatch to your current status. Click to set.");
            RequestCallMenuButton = new UIMenuItem("Request Call", "Requests a nearby call from dispatch");
            RequestQueueMenuButton = new UIMenuItem("Queue Crime Stats", "Requests current crime statistics from dispatch");
            EndCallMenuButton = new UIMenuItem("Code 4", "Tells dispatch the current call is complete.");

            // Fill List Items
            foreach (var role in Enum.GetValues(typeof(OfficerStatus)))
            {
                OfficerStatusMenuButton.Collection.Add(role, role.ToString());
            }

            // Create button events
            OfficerStatusMenuButton.Activated += (s, e) =>
            {       
                var item = (OfficerStatus)OfficerStatusMenuButton.SelectedValue;
                Dispatch.SetPlayerStatus(item);

                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch and Callouts+",
                    "~b~Status Update",
                    "Status changed to: " + Enum.GetName(typeof(OfficerStatus), item)
                );
            };
            RequestCallMenuButton.Activated += RequestCallMenuButton_Activated;
            EndCallMenuButton.Activated += (s, e) => Dispatch.EndPlayerCallout();
            RequestQueueMenuButton.Activated += RequestQueueMenuButton_Activated;

            // Add dispatch menu buttons
            DispatchUIMenu.AddItem(OfficerStatusMenuButton);
            DispatchUIMenu.AddItem(RequestQueueMenuButton);
            DispatchUIMenu.AddItem(RequestCallMenuButton);
            DispatchUIMenu.AddItem(EndCallMenuButton);

            // Create patrol menu
            PatrolUIMenu = new UIMenu("AD&C+", "~b~Patrol Settings Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true
            };
            DispatchUIMenu.SetMenuWidthOffset(12);

            // Setup Patrol Menu
            SetRoleMenuButton = new UIMenuListItem("Primary Role", "Sets your role in the department. This will determine that types of calls you will recieve. Click to set.");
            foreach (var role in Enum.GetValues(typeof(OfficerRole)))
            {
                SetRoleMenuButton.Collection.Add(role, role.ToString());
            }

            PatrolAreaMenuButton = new UIMenuListItem("Patrol Area", "Sets your patrol area. Click to set.");
            foreach (string value in ZoneInfo.GetRegions())
            {
                PatrolAreaMenuButton.Collection.Add(value, value);
            }

            DivisionMenuButton = new UIMenuListItem("Division", "Sets your division number. Click to set.");
            DivisionMenuButton.Activated += (s, e) => Dispatch.SetPlayerDivisionId((int)DivisionMenuButton.SelectedValue);
            for (int i = 1; i < 11; i++)
            {
                string value = i.ToString();
                DivisionMenuButton.Collection.Add(i, value);
            }

            // Find and set index
            var index = DivisionMenuButton.Collection.IndexOf(Settings.AudioDivision);
            if (index >= 0)
            {
                DivisionMenuButton.Index = index;
            }

            BeatMenuButton = new UIMenuListItem("Beat", "Sets your Beat number. Click to set.");
            BeatMenuButton.Activated += (s, e) => Dispatch.SetPlayerBeat((int)BeatMenuButton.SelectedValue);
            for (int i = 1; i < 25; i++)
            {
                string value = i.ToString();
                BeatMenuButton.Collection.Add(i, value);
            }

            // Find and set index
            index = BeatMenuButton.Collection.IndexOf(Settings.AudioBeat);
            if (index >= 0)
            {
                BeatMenuButton.Index = index;
            }

            UnitTypeMenuButton = new UIMenuListItem("Unit Type", "Sets your unit type. Click to set.");
            UnitTypeMenuButton.Activated += (s, e) => Dispatch.SetPlayerUnitType(UnitTypeMenuButton.Index + 1);
            foreach (string value in Dispatch.LAPDphonetic)
            {
                UnitTypeMenuButton.Collection.Add(value, value);
            }

            // Find and set index
            index = UnitTypeMenuButton.Collection.IndexOf(Settings.AudioUnitType);
            if (index >= 0)
            {
                UnitTypeMenuButton.Index = index;
            }

            // Add patrol menu buttons
            PatrolUIMenu.AddItem(SetRoleMenuButton);
            PatrolUIMenu.AddItem(DivisionMenuButton);
            PatrolUIMenu.AddItem(UnitTypeMenuButton);
            PatrolUIMenu.AddItem(BeatMenuButton);
            PatrolUIMenu.AddItem(PatrolAreaMenuButton);

            // Bind Menus
            MainUIMenu.BindMenuToItem(DispatchUIMenu, DispatchMenuButton);
            MainUIMenu.BindMenuToItem(PatrolUIMenu, PatrolSettingsMenuButton);

            // Create menu pool
            AllMenus = new MenuPool
            {
                MainUIMenu,
                DispatchUIMenu,
                PatrolUIMenu
            };

            // Refresh indexes
            AllMenus.RefreshIndex();
            MainUIMenu.OnMenuChange += MainUIMenu_OnMenuChange;
        }

        private void MainUIMenu_OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            if (!forward) return;

            if (newMenu == DispatchUIMenu)
            {
                var status = Dispatch.GetPlayerStatus();
                int index = OfficerStatusMenuButton.Collection.IndexOf(status);
                OfficerStatusMenuButton.Index = index;
            }
        }

        private void RequestQueueMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            var builder = new StringBuilder("Status: ");

            // Add status
            switch (Dispatch.CurrentCrimeLevel)
            {
                case CrimeLevel.VeryLow:
                    builder.Append("~g~It is currently very slow~w~");
                    break;
                case CrimeLevel.Low:
                    builder.Append("~g~It is slower than usual~w~");
                    break;
                case CrimeLevel.Moderate:
                    builder.Append("~b~Calls are coming in steady~w~");
                    break;
                case CrimeLevel.High:
                    builder.Append("~y~It is currently busy~w~");
                    break;
                case CrimeLevel.VeryHigh:
                    builder.Append("~o~We have lots of calls coming in~w~");
                    break;
            }

            // Add each call priority data
            for (int i = 1; i < 5; i++)
            {
                var calls = Dispatch.GetCallList(i);
                int c1c = calls.Where(x => x.CallStatus == CallStatus.Created || x.NeedsMoreOfficers).Count();
                int c1b = calls.Where(x => x.CallStatus == CallStatus.Dispatched).Count() + c1c;
                builder.Append($"<br />- Priority {i} Calls: ~b~{c1b} ~w~(~g~{c1c} ~w~Avail)");
            }

            // Display the information to the player
            Rage.Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "Agency Dispatch and Callouts+",
                "~b~Current Crime Statistics",
                builder.ToString()
            );
        }

        private void RequestCallMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            RequestCallMenuButton.Enabled = false;
            if (!Dispatch.InvokeNextCalloutForPlayer(out bool dispatched))
            {
                Rage.Game.DisplayNotification("~r~You are currently not available for calls!");
            }
            else
            {
                if (!dispatched)
                {
                    Rage.Game.DisplayNotification("There are no calls currently available. ~g~Dispatch will send you the next call that comes in");
                }
            }
        }

        private void TeleportMenuButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            Vector3 pos = Vector3.Zero;
            switch (TeleportMenuButton.SelectedValue)
            {
                case "Sandy":
                    pos = new Vector3(1848.73f, 3689.98f, 34.27f);
                    break;
                case "Paleto":
                    pos = new Vector3(-448.22f, 6008.23f, 31.72f);
                    break;
                case "Vespucci":
                    pos = new Vector3(-1108.18f, -845.18f, 19.32f);
                    break;
                case "Rockford":
                    pos = new Vector3(-561.65f, -131.65f, 38.21f);
                    break;
                case "Downtown":
                    pos = new Vector3(50.0654f, -993.0596f, 30f);
                    break;
                case "La Mesa":
                    pos = new Vector3(826.8f, -1290f, 28.24f);
                    break;
                case "Vinewood":
                    pos = new Vector3(638.5f, 1.75f, 82.8f);
                    break;
                case "Davis":
                    pos = new Vector3(360.97f, -1584.70f, 29.29f);
                    break;
            }

            // Just in case
            if (pos == Vector3.Zero) return;

            var player = Rage.Game.LocalPlayer;
            if (player.Character.IsInAnyVehicle(false))
            {
                // Find a safe vehicle location
                if (pos.GetClosestVehicleSpawnPoint(out SpawnPoint p))
                {
                    World.TeleportLocalPlayer(p, false);
                }
                else
                {
                    var location = World.GetNextPositionOnStreet(pos);
                    World.TeleportLocalPlayer(location, false);
                }
            }
            else
            {
                // Teleport player
                World.TeleportLocalPlayer(pos, false);
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

                    // If menu is closed, Wait for key press, then open menu
                    if (!AllMenus.IsAnyMenuOpen() && Keyboard.IsKeyDownWithModifier(Settings.OpenMenuKey, Settings.OpenMenuModifierKey))
                    {
                        MainUIMenu.Visible = true;
                    }

                    // Disable patrol area selection if not highway patrol
                    if (DispatchUIMenu.Visible)
                    {
                        // Disable the Callout menu button if player is not on a callout
                        EndCallMenuButton.Enabled = Dispatch.PlayerActiveCall != null;
                        RequestCallMenuButton.Enabled = Dispatch.CanInvokeCalloutForPlayer();
                    }
                    else if (PatrolUIMenu.Visible)
                    {
                        PatrolAreaMenuButton.Enabled = (Dispatch.PlayerAgency.AgencyType == AgencyType.HighwayPatrol);
                    }
                }
            });
        }
    }
}
