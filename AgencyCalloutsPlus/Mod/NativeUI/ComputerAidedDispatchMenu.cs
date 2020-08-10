using AgencyCalloutsPlus.API;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;
using RAGENativeUI.Elements;
using RAGENativeUI.PauseMenu;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AgencyCalloutsPlus.NativeUI
{
    public static class ComputerAidedDispatchMenu
    {
        private static TabView tabView;

        private static TabItemSimpleList simpleListTab;
        private static TabMissionSelectItem missionSelectTab;
        private static TabTextItem textTab;
        private static TabSubmenuItem submenuTab;

        private static Persona PlayerPersona { get; set; }

        public static void Initialize()
        {
            // Tell the game to call our method every tick
            Game.FrameRender += Process;

            // Grab player Persona
            PlayerPersona = Functions.GetPersonaForPed(Game.LocalPlayer.Character);
            
            // Setup the tab view
            tabView = new TabView("Computer Aided Dispatch System");
            tabView.Name = PlayerPersona.FullName;
            tabView.Money = Dispatch.PlayerAgency.FriendlyName;
            tabView.MoneySubtitle = "Status: " + Enum.GetName(typeof(OfficerStatus), Dispatch.GetPlayerStatus());

            tabView.AddTab(textTab = new TabTextItem("TabTextItem", "Text Tab Item", "I'm a text tab item"));
            textTab.Activated += TextTab_Activated;

            List<TabItem> items = new List<TabItem>(25);
            for (int i = 0; i < 25; i++)
            {
                TabTextItem tItem = new TabTextItem("Item #" + i, "Title #" + i, "Some random text for #" + i);

                tItem.Activated += SubMenuItem_Activated;
                items.Add(tItem);
            }
            tabView.AddTab(submenuTab = new TabSubmenuItem("A submenu", items));

            List<UIMenuItem> menuItems = new List<UIMenuItem>()
            {
                new UIMenuItem("First MenuItem!", ""),
                new UIMenuCheckboxItem("A Checkbox", true),
                new UIMenuListItem("List", new List<dynamic>()
                {
                    "something",
                    new Vector3(5, 0, 5),
                    10,
                    5,
                    false,
                }, 0),
            };
            TabInteractiveListItem interactiveListItem = new TabInteractiveListItem("An Interactive List", menuItems);
            tabView.AddTab(interactiveListItem);

            tabView.RefreshIndex();
        }

        public static void Process(object sender, GraphicsEventArgs e)
        {
            // Our menu on/off switch.
            if (Game.IsKeyDown(Keys.F9))
            {
                tabView.Visible = !tabView.Visible;
                Game.IsPaused = tabView.Visible;
            }

            // Update status
            var status = Dispatch.GetPlayerStatus();
            string statusName = Enum.GetName(typeof(OfficerStatus), status);
            tabView.MoneySubtitle = String.Concat("Status: 10-", (int)status, " (", statusName, ")");

            // Update tabview
            tabView.Update();
            tabView.DrawTextures(e.Graphics);
        }

        private static void SubMenuItem_Activated(object sender, EventArgs e)
        {
            Game.DisplaySubtitle("Activated Submenu Item #" + submenuTab.Index, 5000);
        }

        private static void TextTab_Activated(object sender, EventArgs e)
        {
            Game.DisplaySubtitle("I'm in the text tab", 5000);
        }

        private static void MissionSelectTab_OnItemSelect(MissionInformation selectedItem)
        {
            if (selectedItem.Name == "Mission One")
            {
                Game.DisplaySubtitle("~g~Mission One Activated", 5000);
            }
            else if (selectedItem.Name == "Mission Two")
            {
                Game.DisplaySubtitle("~b~Mission Two Activated", 5000);
            }
        }

        private static void SimpleListTab_Activated(object sender, EventArgs e)
        {
            Game.DisplaySubtitle("I'm in the simple list tab", 5000);
        }
    }
}
