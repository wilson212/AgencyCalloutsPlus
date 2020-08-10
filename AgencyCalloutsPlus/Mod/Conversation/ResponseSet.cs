using System;
using System.Collections.Generic;
using System.Xml;

namespace AgencyCalloutsPlus.Mod.Conversation
{
    /// <summary>
    /// Represents a collection of <see cref="PedResponse" />s for a <see cref="Rage.Ped"/>
    /// </summary>
    internal class ResponseSet
    {
        /// <summary>
        /// Gets the flow outcome Id of this response set
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// A collection of responses using question id as the key
        /// </summary>
        protected Dictionary<string, PedResponse> Responses { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ResponseSet"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="node"></param>
        /// <param name="parser"></param>
        public ResponseSet(string id, XmlNode node, ExpressionParser parser = null)
        {
            this.Id = id;
            Responses = new Dictionary<string, PedResponse>();

            // Load responses from the XML node within the Flow Sequence XML document
            LoadResponsesFromXml(node, parser);
        }

        /// <summary>
        /// Gets a <see cref="PedResponse"/> by the specified question ID
        /// </summary>
        /// <param name="questionId"></param>
        /// <returns><see cref="PedResponse"/> on success, or null</returns>
        public PedResponse GetResponseTo(string questionId)
        {
            if (!Responses.TryGetValue(questionId, out PedResponse response))
            {
                return null;
            }

            return response;
        }

        /// <summary>
        /// Loads the response data from the flow sequence document
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parser"></param>
        private void LoadResponsesFromXml(XmlNode node, ExpressionParser parser)
        {
            // Ensure our node is not null
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            XmlNodeList responses = node.SelectNodes("Response");
            if (responses == null || responses.Count == 0)
            {
                throw new ArgumentNullException("Response", "FlowOutcome has no Response nodes.");
            }

            // Each Response Node
            foreach (XmlNode n in responses)
            {
                // Validate and extract attributes
                if (n.Attributes == null || n.Attributes["to"]?.Value == null)
                {
                    Log.Error($"ResponseSet.LoadResponsesFromXml(): Response has no 'to' attribute");
                    continue;
                }

                // Validate and extract attributes
                if (n.Attributes["returnMenu"]?.Value == null)
                {
                    Log.Error($"ResponseSet.LoadResponsesFromXml(): Response has no 'returnMenu' attribute");
                    continue;
                }

                // Check for line sets
                var childNodes = n.SelectNodes("LineSet");
                if (childNodes == null || childNodes.Count == 0)
                {
                    Log.Error($"ResponseSet.LoadResponsesFromXml(): Response has no LineSet child nodes");
                    continue;
                }

                // Create response object
                var response = new PedResponse(n.Attributes["to"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), n.Attributes["returnMenu"].Value);

                // See if we have an hide statement to hide menuitems
                if (n.Attributes["hide"]?.Value != null)
                {
                    var items = n.Attributes["hide"].Value.Split(',', ' ');
                    response.HidesMenuItems = items;
                }
                else
                {
                    response.HidesMenuItems = new string[0];
                }

                // See if we have an unhide statement to hide menuitems
                if (n.Attributes["show"]?.Value != null)
                {
                    var items = n.Attributes["show"].Value.Split(',', ' ');
                    response.ShowMenuItems = items;
                }
                else
                {
                    response.ShowMenuItems = new string[0];
                }

                // Each LineSet
                foreach (XmlNode lsNode in childNodes)
                {
                    // Ensure we have lines to read
                    if (!lsNode.HasChildNodes)
                    {
                        Log.Error($"ResponseSet.LoadResponsesFromXml(): LineSet has no Line child nodes");
                        continue;
                    }

                    // Validate and extract attributes
                    if (lsNode.Attributes == null || !Int32.TryParse(lsNode.Attributes["probability"]?.Value, out int prob))
                    {
                        Log.Error($"ResponseSet.LoadResponsesFromXml(): LineSet has no attributes or cannot parse 'id' attribute");
                        continue;
                    }

                    // See if we have an IF statement
                    if (lsNode.Attributes["if"]?.Value != null)
                    {
                        // Do we have a parser?
                        if (parser == null)
                        {
                            string id = response.FromInputIds[0]; 
                            Log.Warning($"ResponseSet.LoadResponsesFromXml(): LineSet from Response TO '{id}' has 'if' statement but no ExpressionParser is defined... Skipping");
                            continue;
                        }

                        // parse expression
                        if (!parser.Evaluate<bool>(lsNode.Attributes["if"].Value))
                        {
                            // If we failed to evaluate to true, skip
                            continue;
                        }
                    }

                    // Create LineSet
                    var lineSet = new LineSet(prob);

                    // See if we have an hide statement to hide menuitems
                    if (lsNode.Attributes["hide"]?.Value != null)
                    {
                        var items = lsNode.Attributes["hide"].Value.Split(',');
                        lineSet.HidesMenuItems = items;
                    }
                    else
                    {
                        lineSet.HidesMenuItems = new string[0];
                    }

                    // See if we have an unhide statement to hide menuitems
                    if (lsNode.Attributes["show"]?.Value != null)
                    {
                        var items = lsNode.Attributes["show"].Value.Split(',');
                        lineSet.ShowMenuItems = items;
                    }
                    else
                    {
                        lineSet.ShowMenuItems = new string[0];
                    }

                    // Fetch response lines
                    List<LineItem> lines = new List<LineItem>(lsNode.ChildNodes.Count);
                    foreach (XmlNode lNode in lsNode)
                    {
                        // Validate and extract attributes
                        if (lNode.Attributes == null || !Int32.TryParse(lNode.Attributes["time"]?.Value, out int time))
                        {
                            time = 3000;
                        }

                        lines.Add(new LineItem() { Text = lNode.InnerText, Time = time });
                    }

                    // Save lines
                    lineSet.Lines = lines.ToArray();
                    response.AddLineSet(lineSet);
                }

                // Add final response
                foreach (string eyeD in response.FromInputIds)
                {
                    if (!Responses.ContainsKey(eyeD))
                        Responses.Add(eyeD, response);
                }
            }
        }
    }
}
