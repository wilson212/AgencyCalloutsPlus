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
    /// This class handles the Agency Dispatch and Callouts+ Computer Aided Dispatch UI menu
    /// </summary>
    public static class ComputerAidedDispatchMenu
    {
        /// <summary>
        /// Contains the main <see cref="TabView"/>
        /// </summary>
        private static TabView tabView;

        /// <summary>
        /// Contains the current call tab
        /// </summary>
        private static CurrentCallTabPage CurrentCallTab { get; set; }

        /// <summary>
        /// Contains the call list tab
        /// </summary>
        private static OpenCallListTabPage CallListTab { get; set; }


        /// <summary>
        /// Containts the players <see cref="Persona"/>
        /// </summary>
        private static Persona PlayerPersona { get; set; }

        /// <summary>
        /// Initializes the CAD for this mod
        /// </summary>
        public static void Initialize()
        {
            // Tell the game to process this menu every game tick
            Rage.Game.FrameRender += Process;

            // Grab player Persona
            PlayerPersona = Functions.GetPersonaForPed(Rage.Game.LocalPlayer.Character);
                     
            // Setup the tab view
            tabView = new TabView("~y~eForce ~w~Computer Aided Dispatch System");
            tabView.Name = PlayerPersona.FullName;
            tabView.Money = Dispatch.PlayerAgency.FriendlyName;
            tabView.OnMenuClose += (s, e) => Rage.Game.IsPaused = false;

            // Add active calls list
            tabView.AddTab(CallListTab = new OpenCallListTabPage("Open Calls"));

            // Add Current Call tab
            tabView.AddTab(CurrentCallTab = new CurrentCallTabPage("Assigned Call"));

            // Add each registered callout scenario to the list
            var menuItems = new List<MyUIMenuItem<CalloutScenarioInfo>>();
            foreach (var scenes in Dispatch.ScenariosByName)
            {
                var item = new MyUIMenuItem<CalloutScenarioInfo>(scenes.Key)
                {
                    Tag = scenes.Value
                };
                item.Activated += ScenarioItem_Activated;
                menuItems.Add(item);
            }

            // Add scenario list tab page
            TabInteractiveListItem interactiveListItem = new TabInteractiveListItem("Scenario List", menuItems);
            tabView.AddTab(interactiveListItem);

            // Always refresh index after adding or removing items
            tabView.RefreshIndex();
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
            // Our menu toggle key switch.
            if (Keyboard.IsKeyDownWithModifier(Settings.OpenCADMenuKey, Settings.OpenCADMenuModifierKey))
            {
                tabView.Visible = !tabView.Visible;

                // Always refresh index after opening the menu again
                if (tabView.Visible)
                {
                    tabView.RefreshIndex();
                }
            }
            
            // Update
            if (tabView.Visible)
            {
                // Ensure game is paused
                Rage.Game.IsPaused = true;

                // Update player status text
                var status = Dispatch.GetPlayerStatus();
                string statusName = Enum.GetName(typeof(OfficerStatus), status);
                tabView.MoneySubtitle = String.Concat("Status: 10-", (int)status, " (", statusName, ")");

                // Update tabview
                tabView.Update();
                tabView.DrawTextures(e.Graphics);
            }
        }
    }
}
