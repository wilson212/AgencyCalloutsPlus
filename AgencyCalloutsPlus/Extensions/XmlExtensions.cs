using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AgencyCalloutsPlus.Extensions
{
    public static class XmlExtensions
    {
        // Get the node full path
        public static string GetFullPath(this XmlNode node)
        {
            string path = node.Name;
            XmlNode search = null;

            // Get up until ROOT
            while ((search = node.ParentNode).NodeType != XmlNodeType.Document)
            {
                path = search.Name + " > " + path; // Add to path
                node = search;
            }

            return path;
        }
    }
}
