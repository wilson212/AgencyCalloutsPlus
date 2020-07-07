using System;
using System.Collections.Generic;
using System.Xml;

namespace AgencyCalloutsPlus.Mod.Conversation
{
    internal class ResponseSet
    {
        public string Id { get; set; }

        protected Dictionary<string, Response> Responses { get; set; }

        public ResponseSet(string id, XmlNode node, ExpressionParser parser = null)
        {
            this.Id = id;
            Responses = new Dictionary<string, Response>();
            LoadResponsesFromXml(node, parser);
        }

        public Response GetResponseTo(string input)
        {
            if (!Responses.TryGetValue(input, out Response response))
            {
                return null;
            }

            return response;
        }

        private void LoadResponsesFromXml(XmlNode node, ExpressionParser parser = null)
        {
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
                var response = new Response(n.Attributes["to"].Value, n.Attributes["returnMenu"].Value);

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
                            string id = response.FromInputId; 
                            Log.Warning($"ResponseSet.LoadResponsesFromXml(): LineSet from Response TO '{id}' has 'if' statement but no ExpressionParser is defined... Skipping");
                            continue;
                        }

                        // parse expression
                        if (!parser.Evaluate(lsNode.Attributes["if"].Value))
                        {
                            // If we failed to evaluate to true, skip
                            continue;
                        }
                    }

                    // Create LineSet
                    var lineSet = new LineSet(prob);
                    List<LineItem> lines = new List<LineItem>(lsNode.ChildNodes.Count);

                    // Each Line
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
                Responses.Add(response.FromInputId, response);
            }
        }
    }
}
