using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Xml;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace AgencyDispatchFramework.NativeUI
{
    internal partial class PluginMenu
    {
        private UIMenuItem RoadShoulderCreateButton { get; set; }

        private UIMenuItem RoadShoulderLoadBlipsButton { get; set; }

        private UIMenuItem RoadShoulderClearBlipsButton { get; set; }

        private SpawnPoint RoadShoulderLocation { get; set; }

        private UIMenuNumericScrollerItem<int> RoadShoulderSpeedButton { get; set; }

        private UIMenuListItem RoadShoulderPostalButton { get; set; }

        private UIMenuListItem RoadShoulderZoneButton { get; set; }

        private UIMenuItem RoadShoulderStreetButton { get; set; }

        private UIMenuItem RoadShoulderHintButton { get; set; }

        private UIMenuItem RoadShoulderFlagsButton { get; set; }

        private Dictionary<RoadFlags, UIMenuCheckboxItem> RoadShouldFlagsItems { get; set; }

        private UIMenuItem RoadShoulderBeforeFlagsButton { get; set; }

        private UIMenuListItem RoadShoulderBeforeListButton { get; set; }

        private Dictionary<IntersectionFlags, UIMenuCheckboxItem> BeforeIntersectionItems { get; set; }

        private UIMenuItem RoadShoulderAfterFlagsButton { get; set; }

        private UIMenuListItem RoadShoulderAfterListButton { get; set; }

        private Dictionary<IntersectionFlags, UIMenuCheckboxItem> AfterIntersectionItems { get; set; }

        private UIMenuItem RoadShoulderSaveButton { get; set; }

        private UIMenuItem RoadShoulderSpawnPointsButton { get; set; }

        private Dictionary<RoadShoulderPosition, UIMenuItem<SpawnPoint>> RoadShoulderSpawnPointItems { get; set; }

        /// <summary>
        /// Builds the menu and its buttons
        /// </summary>
        private void BuildRoadShouldersMenu()
        {
            // Create road shoulder ui menu
            RoadShoulderUIMenu = new UIMenu(MENU_NAME, "~b~Road Shoulder Menu")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Create road shoulder ui menu
            AddRoadShoulderUIMenu = new UIMenu(MENU_NAME, "~b~Add Road Shoulder")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Road Shoulder flags selection menu
            RoadShoulderFlagsUIMenu = new UIMenu(MENU_NAME, "~b~Road Shoulder Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Road shoulder spawn points menu
            RoadShoulderSpawnPointsUIMenu = new UIMenu(MENU_NAME, "~b~Road Shoulder Spawn Points")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // *************************************************
            // RoadShoulder UI Menu
            // *************************************************

            // Setup buttons
            RoadShoulderCreateButton = new UIMenuItem("Add New Location", "Creates a new Road Shoulder location where you are currently");
            RoadShoulderLoadBlipsButton = new UIMenuItem("Load Checkpoints", "Loads checkpoints in the world as well as blips on the map to show all saved locations in this zone");
            RoadShoulderClearBlipsButton = new UIMenuItem("Clear Checkpoints", "Clears all checkpoints and blips loaded by the ~y~Load Checkpoints ~w~option");

            // Button Events
            RoadShoulderCreateButton.Activated += RoadShouldersCreateButton_Activated;
            RoadShoulderLoadBlipsButton.Activated += (s, e) => LoadZoneLocations("RoadShoulders", Color.Red);
            RoadShoulderClearBlipsButton.Activated += (s, e) => ClearZoneLocations();

            // Add buttons
            RoadShoulderUIMenu.AddItem(RoadShoulderCreateButton);
            RoadShoulderUIMenu.AddItem(RoadShoulderLoadBlipsButton);
            RoadShoulderUIMenu.AddItem(RoadShoulderClearBlipsButton);

            // Bind Buttons
            RoadShoulderUIMenu.BindMenuToItem(AddRoadShoulderUIMenu, RoadShoulderCreateButton);

            // *************************************************
            // Add RoadShoulder UI Menu
            // *************************************************

            // Setup Buttons
            RoadShoulderStreetButton = new UIMenuItem("Street Name", "");
            RoadShoulderHintButton = new UIMenuItem("Location Hint", "");
            RoadShoulderSpeedButton = new UIMenuNumericScrollerItem<int>("Speed Limit", "Sets the speed limit of this road", 10, 80, 5);
            RoadShoulderZoneButton = new UIMenuListItem("Zone", "Selects the jurisdictional zone");
            RoadShoulderPostalButton = new UIMenuListItem("Postal", "The current location's Postal Code.");
            RoadShoulderSpawnPointsButton = new UIMenuItem("Spawn Points", "Sets safe spawn points for ped groups");
            RoadShoulderFlagsButton = new UIMenuItem("Road Shoulder Flags", "Open the RoadShoulder flags menu.");
            RoadShoulderSaveButton = new UIMenuItem("Save", "Saves the current location data to the XML file");

            // Button events
            RoadShoulderStreetButton.Activated += DispayKeyboard_SetDescription;
            RoadShoulderHintButton.Activated += DispayKeyboard_SetDescription;
            RoadShoulderSaveButton.Activated += RoadShoulderSaveButton_Activated;

            // Add Buttons
            AddRoadShoulderUIMenu.AddItem(RoadShoulderStreetButton);
            AddRoadShoulderUIMenu.AddItem(RoadShoulderHintButton);
            AddRoadShoulderUIMenu.AddItem(RoadShoulderSpeedButton);
            AddRoadShoulderUIMenu.AddItem(RoadShoulderZoneButton);
            AddRoadShoulderUIMenu.AddItem(RoadShoulderPostalButton);
            AddRoadShoulderUIMenu.AddItem(RoadShoulderSpawnPointsButton);
            AddRoadShoulderUIMenu.AddItem(RoadShoulderFlagsButton);
            AddRoadShoulderUIMenu.AddItem(RoadShoulderSaveButton);

            // Bind buttons
            AddRoadShoulderUIMenu.BindMenuToItem(RoadShoulderFlagsUIMenu, RoadShoulderFlagsButton);
            AddRoadShoulderUIMenu.BindMenuToItem(RoadShoulderSpawnPointsUIMenu, RoadShoulderSpawnPointsButton);

            // *************************************************
            // Intersection Flags
            // *************************************************
            RoadShoulderBeforeFlagsUIMenu = new UIMenu(MENU_NAME, "~b~Before Intersection Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            RoadShoulderAfterFlagsUIMenu = new UIMenu(MENU_NAME, "~b~After Intersection Flags")
            {
                MouseControlsEnabled = false,
                AllowCameraMovement = true,
                WidthOffset = 12
            };

            // Create Buttons
            RoadShoulderBeforeListButton = new UIMenuListItem("Direction", "The direction of the ajoining road (only applies to ~y~Three Way intersections)");
            RoadShoulderAfterListButton = new UIMenuListItem("Direction", "The direction of the ajoining road (only applies to ~y~Three Way intersections)");

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

            // Add positions
            RoadShoulderSpawnPointItems = new Dictionary<RoadShoulderPosition, UIMenuItem<SpawnPoint>>();
            foreach (RoadShoulderPosition flag in Enum.GetValues(typeof(RoadShoulderPosition)))
            {
                var name = Enum.GetName(typeof(RoadShoulderPosition), flag);
                var item = new UIMenuItem<SpawnPoint>(name, "Activate to set position. ~y~Ensure there is proper space to spawn a group of Peds at this location");
                item.Activated += RoadShoulderSpawnPointButton_Activated;
                RoadShoulderSpawnPointItems.Add(flag, item);

                // Add button
                RoadShoulderSpawnPointsUIMenu.AddItem(item);
            }

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

            // Register for events
            AddRoadShoulderUIMenu.OnMenuChange += AddRoadShoulderUIMenu_OnMenuChange;
            RoadShoulderFlagsUIMenu.OnMenuChange += RoadShoulderFlagsUIMenu_OnMenuChange;
        }

        /// <summary>
        /// Method called when the "Create New Road Shoulder" button is clicked.
        /// Clears all prior data.
        /// </summary>
        private void RoadShouldersCreateButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            //
            // Reset everything!
            //
            if (NewLocationCheckpointHandle != -123456789)
            {
                GameWorld.DeleteCheckpoint(NewLocationCheckpointHandle);
                NewLocationCheckpointHandle = -123456789;
            }

            // Grab player location
            var pos = Rage.Game.LocalPlayer.Character.Position;
            var cpPos = new Vector3(pos.X, pos.Y, pos.Z - ZCorrection);
            var heading = Rage.Game.LocalPlayer.Character.Heading;
            RoadShoulderLocation = new SpawnPoint(cpPos, heading);

            // Reset road shoulder flags
            foreach (var cb in RoadShouldFlagsItems.Values)
            {
                cb.Checked = false;
            }

            // Reset spawn points
            foreach (UIMenuItem<SpawnPoint> item in RoadShoulderSpawnPointItems.Values)
            {
                item.Tag = null;
                item.RightBadge = UIMenuItem.BadgeStyle.None;
            }

            // Reset intersection flags
            foreach (var cb in BeforeIntersectionItems)
            {
                cb.Value.Checked = false;
                AfterIntersectionItems[cb.Key].Checked = false;
            }
            RoadShoulderBeforeListButton.Index = 0;
            RoadShoulderAfterListButton.Index = 0;

            // Create checkpoint at the player location
            NewLocationCheckpointHandle = GameWorld.CreateCheckpoint(cpPos, Color.Red);

            // Get current Postal
            RoadShoulderPostalButton.Collection.Clear();
            var postal = Postal.FromVector(pos);
            RoadShoulderPostalButton.Collection.Add(postal, postal.Code.ToString());

            // Add Zones
            RoadShoulderZoneButton.Collection.Clear();
            RoadShoulderZoneButton.Collection.Add(GameWorld.GetZoneNameAtLocation(pos));
            RoadShoulderZoneButton.Collection.Add("HIGHWAY");

            // Set street name default
            var streetName = GameWorld.GetStreetNameAtLocation(pos, out string crossingRoad);
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

            // Crossing road
            if (!String.IsNullOrEmpty(crossingRoad))
            {
                RoadShoulderHintButton.Description = $"near {crossingRoad}";
                RoadShoulderHintButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
            else
            {
                // Reset ticks
                RoadShoulderHintButton.RightBadge = UIMenuItem.BadgeStyle.None;
            }

            // Reset
            RoadShoulderFlagsButton.RightBadge = UIMenuItem.BadgeStyle.None;
            RoadShoulderSpawnPointsButton.RightBadge = UIMenuItem.BadgeStyle.None;

            // Enable button
            RoadShoulderSaveButton.Enabled = true;
        }

        /// <summary>
        /// Method called when a residece "Spawnpoint" button is clicked on the Road Shoulder Spawn Points UI menu
        /// </summary>
        private void RoadShoulderSpawnPointButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            int handle = 0;
            var pos = GamePed.Player.Position;
            var heading = GamePed.Player.Heading;
            var value = (RoadShoulderPosition)Enum.Parse(typeof(RoadShoulderPosition), selectedItem.Text);
            int index = (int)value;
            var menuItem = (UIMenuItem<SpawnPoint>)selectedItem;

            // Check, do we have a check point already for this position?
            if (SpawnPointHandles.ContainsKey(index))
            {
                handle = SpawnPointHandles[index];
                GameWorld.DeleteCheckpoint(handle);
            }

            // Create new checkpoint !!important, need to subtract 2 from the Z since checkpoints spawn at waist level
            var cpPos = new Vector3(pos.X, pos.Y, pos.Z - ZCorrection);
            handle = GameWorld.CreateCheckpoint(cpPos, Color.Yellow, radius: 5f);
            if (SpawnPointHandles.ContainsKey(index))
                SpawnPointHandles[index] = handle;
            else
                SpawnPointHandles.Add(index, handle);

            // Create spawn point
            menuItem.Tag = new SpawnPoint(cpPos, heading);
            menuItem.RightBadge = UIMenuItem.BadgeStyle.Tick;

            // Are we complete
            bool complete = true;
            foreach (UIMenuItem<SpawnPoint> item in RoadShoulderSpawnPointItems.Values)
            {
                if (item.Tag == null)
                {
                    complete = false;
                    break;
                }
            }

            // Signal to the player
            if (complete)
            {
                RoadShoulderSpawnPointsButton.RightBadge = UIMenuItem.BadgeStyle.Tick;
            }
        }

        /// <summary>
        /// Method called when the "Save" button is clicked on the Road Shoulder UI menu
        /// </summary>
        private void RoadShoulderSaveButton_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Disable button to prevent spam clicking!
            RoadShoulderSaveButton.Enabled = false;

            // Ensure everything is done
            var requiredItems = new[] { RoadShoulderStreetButton, RoadShoulderSpawnPointsButton, RoadShoulderFlagsButton };
            foreach (var item in requiredItems)
            {
                if (item.RightBadge != UIMenuItem.BadgeStyle.Tick)
                {
                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "Add Road Shoulder",
                        $"~o~Location does not have all required parameters set"
                    );
                    RoadShoulderSaveButton.Enabled = true;
                    return;
                }
            }

            // @todo Save file
            var pos = RoadShoulderLocation.Position;
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
                    "Add Road Shoulder",
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
                headingAttr.Value = $"{RoadShoulderLocation.Heading}";

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

                // Pretty print
                CloseElementTagShortIfEmpty(hintNode);
                CloseElementTagShortIfEmpty(beforeFlagsNode);
                CloseElementTagShortIfEmpty(afterFlagsNode);

                // Add
                flagsNode.AppendChild(roadFlagsNode);
                flagsNode.AppendChild(beforeFlagsNode);
                flagsNode.AppendChild(afterFlagsNode);
                locationNode.AppendChild(flagsNode);

                // Add spawn points
                var positionsNode = CreatePositionsNodeWithSpawnPoints(file.Document, RoadShoulderSpawnPointItems);
                locationNode.AppendChild(positionsNode);

                // Ensure path exists and add the new location node
                var rootNode = UpdateOrCreateXmlNode(file.Document, zoneName, "Locations", "RoadShoulders");
                rootNode.AppendChild(locationNode);

                // Save
                file.Document.Save(path);
            }

            // Display notification to the player
            Rage.Game.DisplayNotification(
                "3dtextures",
                "mpgroundlogo_cops",
                "Agency Dispatch Framework",
                "~b~Add Road Shoulder.",
                $"~g~Location saved Successfully. It will be available next time you load up the game!"
            );

            // Go back
            AddRoadShoulderUIMenu.GoBack();

            // Are we currently showing checkpoints and blips?
            if (ZoneCheckpoints.Count > 0)
            {
                ZoneCheckpoints.Add(GameWorld.CreateCheckpoint(pos, Color.Yellow, forceGround: true));
                ZoneBlips.Add(new Blip(pos) { Color = Color.Red });
            }
        } 
    }
}
