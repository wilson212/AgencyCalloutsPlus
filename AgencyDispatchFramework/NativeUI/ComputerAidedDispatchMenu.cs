using AgencyDispatchFramework.Dispatching;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;
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
        private static CurrentCallTabPage CurrentCallTab { get; set; }

        /// <summary>
        /// Contains the call list tab
        /// </summary>
        private static OpenCallListTabPage CallListTab { get; set; }

        /// <summary>
        /// Contains the scenario list tab
        /// </summary>
        private static TabInteractiveListItem ScenarioListTab { get; set; }

        /// <summary>
        /// Containts the players <see cref="Persona"/>
        /// </summary>
        private static Persona PlayerPersona { get; set; }

        /// <summary>
        /// Initializes the CAD for this mod
        /// </summary>
        public static void Initialize()
        {
            // Exit if already running
            if (IsRunning) return;
            IsRunning = true;

            // Tell the game to process this menu every game tick
            Rage.Game.FrameRender += Process;

            // Is this the first time this session the player has gone on duty?
            if (!Initialized)
            {
                // Grab player Persona
                PlayerPersona = Functions.GetPersonaForPed(Rage.Game.LocalPlayer.Character);

                // Setup the tab view
                DispatchWindow = new TabView("~y~eForce ~w~Computer Aided Dispatch System");
                DispatchWindow.Name = PlayerPersona.FullName;
                DispatchWindow.Money = Dispatch.PlayerAgency.FriendlyName;
                DispatchWindow.OnMenuClose += (s, e) => Rage.Game.IsPaused = false;

                // Add active calls list
                DispatchWindow.AddTab(CallListTab = new OpenCallListTabPage("Open Calls"));

                // Add Current Call tab
                DispatchWindow.AddTab(CurrentCallTab = new CurrentCallTabPage("Assigned Call"));

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
                DispatchWindow.Money = Dispatch.PlayerAgency.FriendlyName;

                // Build initial scnario page
                BuildScenarioPage();
            }
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
            var menuItems = new List<MyUIMenuItem<CalloutScenarioInfo>>();
            foreach (var scenes in ScenarioPool.ScenariosByName)
            {
                var item = new MyUIMenuItem<CalloutScenarioInfo>(scenes.Key)
                {
                    Tag = scenes.Value
                };
                item.Activated += ScenarioItem_Activated;
                menuItems.Add(item);
            }

            // Add scenario list tab page
            ScenarioListTab = new TabInteractiveListItem("Scenario List", menuItems);
            DispatchWindow.AddTab(ScenarioListTab);

            // Always refresh index after adding or removing items
            DispatchWindow.RefreshIndex();
        }

        /// <summary>
        /// Method called when a scenario is activated from the scenario list. 
        /// Creates a new <see cref="PriorityCall"/> using the activated <see cref="CalloutScenario"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedItem"></param>
        private static void ScenarioItem_Activated(RAGENativeUI.UIMenu sender, UIMenuItem selectedItem)
        {
            // Get our scenario info
            var item = selectedItem as MyUIMenuItem<CalloutScenarioInfo>;
            var call = Dispatch.CrimeGenerator.CreateCallFromScenario(item.Tag);

            // Add call to dispatch
            Dispatch.AddIncomingCall(call);

            // Try and invoke callout for player
            if (Dispatch.InvokeCalloutForPlayer(call))
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
        public static void Process(object sender, GraphicsEventArgs e)
        {
            // Ignore if we are not on duty
            if (!IsRunning) return;

            // Our menu toggle key switch.
            if (Keyboard.IsKeyDownWithModifier(Settings.OpenCADMenuKey, Settings.OpenCADMenuModifierKey))
            {
                DispatchWindow.Visible = !DispatchWindow.Visible;

                // Always refresh index after opening the menu again
                if (DispatchWindow.Visible)
                {
                    DispatchWindow.RefreshIndex();
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
            }
        }
    }
}
