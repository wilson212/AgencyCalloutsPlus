using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Dispatching.Assignments;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Xml;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using static Rage.Native.NativeFunction;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// Represents a basic <see cref="RAGENativeUI.MenuPool"/> for questioning peds during a callout
    /// </summary>
    internal class PluginMenu
    {
        private UIMenu MainUIMenu;
        private UIMenu DispatchUIMenu;
        private UIMenu PatrolUIMenu;
        private UIMenu LocationsUIMenu;

        private UIMenu RoadShoulderUIMenu;
        private UIMenu RoadShoulderFlagsUIMenu;

        private UIMenu RoadShoulderBeforeFlagsUIMenu;
        private UIMenu RoadShoulderAfterFlagsUIMenu;

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

        #endregion

        #region Road Shoulder Menu Buttons

        private UIMenuNumericScrollerItem<int> RoadShoulderSpeedButton { get; set; }

        private UIMenuListItem RoadShoulderPostalButton { get; set; }

        private UIMenuListItem RoadShoulderZoneButton { get; set; }

        private UIMenuItem RoadShoulderStreetButton { get; set; }

        private UIMenuItem RoadShoulderHintButton { get; set; }

        private UIMenuItem RoadShoulderFlagsButton { get; set; }

        private Dictionary<RoadFlags, UIMenuCheckboxItem> RoadShouldFlagsItems { get; set; }

        private UIMenuItem RoadShoulderBeforeFlagsButton { get; set; }

        private RelativeDirection BeforeRelativeDirection { get; set; }

        private UIMenuListItem RoadShoulderBeforeListButton { get; set; }

        private Dictionary<IntersectionFlags, UIMenuCheckboxItem> BeforeIntersectionItems { get; set; }

        private UIMenuItem RoadShoulderAfterFlagsButton { get; set; }

        private RelativeDirection AfterRelativeDirection { get; set; }

        private UIMenuListItem RoadShoulderAfterListButton { get; set; }

        private Dictionary<IntersectionFlags, UIMenuCheckboxItem> AfterIntersectionItems { get; set; }

        private UIMenuItem RoadShoulderSaveButton { get; set; }

        #endregion

        internal bool IsKeyboardOpen { get; set; }

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
            LocationsMenuButton = new UIMenuItem("Location Editor", "Opens the location editor menu");
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

            // Create Patrol Menu
            BuildRoadShouldersMenu();

            // Create Patrol Menu
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
                RoadShoulderAfterFlagsUIMenu
            };

            // Refresh indexes
            AllMenus.RefreshIndex();
            MainUIMenu.OnMenuChange += MainUIMenu_OnMenuChange;
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

        private void BuildRoadShouldersMenu()
        {
            // Create road shoulder ui menu
            RoadShoulderUIMenu = new UIMenu("ADF", "~b~Road Shoulder Creator")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Flags selection menu
            RoadShoulderFlagsUIMenu = new UIMenu("ADF", "~b~Road Shoulder Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Setup Buttons
            RoadShoulderStreetButton = new UIMenuItem("Street Name", "");
            RoadShoulderHintButton = new UIMenuItem("Location Hint", "");
            RoadShoulderSpeedButton = new UIMenuNumericScrollerItem<int>("Speed Limit", "Sets the speed limit of this road", 10, 80, 5);
            RoadShoulderZoneButton = new UIMenuListItem("Zone", "Selects the jurisdictional zone");
            RoadShoulderPostalButton = new UIMenuListItem("Postal", "The current location's Postal Code.");
            RoadShoulderFlagsButton = new UIMenuItem("Road Shoulder Flags", "Open the RoadShoulder flags menu.");
            RoadShoulderSaveButton = new UIMenuItem("Save", "Saves the current location data to the XML file");

            // Button events
            RoadShoulderStreetButton.Activated += DispayKeyboard_SetDescription;
            RoadShoulderHintButton.Activated += DispayKeyboard_SetDescription;
            RoadShoulderSaveButton.Activated += RoadShoulderSaveButton_Activated;

            // Add Buttons
            RoadShoulderUIMenu.AddItem(RoadShoulderStreetButton);
            RoadShoulderUIMenu.AddItem(RoadShoulderHintButton);
            RoadShoulderUIMenu.AddItem(RoadShoulderSpeedButton);
            RoadShoulderUIMenu.AddItem(RoadShoulderZoneButton);
            RoadShoulderUIMenu.AddItem(RoadShoulderPostalButton);
            RoadShoulderUIMenu.AddItem(RoadShoulderFlagsButton);
            RoadShoulderUIMenu.AddItem(RoadShoulderSaveButton);

            // Bind buttons
            RoadShoulderUIMenu.BindMenuToItem(RoadShoulderFlagsUIMenu, RoadShoulderFlagsButton);

            // *************************************************
            // Intersection Flags
            // *************************************************
            RoadShoulderBeforeFlagsUIMenu = new UIMenu("ADF", "~b~Before Intersection Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            RoadShoulderAfterFlagsUIMenu = new UIMenu("ADF", "~b~After Intersection Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Create Buttons
            RoadShoulderBeforeListButton = new UIMenuListItem("Direction", "The direction of the intersection (only applies to T intersections)");
            RoadShoulderAfterListButton = new UIMenuListItem("Direction", "The direction of the intersection (only applies to T intersections)");

            // Add directions
            foreach (RelativeDirection direction in Enum.GetValues(typeof(RelativeDirection)))
            {
                var name = Enum.GetName(typeof(RelativeDirection), direction);
                RoadShoulderBeforeListButton.Collection.Add(direction, name);
                RoadShoulderAfterListButton.Collection.Add(direction, name);
            }

            // Add buttons to the menu
            RoadShoulderBeforeFlagsUIMenu.AddItem(RoadShoulderBeforeListButton);
            RoadShoulderAfterFlagsUIMenu.AddItem(RoadShoulderAfterListButton);

            // Add road shoulder intersection flags
            BeforeIntersectionItems = new Dictionary<IntersectionFlags, UIMenuCheckboxItem>();
            AfterIntersectionItems = new Dictionary<IntersectionFlags, UIMenuCheckboxItem>();
            foreach (IntersectionFlags flag in Enum.GetValues(typeof(IntersectionFlags)))
            {
                var name = Enum.GetName(typeof(IntersectionFlags), flag);
                var cb = new UIMenuCheckboxItem(name, false);
                BeforeIntersectionItems.Add(flag, cb);
                RoadShoulderBeforeFlagsUIMenu.AddItem(cb);

                cb = new UIMenuCheckboxItem(name, false);
                AfterIntersectionItems.Add(flag, cb);
                RoadShoulderAfterFlagsUIMenu.AddItem(cb);
            }

            // Bind buttons
            RoadShoulderBeforeFlagsButton = new UIMenuItem("Before Intersection Flags");
            RoadShoulderBeforeFlagsButton.LeftBadge = UIMenuItem.BadgeStyle.Car;
            RoadShoulderFlagsUIMenu.AddItem(RoadShoulderBeforeFlagsButton);
            RoadShoulderFlagsUIMenu.BindMenuToItem(RoadShoulderBeforeFlagsUIMenu, RoadShoulderBeforeFlagsButton);
            RoadShoulderAfterFlagsButton = new UIMenuItem("After Intersection Flags");
            RoadShoulderAfterFlagsButton.LeftBadge = UIMenuItem.BadgeStyle.Car;
            RoadShoulderFlagsUIMenu.AddItem(RoadShoulderAfterFlagsButton);
            RoadShoulderFlagsUIMenu.BindMenuToItem(RoadShoulderAfterFlagsUIMenu, RoadShoulderAfterFlagsButton);

            // Finally, add road
            // Add road shoulder flags list
            RoadShouldFlagsItems = new Dictionary<RoadFlags, UIMenuCheckboxItem>();
            foreach (RoadFlags flag in Enum.GetValues(typeof(RoadFlags)))
            {
                var name = Enum.GetName(typeof(RoadFlags), flag);
                var cb = new UIMenuCheckboxItem(name, false);
                RoadShouldFlagsItems.Add(flag, cb);

                // Add button
                RoadShoulderFlagsUIMenu.AddItem(cb);
            }
        }

        private void BuildLocationsMenu()
        {
            // Create patrol menu
            LocationsUIMenu = new UIMenu("ADF", "~b~Location Editor Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Setup Buttons
            RoadShouldersButton = new UIMenuItem("New Road Shoulder", "Open the RoadShoulder location menu.");
            RoadShouldersButton.Activated += RoadShouldersButton_Activated;

            // Add buttons
            LocationsUIMenu.AddItem(RoadShouldersButton);

            // Bind buttons
            LocationsUIMenu.BindMenuToItem(RoadShoulderUIMenu, RoadShouldersButton);
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
        /// Method called when the "Create New Road Shoulder" button is clicked.
        /// Clears all prior data.
        /// </summary>
        private void RoadShouldersButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            //
            // Reset everything!
            //

            // Reset road shoulder flags
            foreach (var cb in RoadShouldFlagsItems.Values)
            {
                cb.Checked = false;
            }

            // Reset intersection flags
            foreach (var cb in BeforeIntersectionItems)
            {
                cb.Value.Checked = false;
                AfterIntersectionItems[cb.Key].Checked = false;
            }
            RoadShoulderBeforeListButton.Index = 0;
            RoadShoulderAfterListButton.Index = 0;

            // Grab player location
            var pos = Rage.Game.LocalPlayer.Character.Position;

            // Get current Postal
            RoadShoulderPostalButton.Collection.Clear();
            var postal = Postal.FromVector(pos);
            RoadShoulderPostalButton.Collection.Add(postal, postal.Code.ToString());

            // Add Zones
            RoadShoulderZoneButton.Collection.Clear();
            RoadShoulderZoneButton.Collection.Add(GameWorld.GetZoneNameAtLocation(pos));
            RoadShoulderZoneButton.Collection.Add("HIGHWAY");

            // Set street name default
            var streetName = GameWorld.GetStreetNameAtLocation(pos);
            if (!String.IsNullOrEmpty(streetName))
            {
                RoadShoulderStreetButton.Description = streetName;
                RoadShoulderStreetButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
            else
            {
                RoadShoulderStreetButton.Description = "";
                RoadShoulderStreetButton.RightBadge = UIMenuItem.BadgeStyle.None;
            }

            // Reset ticks
            RoadShoulderHintButton.RightBadge = UIMenuItem.BadgeStyle.None;

            // Enable button
            RoadShoulderSaveButton.Enabled = true;
        }

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

        /// <summary>
        /// Method called when the "Save" button is clicked on the Road Shoulder UI menu
        /// </summary>
        private void RoadShoulderSaveButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Disable button to prevent spam clicking!
            RoadShoulderSaveButton.Enabled = false;

            // @todo Save file
            var pos = GamePed.Player.Position;
            string zoneName = RoadShoulderZoneButton.SelectedValue.ToString();
            string path = Path.Combine(Main.FrameworkFolderPath, "Locations", $"{zoneName}.xml");

            // Make sure the file exists!
            if (!File.Exists(path))
            {
                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "Add Road Shoulder.",
                    $"~o~Location file {zoneName}.xml does not exist!"
                );
                return;
            }

            // Open the file, and add the location
            using (var file = new WorldZoneFile(path))
            {
                // Create attributes
                var vectorAttr = file.Document.CreateAttribute("coordinates");
                vectorAttr.Value = $"{pos.X}, {pos.Y}, {pos.Z}";

                var headingAttr = file.Document.CreateAttribute("heading");
                headingAttr.Value = $"{GamePed.Player.Heading}";

                // Create location node, and add attributes
                var locationNode = file.Document.CreateElement("Location");
                locationNode.Attributes.Append(vectorAttr);
                locationNode.Attributes.Append(headingAttr);

                // Create street node
                var streetNode = file.Document.CreateElement("Street");
                streetNode.InnerText = RoadShoulderStreetButton.Description;
                locationNode.AppendChild(streetNode);

                // Create street node
                var hintNode = file.Document.CreateElement("Hint");
                hintNode.InnerText = RoadShoulderHintButton.Description;
                locationNode.AppendChild(hintNode);

                // Create speed node
                var speedNode = file.Document.CreateElement("Speed");
                speedNode.InnerText = RoadShoulderSpeedButton.Value.ToString();
                locationNode.AppendChild(speedNode);

                // Flags
                var flagsNode = file.Document.CreateElement("Flags");

                // Road flags
                var roadFlagsNode = file.Document.CreateElement("Road");
                var items = RoadShouldFlagsItems.Values.Where(x => x.Checked).Select(x => x.Text).ToArray();
                roadFlagsNode.InnerText = String.Join(", ", items);

                // Before intersection flags
                var beforeFlagsNode = file.Document.CreateElement("BeforeIntersection");
                if (RoadShoulderBeforeListButton.Index != 0)
                {
                    // Create attribute
                    var beforeAttr = file.Document.CreateAttribute("direction");
                    beforeAttr.Value = RoadShoulderBeforeListButton.SelectedValue.ToString();
                    beforeFlagsNode.Attributes.Append(beforeAttr);
                }
                items = BeforeIntersectionItems.Values.Where(x => x.Checked).Select(x => x.Text).ToArray();
                beforeFlagsNode.InnerText = String.Join(", ", items);

                // After intersection flags
                var afterFlagsNode = file.Document.CreateElement("AfterIntersection");
                if (RoadShoulderAfterListButton.Index != 0)
                {
                    // Create attribute
                    var afterAttr = file.Document.CreateAttribute("direction");
                    afterAttr.Value = RoadShoulderAfterListButton.SelectedValue.ToString();
                    afterFlagsNode.Attributes.Append(afterAttr);
                }
                items = AfterIntersectionItems.Values.Where(x => x.Checked).Select(x => x.Text).ToArray();
                afterFlagsNode.InnerText = String.Join(", ", items);

                // Add
                flagsNode.AppendChild(roadFlagsNode);
                flagsNode.AppendChild(beforeFlagsNode);
                flagsNode.AppendChild(afterFlagsNode);
                locationNode.AppendChild(flagsNode);

                // Add the new location node
                var node = file.Document.SelectSingleNode($"{zoneName}/Locations/RoadShoulders");
                node.AppendChild(locationNode);

                // Save
                file.Document.Save(path);
            }

            // Display notification to the player
            Rage.Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "Agency Dispatch Framework",
                "Add Road Shoulder.",
                $"~g~Location saved Successfully"
            );

            // Go back
            RoadShoulderUIMenu.GoBack();
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
