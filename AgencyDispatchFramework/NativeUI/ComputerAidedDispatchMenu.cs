using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using RAGENativeUI.PauseMenu;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// This class handles the AgencyDispatchFramework Computer Aided Dispatch UI menu
    /// </summary>
    public static class ComputerAidedDispatchMenu
    {
        /// <summary>
        /// Indicates whether this CAD has been initialized this session
        /// </summary>
        private static bool Initialized { get; set; }

        /// <summary>
        /// Indicates whether this CAD is currently running
        /// </summary>
        private static bool IsRunning { get; set; }

        /// <summary>
        /// Contains the main <see cref="TabView"/>
        /// </summary>
        private static TabView DispatchWindow;

        /// <summary>
        /// Contains the current call tab
        /// </summary>
        private static AssignmentTabPage CurrentCallTab { get; set; }

        /// <summary>
        /// Contains the call list tab
        /// </summary>
        private static CallListTabPage CallListTab { get; set; }

        /// <summary>
        /// Contains the scenario list tab
        /// </summary>
        private static TabSubmenuItem ScenarioListTab { get; set; }

        /// <summary>
        /// Contains the officer status tab
        /// </summary>
        private static TabSubmenuItem DepartmentListTab { get; set; }

        /// <summary>
        /// Contains the officer status tabs
        /// </summary>
        private static List<TabInteractiveListItem> OfficersList { get; set; }

        /// <summary>
        /// Containts the players <see cref="Persona"/>
        /// </summary>
        private static Persona PlayerPersona { get; set; }

        /// <summary>
        /// Initializes the CAD for this mod
        /// </summary>
        internal static void Initialize()
        {
            // Exit if already running
            if (IsRunning) return;
            IsRunning = true;

            // Is this the first time this session the player has gone on duty?
            if (!Initialized)
            {
                // Grab player Persona
                PlayerPersona = Functions.GetPersonaForPed(Rage.Game.LocalPlayer.Character);

                // Setup the tab view
                DispatchWindow = new TabView("~y~eForce ~w~Computer Aided Dispatch System");
                DispatchWindow.Name = PlayerPersona.FullName;
                DispatchWindow.Money = Dispatch.PlayerAgency.FullName;
                DispatchWindow.OnMenuClose += (s, e) => Rage.Game.IsPaused = false;

                // BOLO'S ?

                // Add active calls list
                DispatchWindow.AddTab(CallListTab = new CallListTabPage("Open Calls"));

                // Add Current Call tab
                DispatchWindow.AddTab(CurrentCallTab = new AssignmentTabPage("Assigned Call"));

                // Officer Status (per department)
                OfficersList = new List<TabInteractiveListItem>();
                DispatchWindow.AddTab(DepartmentListTab = new TabSubmenuItem("Unit Status", OfficersList));

                // Build initial scnario page
                BuildScenarioPage();

                // Register for events to add scenarios
                ScenarioPool.OnCalloutPackLoaded += (path, assembly, items) => BuildScenarioPage();

                // Flag
                Initialized = true;
            }
            else
            {
                // Grab player Persona
                PlayerPersona = Functions.GetPersonaForPed(Rage.Game.LocalPlayer.Character);

                // Setup the tab view
                DispatchWindow.Name = PlayerPersona.FullName;
                DispatchWindow.Money = Dispatch.PlayerAgency.FullName;

                // Build initial scnario page
                BuildScenarioPage();
            }

            // Tell the game to process this menu every game tick
            Rage.Game.FrameRender += Process;
        }

        internal static void Dispose()
        {
            // Remove the process this menu every game tick
            Rage.Game.FrameRender -= Process;

            // Indicate we are not running
            IsRunning = false;
        }

        /// <summary>
        /// Method called everytime a callout pack is added to the <see cref="ScenarioPool"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assembly"></param>
        /// <param name="items"></param>
        private static void BuildScenarioPage() // (string path, Assembly assembly, int items)
        {
            // If a page exists already, remove it!
            if (ScenarioListTab != null)
            {
                DispatchWindow.Tabs.Remove(ScenarioListTab);
            }

            // Add each registered callout scenario to the list
            var iList = new List<TabInteractiveListItem>();
            foreach (var scenes in ScenarioPool.ScenariosByAssembly)
            {
                var menuItems = new List<UIMenuItem<CalloutScenarioInfo>>(scenes.Value.Count);
                foreach (var scenario in scenes.Value)
                {
                    var item = new UIMenuItem<CalloutScenarioInfo>(scenario.Name)
                    {
                        Tag = scenario
                    };
                    item.Activated += ScenarioItem_Activated;
                    menuItems.Add(item);
                }

                iList.Add(new TabInteractiveListItem(scenes.Key, menuItems));
            }

            // Add scenario list tab page
            DispatchWindow.AddTab(ScenarioListTab = new TabSubmenuItem("Scenario List", iList));

            // Always refresh index after adding or removing items
            DispatchWindow.RefreshIndex();
        }

        /// <summary>
        /// Method called when a scenario is activated from the scenario list. 
        /// Creates a new <see cref="PriorityCall"/> using the activated <see cref="CalloutScenario"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedItem"></param>
        private static void ScenarioItem_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Get our scenario info
            var item = selectedItem as UIMenuItem<CalloutScenarioInfo>;
            var call = Dispatch.CrimeGenerator.CreateCallFromScenario(item.Tag);

            // Add call to dispatch
            Dispatch.AddIncomingCall(call);

            // Try and invoke callout for player
            if (Dispatch.InvokeCallForPlayer(call))
            {
                Rage.Game.DisplaySubtitle("~b~Call created! You will be dispatched to this call once you exit the menu", 5000);
            }
            else
            {
                Rage.Game.DisplaySubtitle("~o~You are already on an active callout", 5000);
            }
        }

        /// <summary>
        /// Method to be called on every <see cref="Rage.Game.FrameRender"/> event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal static void Process(object sender, GraphicsEventArgs e)
        {
            // Ignore if we are not on duty
            if (!IsRunning) return;
            var justOpened = false;

            // Our menu toggle key switch.
            if (Keyboard.IsKeyDownWithModifier(Settings.OpenCADMenuKey, Settings.OpenCADMenuModifierKey))
            {
                // Toggle window state
                DispatchWindow.Visible = !DispatchWindow.Visible;

                // Always refresh index after opening the menu again
                if (DispatchWindow.Visible)
                {
                    // Refresh index
                    DispatchWindow.RefreshIndex();
                    justOpened = true;
                }
            }
            
            // Update
            if (DispatchWindow.Visible)
            {
                // Ensure game is paused
                Rage.Game.IsPaused = true;

                // Update player status text
                var status = Dispatch.GetPlayerStatus();
                string statusName = Enum.GetName(typeof(OfficerStatus), status);
                DispatchWindow.MoneySubtitle = String.Concat("Status: 10-", (int)status, " (", statusName, ")");

                // Update tabview
                DispatchWindow.Update();
                DispatchWindow.DrawTextures(e.Graphics);

                // if we just opened, update the list
                if (justOpened)
                {
                    // Build new list
                    DepartmentListTab.Items.Clear();
                    foreach (var agency in Dispatch.GetEnabledAgencies())
                    {
                        var items = new List<UIMenuItem>();
                        var period = GameWorld.CurrentTimePeriod;

                        // Add each unit
                        foreach (var unit in agency.OfficersByShift[period])
                        {
                            items.Add(new UIMenuListScrollerItem<string>(unit.CallSign, "", new[] { unit.Status.ToString() }) { ScrollingEnabled = false });
                        }

                        DepartmentListTab.Items.Add(new TabInteractiveListItem(agency.FullName, items));
                    }
                }
            }
        }
    }
}
