using AgencyCalloutsPlus.Extensions;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        /// Gets the <see cref="Rage.Ped"/> this <see cref="FlowSequence"/> is attacthed to
        /// </summary>
        public PedWrapper SubjectPed { get; private set; }

        /// <summary>
        /// Contains the main menu string name
        /// </summary>
        protected string MainMenu { get; set; }

        /// <summary>
        /// A <see cref="MenuPool"/> that contains all of this <see cref="FlowSequence"/> <see cref="UIMenu"/>s
        /// </summary>
        internal MenuPool AllMenus { get; set; }

        /// <summary>
        /// Contains a list of flow return menu's by name
        /// </summary>
        protected Dictionary<string, UIMenu> FlowReturnMenus { get; set; }

        /// <summary>
        /// Contains our <see cref="ResponseSet"/> for this <see cref="FlowSequence"/>
        /// </summary>
        internal ResponseSet FlowOutcome { get; set; }

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
        /// Creates a new <see cref="FlowSequence"/> instance
        /// </summary>
        /// <param name="ped">The conversation subject ped</param>
        /// <param name="outcomeId">The selected outcome <see cref="ResponseSet"/></param>
        /// <param name="document">The XML document containing the <see cref="FlowSequence"/> XML data</param>
        /// <param name="parser">An <see cref="ExpressionParser" /> containing parameters to parse "if" statements in the XML</param>
        public FlowSequence(Ped ped, string outcomeId, XmlDocument document, ExpressionParser parser)
        {
            FlowOutcomeId = outcomeId;
            SubjectPed = new PedWrapper(ped);
            Parser = parser;
            Variables = new Dictionary<string, object>();
            LoadXml(document, outcomeId);
        }

        /// <summary>
        /// Method to be called within the <see cref="LSPD_First_Response.Mod.Callouts.Callout.Process()"/> method
        /// each tick to process the internal menus
        /// </summary>
        /// <param name="player"></param>
        public void Process(Ped player)
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

        /// <summary>
        /// Indicates whether the <see cref="Player"/> is within talking distance of this <see cref="SubjectPed"/>
        /// </summary>
        /// <param name="player">The <see cref="Player"/> instance</param>
        /// <returns></returns>
        private bool InTalkingDistance(Ped player)
        {
            // Is player within 3m of the ped?
            if (player.Position.DistanceTo(SubjectPed.Ped.Position) > 5f)
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
        public LineSet GetResponseTo(string text)
        {
            //var inputId = TranslateQuestion(text);
            var response = FlowOutcome.GetResponseTo(text);
            if (response == null) return null;

            return response.GetResponseLineSet();
        }

        /// <summary>
        /// Sets a variabe to be parsed on each conversation line
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

        public void Dispose()
        {
            AllMenus.CloseAllMenus();
            AllMenus.Clear();
            AllMenus = null;

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
                FlowReturnMenus[MainMenu].Visible = true;
            }
        }

        #region XML loading methods

        private void LoadXml(XmlDocument document, string outcomeId)
        {
            AllMenus = new MenuPool();
            FlowReturnMenus = new Dictionary<string, UIMenu>();
            OfficerDialogs = new Dictionary<string, LineItem[]>();

            // ====================================================
            // Load input flow return menus
            // ====================================================
            XmlNode catagoryNode = document.DocumentElement.SelectSingleNode("Input");
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
                string name = n.Attributes["id"].Value;
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
                    string id = menuItemNode.Attributes["id"]?.Value;
                    if (String.IsNullOrWhiteSpace(id))
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no or empty 'id' attribute");
                        continue;
                    }

                    // Ensure ID is unique
                    if (OfficerDialogs.ContainsKey(id))
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem 'id' attribute is not unique");
                        continue;
                    }

                    // Extract text attriibute
                    string text = menuItemNode.Attributes["text"]?.Value;
                    if (String.IsNullOrWhiteSpace(text))
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no or empty 'text' attribute");
                        continue;
                    }

                    // Ensure we have lines to read
                    if (!menuItemNode.HasChildNodes)
                    {
                        Log.Error($"FlowSequence.LoadXml(): Menu -> MenuItem has no Line child nodes");
                        continue;
                    }

                    // Load dialog for officer
                    List<LineItem> lines = new List<LineItem>(menuItemNode.ChildNodes.Count);

                    // Each Line
                    foreach (XmlNode lNode in menuItemNode)
                    {
                        // Validate and extract attributes
                        if (lNode.Attributes == null || !Int32.TryParse(lNode.Attributes["time"]?.Value, out int time))
                        {
                            time = 3000;
                        }

                        lines.Add(new LineItem() { Text = lNode.InnerText, Time = time });
                    }

                    // Save lines
                    OfficerDialogs.Add(id, lines.ToArray());

                    // Create button
                    UIMenuItem item = new UIMenuItem(text);
                    item.Description = id;
                    item.Activated += Item_Activated;

                    // Add item to menu
                    menu.AddItem(item);
                }

                // Add menu
                AllMenus.Add(menu);
                FlowReturnMenus.Add(name, menu);

                if (i == 0)
                {
                    MainMenu = name;
                }

                i++;
            }

            // ====================================================
            // Load response sequences
            // ====================================================
            catagoryNode = document.DocumentElement.SelectSingleNode($"Output/FlowOutcome[@id = '{outcomeId}']");
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                throw new ArgumentNullException("Output", $"Scenario FlowOutcome does not exist '{outcomeId}'");
            }

            FlowOutcome = new ResponseSet(outcomeId, catagoryNode, Parser);
        }

        private void Item_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            // Disable button from spam clicks
            selectedItem.Enabled = false;

            // Dont do anything right now!
            var response = GetResponseTo(selectedItem.Description);
            if (response == null || response.Lines.Length == 0)
            {
                Game.DisplayNotification($"~r~No XML Response[@to='~b~{selectedItem.Description}~r~'] node for this Ped.");
                return;
            }

            // Clear any queued messages
            SubtitleQueue.Clear();

            // Officer Dialogs first
            foreach (LineItem line in OfficerDialogs[selectedItem.Description])
            {
                SubtitleQueue.Add($"~y~You~w~: {selectedItem.Text}<br /><br />", line.Time);
            }

            // Add Ped response
            foreach (LineItem line in response.Lines)
            {
                var text = VariableExpression.ReplaceTokens(line.Text, Variables);
                SubtitleQueue.Add($"~y~{SubjectPed.Persona.Forename}~w~: {text}<br /><br />", line.Time);
            }

            // Enable button and change font color to show its been clicked before
            selectedItem.Enabled = true;
            selectedItem.ForeColor = Color.Gray;
        }

        #endregion XML loading methods
    }
}
