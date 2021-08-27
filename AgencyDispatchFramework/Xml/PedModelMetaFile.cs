using AgencyDispatchFramework.Game;
using System;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    internal class PedModelMetaFile : XmlFileBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filePath"></param>
        public PedModelMetaFile(string filePath) : base(filePath)
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        public int Parse()
        {
            int metasLoaded = 0;

            // Load the ped model meta nodes
            foreach (XmlNode node in Document.SelectNodes("/PedModelMeta//Ped"))
            {
                PedModelMeta newMeta = null;
                try
                {
                    newMeta = new PedModelMeta(node);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                if (newMeta == null)
                {
                    continue;
                }

                // get the new meta model name and use that as the key in the Dictionary
                string newKey = newMeta.Model.ToUpperInvariant();
                if (GamePed.PedModelMetaLookup.ContainsKey(newKey))
                {
                    //Configuration.Log($"Lookup dict already contains a key for {newKey}");
                    continue;
                }

                try
                {
                    GamePed.PedModelMetaLookup.Add(newKey, newMeta);
                    metasLoaded++;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to add created PedModelMeta for {newKey} to the lookup dict. Skipping this one.");
                    Log.Exception(e);
                }
            }

            return metasLoaded;
        }
    }
}
