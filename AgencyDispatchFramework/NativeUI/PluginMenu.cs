using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Dispatching.Assignments;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using static Rage.Native.NativeFunction;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// Represents a basic <see cref="MenuPool"/> for questioning peds during a callout
    /// </summary>
    internal partial class PluginMenu
    {
        private UIMenu MainUIMenu;
        private UIMenu DispatchUIMenu;
        private UIMenu PatrolUIMenu;
        private UIMenu LocationsUIMenu;

        private UIMenu RoadShoulderUIMenu;
        private UIMenu RoadShoulderFlagsUIMenu;
        private UIMenu RoadShoulderBeforeFlagsUIMenu;
        private UIMenu RoadShoulderAfterFlagsUIMenu;
        private UIMenu RoadShoulderSpawnPointsUIMenu;

        private UIMenu ResidenceUIMenu;
        private UIMenu ResidenceSpawnPointsUIMenu;
        private UIMenu ResidenceFlagsUIMenu;

        private MenuPool AllMenus;

        private GameFiber ListenFiber { get; set; }

        #region Main Menu Buttons

        private UIMenuItem DispatchMenuButton { get; set; }

        private UIMenuItem PatrolSettingsMenuButton { get; set; }

        private UIMenuItem ModSettingsMenuButton { get; set; }

        private UIMenuItem LocationsMenuButton { get; set; }

        private UIMenuListItem TeleportMenuButton { get; set; }

        private UIMenuItem CloseMenuButton { get; set; }

        #endregion Main Menu Buttons

        #region Dispatch Menu Buttons

        private UIMenuCheckboxItem OutOfServiceButton { get; set; }

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

        #region Locations Menu Buttons

        private UIMenuItem RoadShouldersButton { get; set; }

        private UIMenuItem ResidenceButton { get; set; }

        #endregion

        /// <summary>
        /// flagcode => handle
        /// </summary>
        private Dictionary<int, int> CheckpointHandles { get; set; }

        private SpawnPoint NewLocationPosition { get; set; }

        private int NewLocationCheckpointHandle { get; set; }

        /// <summary>
        /// Indicates to stop processing the controls of this menu while the keyboard is open
        /// </summary>
        internal bool IsKeyboardOpen { get; set; }

        /// <summary>
        /// Creates a new isntance of <see cref="PluginMenu"/>
        /// </summary>
        public PluginMenu()
        {
            // Create main menu
            MainUIMenu = new UIMenu("ADF", "~b~Main Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true
            };
            MainUIMenu.WidthOffset = 12;

            // Create menu buttons
            DispatchMenuButton = new UIMenuItem("Dispatch Menu", "Opens the dispatch menu");
            PatrolSettingsMenuButton = new UIMenuItem("Patrol Settings", "Opens the patrol settings menu");
            ModSettingsMenuButton = new UIMenuItem("Mod Settings", "Opens the patrol settings menu");
            LocationsMenuButton = new UIMenuItem("Add Location", "Allows you to add new locations for callouts");
            CloseMenuButton = new UIMenuItem("Close", "Closes the main menu");

            // Cheater menu
            var places = new List<string>()
            {
                "Sandy", "Paleto", "Vespucci", "Rockford", "Downtown", "La Mesa", "Vinewood", "Davis"
            };
            TeleportMenuButton = new UIMenuListItem("Teleport To", "Select police station to teleport to", places);
            TeleportMenuButton.Activated += TeleportMenuButton_Activated;
            
            // Add menu buttons
            MainUIMenu.AddItem(DispatchMenuButton);
            MainUIMenu.AddItem(PatrolSettingsMenuButton);
            MainUIMenu.AddItem(ModSettingsMenuButton);
            MainUIMenu.AddItem(LocationsMenuButton);
            MainUIMenu.AddItem(TeleportMenuButton);
            MainUIMenu.AddItem(CloseMenuButton);

            // Register for events
            CloseMenuButton.Activated += (s, e) => MainUIMenu.Visible = false;

            // Create Dispatch Menu
            BuildDispatchMenu();

            // Create Patrol Menu
            BuildPatrolMenu();

            // Create RoadShoulders Menu
            BuildRoadShouldersMenu();

            // Create Residences Menu
            BuildResidencesMenu();

            // Create RoadShoulder Menu
            BuildLocationsMenu();

            // Bind Menus
            MainUIMenu.BindMenuToItem(DispatchUIMenu, DispatchMenuButton);
            MainUIMenu.BindMenuToItem(PatrolUIMenu, PatrolSettingsMenuButton);
            MainUIMenu.BindMenuToItem(LocationsUIMenu, LocationsMenuButton);

            // Create menu pool
            AllMenus = new MenuPool
            {
                MainUIMenu,
                DispatchUIMenu,
                PatrolUIMenu,
                LocationsUIMenu,
                RoadShoulderUIMenu,
                RoadShoulderFlagsUIMenu,
                RoadShoulderBeforeFlagsUIMenu,
                RoadShoulderAfterFlagsUIMenu,
                RoadShoulderSpawnPointsUIMenu,
                ResidenceUIMenu,
                ResidenceFlagsUIMenu,
                ResidenceSpawnPointsUIMenu
            };

            // Refresh indexes
            AllMenus.RefreshIndex();
            MainUIMenu.OnMenuChange += MainUIMenu_OnMenuChange;
        }

        private void BuildLocationsMenu()
        {
            // Create patrol menu
            LocationsUIMenu = new UIMenu("ADF", "~b~Add Location Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Setup Buttons
            RoadShouldersButton = new UIMenuItem("New Road Shoulder", "Creates a new Road Shoulder location");
            RoadShouldersButton.Activated += RoadShouldersButton_Activated;

            ResidenceButton = new UIMenuItem("New Residence", "Creates a new residence location");
            ResidenceButton.Activated += ResidenceButton_Activated;

            // Add buttons
            LocationsUIMenu.AddItem(RoadShouldersButton);
            LocationsUIMenu.AddItem(ResidenceButton);

            // Bind buttons
            LocationsUIMenu.BindMenuToItem(RoadShoulderUIMenu, RoadShouldersButton);
            LocationsUIMenu.BindMenuToItem(ResidenceUIMenu, ResidenceButton);
        }

        private void BuildPatrolMenu()
        {
            // Create patrol menu
            PatrolUIMenu = new UIMenu("ADF", "~b~Patrol Settings Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Setup Patrol Menu
            SetRoleMenuButton = new UIMenuListItem("Primary Role", "Sets your role in the department. This will determine that types of calls you will recieve. Click to set.");
            foreach (var role in Enum.GetValues(typeof(OfficerRole)))
            {
                SetRoleMenuButton.Collection.Add(role, role.ToString());
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
        }

        private void BuildDispatchMenu()
        {
            // Create dispatch menu
            DispatchUIMenu = new UIMenu("ADF", "~b~Dispatch Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Create Dispatch Buttons
            OutOfServiceButton = new UIMenuCheckboxItem("Out Of Service", false);
            OutOfServiceButton.CheckboxEvent += OutOfServiceButton_CheckboxEvent;
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
                    "Agency Dispatch Framework",
                    "~b~Status Update",
                    "Status changed to: " + Enum.GetName(typeof(OfficerStatus), item)
                );
            };
            RequestCallMenuButton.Activated += RequestCallMenuButton_Activated;
            EndCallMenuButton.Activated += (s, e) => Dispatch.EndPlayerCallout();
            RequestQueueMenuButton.Activated += RequestQueueMenuButton_Activated;

            // Add dispatch menu buttons
            DispatchUIMenu.AddItem(OutOfServiceButton);
            DispatchUIMenu.AddItem(OfficerStatusMenuButton);
            DispatchUIMenu.AddItem(RequestQueueMenuButton);
            DispatchUIMenu.AddItem(RequestCallMenuButton);
            DispatchUIMenu.AddItem(EndCallMenuButton);
        }

        #region Events

        /// <summary>
        /// Method called when a UIMenu item is clicked. The OnScreen keyboard is displayed,
        /// and the text that is typed will be saved in the description
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedItem"></param>
        private void DispayKeyboard_SetDescription(UIMenu sender, UIMenuItem selectedItem)
        {
            GameFiber.StartNew(() =>
            {
                // Open keyboard
                Natives.DisplayOnscreenKeyboard(1, "FMMC_KEY_TIP8", "", selectedItem.Description, "", "", "", 48);
                IsKeyboardOpen = true;
                sender.InstructionalButtonsEnabled = false;
                Rage.Game.IsPaused = true;

                // Loop until the keyboard closes
                while (true)
                {
                    int status = Natives.UpdateOnscreenKeyboard<int>();
                    switch (status)
                    {
                        case 2: // Cancelled
                        case -1: // Not active
                            IsKeyboardOpen = false;
                            sender.InstructionalButtonsEnabled = true;
                            Rage.Game.IsPaused = false;
                            return;
                        case 0:
                            // Still editing
                            break;
                        case 1:
                            // Finsihed
                            string message = Natives.GetOnscreenKeyboardResult<string>();
                            selectedItem.Description = message;
                            selectedItem.RightBadge = UIMenuItem.BadgeStyle.Tick;
                            sender.InstructionalButtonsEnabled = true;
                            IsKeyboardOpen = false;
                            Rage.Game.IsPaused = false;
                            return;
                    }

                    GameFiber.Yield();
                }
            });
        }

        private void OutOfServiceButton_CheckboxEvent(UIMenuCheckboxItem sender, bool Checked)
        {
            var player = Dispatch.PlayerUnit;
            if (Checked)
            {
                player.Assignment = new OutOfService();

                // @todo change status to OutOfService
            }
            else
            {
                player.Assignment = null;
            }
        }

        private void RoadShoulderUIMenu_OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            // Are we backing out of this main menu
            if (!forward && oldMenu == RoadShoulderUIMenu)
            {
                ResetCheckPoints();
            }
        }

        private void RoadShoulderFlagsUIMenu_OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            // Are we backing out of this menu?
            if (!forward && oldMenu == RoadShoulderFlagsUIMenu)
            {
                // We must have at least 1 item checked
                if (RoadShouldFlagsItems.Any(x => x.Value.Checked))
                {
                    RoadShoulderFlagsButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
                }
                else
                {
                    RoadShoulderFlagsButton.RightBadge = UIMenuItem.BadgeStyle.None;
                }
            }
        }

        private void ResidenceUIMenu_OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            // Reset checkpoint handles
            if (newMenu == LocationsUIMenu || oldMenu == LocationsUIMenu)
            {
                ResetCheckPoints();
            }
        }

        private void ResidenceFlagsUIMenu_OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
        {
            // Are we backing out of this menu?
            if (!forward && oldMenu == ResidenceFlagsUIMenu)
            {
                // We must have at least 1 item checked
                if (ResidenceFlagsItems.Any(x => x.Value.Checked))
                {
                    ResidenceFlagsButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
                }
                else
                {
                    ResidenceFlagsButton.RightBadge = UIMenuItem.BadgeStyle.None;
                }
            }
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
                "Agency Dispatch Framework",
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

        #endregion Events

        /// <summary>
        /// Ensures a node path exists in an <see cref="XmlDocument"/>
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="rootName">The root node name in the XmlDocument</param>
        /// <param name="paths"></param>
        /// <returns></returns>
        private static XmlNode UpdateOrCreateXmlNode(XmlDocument document, string rootName, params string[] paths)
        {
            // Walk
            XmlNode node = document.SelectSingleNode(rootName);
            foreach (string path in paths)
            {
                XmlNode child = node.SelectSingleNode(path);
                if (child != null)
                {
                    node = child;
                }
                else
                {
                    child = document.CreateElement(path);
                    node.AppendChild(child);
                    node = child;
                }
            }

            return node;
        }

        /// <summary>
        /// Creates a {Positions} node with child {SpawnPoint} nodes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="document"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        private static XmlElement CreatePositionsNodeWithSpawnPoints<T>(XmlDocument document, Dictionary<T, MyUIMenuItem<SpawnPoint>> items)
        {
            // Add spawn points
            var positionsNode = document.CreateElement("Positions");
            foreach (MyUIMenuItem<SpawnPoint> p in items.Values)
            {
                var spawnPointNode = document.CreateElement("SpawnPoint");

                var idAttr = document.CreateAttribute("id");
                idAttr.Value = p.Text;

                var coordAttr = document.CreateAttribute("coordinates");
                coordAttr.Value = $"{p.Tag.Position.X}, {p.Tag.Position.Y}, {p.Tag.Position.Z}";

                var hAttr = document.CreateAttribute("heading");
                hAttr.Value = $"{p.Tag.Heading}";

                // Set attributes
                spawnPointNode.Attributes.Append(idAttr);
                spawnPointNode.Attributes.Append(coordAttr);
                spawnPointNode.Attributes.Append(hAttr);

                // Add spawn point
                spawnPointNode.IsEmpty = true;
                positionsNode.AppendChild(spawnPointNode);
            }

            return positionsNode;
        }

        /// <summary>
        /// Closes an element tag with short hand if there is no inner text
        /// </summary>
        /// <param name="node"></param>
        private void CloseElementTagShortIfEmpty(XmlElement node)
        {
            if (String.IsNullOrWhiteSpace(node.InnerText))
            {
                node.IsEmpty = true;
            }
        }

        /// <summary>
        /// Resets all check points just added by this position
        /// </summary>
        private void ResetCheckPoints()
        {
            // Delete all checkpoints
            foreach (int handle in CheckpointHandles.Values)
            {
                GameWorld.DeleteCheckpoint(handle);
            }

            // Clear checkpoint handles
            CheckpointHandles.Clear();

            // Clear location check point
            if (NewLocationCheckpointHandle != -123456789)
            {
                GameWorld.DeleteCheckpoint(NewLocationCheckpointHandle);
                NewLocationCheckpointHandle = -123456789;
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

                    // If keyboard is open, do not process controls!
                    if (IsKeyboardOpen) continue;

                    // Process menus
                    AllMenus.ProcessMenus();

                    // If menu is closed, Wait for key press, then open menu
                    if (!AllMenus.IsAnyMenuOpen() && Keyboard.IsKeyDownWithModifier(Settings.OpenMenuKey, Settings.OpenMenuModifierKey))
                    {
                        MainUIMenu.Visible = true;
                    }

                    // Enable/Disable buttons if not/on duty
                    if (MainUIMenu.Visible)
                    {
                        DispatchMenuButton.Enabled = Main.OnDuty;
                        PatrolSettingsMenuButton.Enabled = Main.OnDuty;
                        ModSettingsMenuButton.Enabled = Main.OnDuty;
                    }

                    // Disable patrol area selection if not highway patrol
                    if (DispatchUIMenu.Visible)
                    {
                        // Disable the Callout menu button if player is not on a callout
                        EndCallMenuButton.Enabled = Dispatch.PlayerActiveCall != null;
                        RequestCallMenuButton.Enabled = Dispatch.CanInvokeAnyCalloutForPlayer();
                    }
                }
            });
        }
    }
}
