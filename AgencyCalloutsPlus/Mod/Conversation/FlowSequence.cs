using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.Mod.NativeUI;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace AgencyCalloutsPlus.Mod.Conversation
{
    /// <summary>
    /// Represents a conversation flow for a <see cref="Rage.Ped"/> based on menu input by the player
    /// </summary>
    public class FlowSequence
    {
        /// <summary>
        /// Contains the regular expression to replace variables within Ped responses
        /// </summary>
        private static Regex VariableExpression = new Regex(@"\$(\w+)\$", RegexOptions.Compiled);

        /// <summary>
        /// Gets the sequence ID for this conversation event
        /// </summary>
        public string SequenceId { get; protected set; }

        /// <summary>
        /// Gets the <see cref="Rage.Ped"/> this <see cref="FlowSequence"/> is attacthed to
        /// </summary>
        public PedWrapper SubjectPed { get; private set; }

        /// <summary>
        /// Contains the main menu string name
        /// </summary>
        protected string InitialMenuName { get; set; }

        /// <summary>
        /// A <see cref="MenuPool"/> that contains all of this <see cref="FlowSequence"/> <see cref="UIMenu"/>s
        /// </summary>
        internal MenuPool AllMenus { get; set; }

        /// <summary>
        /// Contains a list of flow return menu's by name
        /// </summary>
        protected Dictionary<string, UIMenu> FlowReturnMenus { get; set; }

        /// <summary>
        /// Contains a list of <see cref="UIMenuItem"/>s by id
        /// that it belongs to
        /// </summary>
        protected Dictionary<string, UIMenuItem> MenuButtonsById { get; set; }

        /// <summary>
        /// Contains a list of flow return menu items that are initially no visible
        /// </summary>
        protected Dictionary<string, UIMenuItem> HiddenMenuItems { get; set; }

        /// <summary>
        /// Contains our <see cref="ResponseSet"/> for this <see cref="FlowSequence"/>
        /// </summary>
        internal ResponseSet PedResponses { get; set; }

        /// <summary>
        /// Contains officer dialogs attached to each menu option
        /// </summary>
        protected Dictionary<string, LineItem[]> OfficerDialogs { get; set; }

        /// <summary>
        /// Contains string replacements in the Output strings
        /// </summary>
        protected Dictionary<string, object> Variables { get; set; }

        /// <summary>
        /// Contains the expression parser used on the "if" attribuutes
        /// </summary>
        protected ExpressionParser Parser { get; set; }

        /// <summary>
        /// Gets the FlowOutcome name selected for this conversation event
        /// </summary>
        public string FlowOutcomeId { get; protected set; }

        /// <summary>
        /// Delegate to handle a <see cref="FlowSequenceEvent"/>
        /// </summary>
        /// <param name="sender">The <see cref="FlowSequence"/> that triggered the <see cref="PedResponse"/></param>
        /// <param name="e">The <see cref="PedResponse"/> instance</param>
        /// <param name="l">The displayed <see cref="LineSet"/> to the player in game</param>
        public delegate void PedResponseEventHandler(FlowSequence sender, PedResponse e, LineSet l);

        /// <summary>
        /// Event fired whenever a <see cref="PedResponse"/> is displayed
        /// </summary>
        public event PedResponseEventHandler OnPedResponse;

        /// <summary>
        /// Creates a new <see cref="FlowSequence"/> instance
        /// </summary>
        /// <param name="ped">The conversation subject ped</param>
        /// <param name="outcomeId">The selected outcome <see cref="ResponseSet"/></param>
        /// <param name="document">The XML document containing the <see cref="FlowSequence"/> XML data</param>
        /// <param name="parser">An <see cref="ExpressionParser" /> containing parameters to parse "if" statements in the XML</param>
        public FlowSequence(string sequenceId, Ped ped, FlowOutcome outcome, XmlDocument document, ExpressionParser parser)
        {
            // Set internal vars
            SequenceId = sequenceId;
            FlowOutcomeId = outcome.Id;
            SubjectPed = new PedWrapper(ped);
            Parser = parser;
            Variables = new Dictionary<string, object>();
            HiddenMenuItems = new Dictionary<string, UIMenuItem>();
            MenuButtonsById = new Dictionary<string, UIMenuItem>();

            // Parse XML document
            LoadXml(document, outcome.Id);
        }

        /// <summary>
        /// Method to be called within the <see cref="LSPD_First_Response.Mod.Callouts.Callout.Process()"/> method
        /// each tick to process the internal menus
        /// </summary>
        /// <param name="player"></param>
        public void Process(Ped player)
        {
            try
            {
                // Process all menu's
                AllMenus.ProcessMenus();

                // Ensure we are still talking to the Ped and in distance
                if (!InTalkingDistance(player))
                {
                    AllMenus.CloseAllMenus();
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        /// <summary>
        /// Indicates whether the <see cref="Player"/> is within talking distance of this <see cref="SubjectPed"/>
        /// </summary>
        /// <param name="player">The <see cref="Player"/> instance</param>
        /// <returns></returns>
        private bool InTalkingDistance(Ped player)
        {
            // Is player within 3m of the ped?
            if (player.Position.DistanceTo(SubjectPed.Ped.Position) > 3f)
            {
                // too far away
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the <see cref="Ped"/> response to the question
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public PedResponse GetResponseToQuestionId(string text)
        {
            //var inputId = TranslateQuestion(text);
            return PedResponses.GetResponseTo(text);
        }

        /// <summary>
        /// Sets a variable to be parsed on each conversation line
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetVariable<T>(string name, T value)
        {
            if (Variables.ContainsKey(name))
            {
                Variables[name] = value;
            }
            else
            {
                Variables.Add(name, value);
            }
        }

        /// <summary>
        /// Sets a variable dictionary with string replacements to be parsed on each conversation line
        /// </summary>
        public void SetVariableDictionary(Dictionary<string, object> variables)
        {
            Variables = variables;
        }

        /// <summary>
        /// Dispose method to clear memory
        /// </summary>
        public void Dispose()
        {
            if (AllMenus != null)
            {
                AllMenus.CloseAllMenus();
                AllMenus.Clear();
                AllMenus = null;
            }

            foreach (var menu in FlowReturnMenus)
            {
                menu.Value.Clear();
            }

            FlowReturnMenus.Clear();
        }

        /// <summary>
        /// Sets the visibility of the <see cref="FlowSequence"/> <see cref="UIMenu"/>
        /// </summary>
        /// <param name="visible">if true, the menu is shown, otherwise menu is closed</param>
        public void SetMenuVisible(bool visible)
        {
            bool isAnyOpen = AllMenus.IsAnyMenuOpen();
            if (!visible && isAnyOpen)
            {
                AllMenus.CloseAllMenus();
            }
            else if (visible && !isAnyOpen)
            {
                // Open default menu
                FlowReturnMenus[InitialMenuName].Visible = true;
            }
        }

        /// <summary>
        /// Hides a questioning menu item by id
        /// </summary>
        /// <param name="buttonId">the button id to hide</param>
        public bool HideMenuItem(string buttonId)
        {
            // Already hidden?
            if (HiddenMenuItems.ContainsKey(buttonId) || !MenuButtonsById.ContainsKey(buttonId))
                return false;

            // Grab menu ID for this item ID
            var menuItem = MenuButtonsById[buttonId];

            // Add to hidden items
            HiddenMenuItems.Add(buttonId, menuItem);

            // Remove Item
            int index = menuItem.Parent.MenuItems.IndexOf(menuItem);
            menuItem.Parent.RemoveItemAt(index);
            menuItem.Parent.RefreshIndex();
            return true;
        }

        /// <summary>
        /// Displays a hidden questioning menu item by id
        /// </summary>
        /// <param name="buttonId">the button id to show</param>
        public bool ShowMenuItem(string buttonId)
        {
            // Already being displayed?
            if (!HiddenMenuItems.ContainsKey(buttonId))
                return false;

            // Grab menu ID for this item ID
            var menuItem = HiddenMenuItems[buttonId];

            // Add Item
            menuItem.Parent.AddItem(menuItem);
            menuItem.Parent.RefreshIndex();

            // Remove from hidden items
            HiddenMenuItems.Remove(buttonId);
            return true;
        }

        /// <summary>
        /// Method called on event <see cref="UIMenuItem.Activated"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedItem"></param>
        private void On_ItemActivated(UIMenu sender, UIMenuItem selectedMenuItem)
        {
            // Convert to my menu item
            var selectedItem = selectedMenuItem as MyUIMenuItem<string>;
            if (selectedMenuItem == null)
            {
                return;
            }

            // Disable button from spam clicks
            selectedItem.Enabled = false;

            // Dont do anything right now!
            var response = GetResponseToQuestionId(selectedItem.Tag);
            if (response == null)
            {
                Game.DisplayNotification($"~r~No XML Response[@to='~b~{selectedItem.Tag}~r~'] node for this Ped.");
                return;
            }

            // Grab dialog
            var lineSet = response.GetResponseLineSet();
            if (lineSet == null || lineSet.Lines.Length == 0)
            {
                Log.Error($"FlowSequence.Item_Activated: Response to '{selectedItem.Tag}' has no lines");
                return;
            }

            // Clear any queued messages
            SubtitleQueue.Clear();

            // Officer Dialogs first
            foreach (LineItem line in OfficerDialogs[selectedItem.Tag])
            {
                var text = VariableExpression.ReplaceTokens(line.Text, Variables);
                SubtitleQueue.Add($"~y~You~w~: {text}<br /><br />", line.Time);
            }

            // Add Ped response
            foreach (LineItem line in lineSet.Lines)
            {
                var text = VariableExpression.ReplaceTokens(line.Text, Variables);
                SubtitleQueue.Add($"~y~{SubjectPed.Persona.Forename}~w~: {text}<br /><br />", line.Time);
            }

            // Enable button and change font color to show its been clicked before
            selectedItem.Enabled = true;
            selectedItem.ForeColor = Color.Gray;

            // Fire off event
            OnPedResponse(this, response, lineSet);
        }

        #region XML loading methods

        private void LoadXml(XmlDocument document, string outcomeId)
        {
            var documentRoot = document.SelectSingleNode("FlowSequence");
            AllMenus = new MenuPool();
            FlowReturnMenus = new Dictionary<string, UIMenu>();
            OfficerDialogs = new Dictionary<string, LineItem[]>();

            // ====================================================
            // Load input flow return menus
            // ====================================================
            XmlNode catagoryNode = documentRoot.SelectSingleNode("Input");
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                throw new ArgumentNullException("Input");
            }

            // Load FlowReturn items
            int i = 0;
            foreach (XmlNode n in catagoryNode.SelectNodes("Menu"))
            {
                // Validate and extract attributes
                if (n.Attributes == null || n.Attributes["id"]?.Value == null)
                {
                    Log.Warning($"FlowSequence.LoadXml(): Menu node has no 'id' attribute");
                    continue;
                }

                // Create
                string menuId = n.Attributes["id"].Value;
                var menu = new UIMenu("Ped Interaction", $"~y~{SubjectPed.Persona.FullName}");

                // Extract menu items
                foreach (XmlNode menuItemNode in n.SelectNodes("MenuItem"))
                {
                    // Extract attributes
                    if (menuItemNode.Attributes == null)
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no attributes");
                        continue;
                    }

                    // Extract id attriibute
                    string buttonId = menuItemNode.Attributes["id"]?.Value;
                    if (String.IsNullOrWhiteSpace(buttonId))
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no or empty 'id' attribute");
                        continue;
                    }

                    // Ensure ID is unique
                    if (OfficerDialogs.ContainsKey(buttonId))
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem 'id' attribute is not unique");
                        continue;
                    }

                    // Extract button text attriibute
                    string text = menuItemNode.Attributes["buttonText"]?.Value;
                    if (String.IsNullOrWhiteSpace(text))
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no or empty 'buttonText' attribute");
                        continue;
                    }

                    // Extract visible attriibute
                    bool isVisible = true;
                    string val = menuItemNode.Attributes["visible"]?.Value;
                    if (!String.IsNullOrWhiteSpace(text))
                    {
                        if (!bool.TryParse(val, out isVisible))
                        {
                            isVisible = true;
                            Log.Warning($"FlowSequence.LoadXml(): Menu -> MenuItem attribute 'visible' attribute is not correctly formatted");
                        }
                    }

                    // Ensure we have lines to read
                    if (!menuItemNode.HasChildNodes)
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no Line child nodes");
                        continue;
                    }

                    // Load dialog for officer
                    var subNodes = menuItemNode.SelectNodes("Line");
                    if (subNodes != null && subNodes.Count > 0)
                    {
                        List<LineItem> lines = new List<LineItem>(subNodes.Count);
                        foreach (XmlNode lNode in subNodes)
                        {
                            // Validate and extract attributes
                            if (lNode.Attributes == null || !Int32.TryParse(lNode.Attributes["time"]?.Value, out int time))
                            {
                                time = 3000;
                            }

                            lines.Add(new LineItem() { Text = lNode.InnerText, Time = time });
                        }

                        // Save lines
                        OfficerDialogs.Add(buttonId, lines.ToArray());
                    }
                    else
                    {
                        // Log
                        Log.Warning("FlowSequence.LoadXml(): Menu -> MenuItem has no Line nodes");

                        // Save default line
                        OfficerDialogs.Add(buttonId, new LineItem[] { new LineItem() { Text = text, Time = 3000 } });
                    }

                    // Create button
                    var item = new MyUIMenuItem<string>(text);
                    item.Tag = buttonId;
                    item.Parent = menu; // !! Important !!
                    item.Activated += On_ItemActivated;

                    // Add named item to our hash table
                    if (!MenuButtonsById.ContainsKey(buttonId))
                    {
                        MenuButtonsById.Add(buttonId, item);
                    }

                    // Add item to menu
                    //menu.AddItem(item);

                    // Handle item visibility
                    if (!isVisible)
                    {
                        // Remove item
                        //menu.MenuItems.Remove(item);

                        // Store for later
                        HiddenMenuItems.Add(buttonId, item);
                    }
                    else
                    {
                        menu.AddItem(item);
                    }
                }

                // Add menu
                AllMenus.Add(menu);
                FlowReturnMenus.Add(menuId, menu);

                if (i == 0)
                {
                    InitialMenuName = menuId;
                }

                i++;
            }

            // Call menu refresh
            AllMenus.RefreshIndex();

            // ====================================================
            // Load response sequences
            // ====================================================
            catagoryNode = documentRoot.SelectSingleNode($"Output/FlowOutcome[@id = '{outcomeId}']");
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                throw new ArgumentNullException("Output", $"Scenario FlowOutcome does not exist '{outcomeId}'");
            }

            // Validate and extract attributes
            if (!String.IsNullOrWhiteSpace(catagoryNode.Attributes["initialMenu"]?.Value))
            {
                // Ensure menu exists
                var menuName = catagoryNode.Attributes["initialMenu"].Value;
                if (FlowReturnMenus.ContainsKey(menuName))
                {
                    InitialMenuName = menuName;
                }
                else
                {
                    // Log as warning and continue
                    Log.Warning($"FlowSequence.LoadXml(): Specified initial menu for FlowOutcome '{FlowOutcomeId}' does not exist!");
                }
            }
            else
            {
                // Log as warning and continue
                Log.Warning($"FlowSequence.LoadXml(): FlowOutcome '{FlowOutcomeId}' does not have an 'initialMenu' attribute!");
            }

            // Load flow outcome
            PedResponses = new ResponseSet(outcomeId, catagoryNode, Parser);
        }

        #endregion XML loading methods
    }
}
