/*
 * PUMA - PeterU Metadata API

Copyright (c) 2018 LSPDFR-PeterU, 
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of the copyright holder nor the names of its
  contributors may be used to endorse or promote products derived from
  this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Xml;

namespace AgencyCalloutsPlus.Mod
{
    /// <summary>
    /// Holds information about metadata properties of a given <see cref="Rage.Ped"/> model. Not intended to be used directly: 
    /// the actual descriptions that apply to a given Ped instance may depend based on the components, drawables
    /// and textures of that instance.
    /// </summary>
    public class PedModelMeta
    {

        /// <summary>
        /// The Ped model to which this entry pertains.
        /// </summary>
        public string Model { get; protected set; }

        /// <summary>
        /// All possible Properties that may potentially describe a <see cref="Rage.Ped"/> model. Not intended for direct use.
        /// Note that the actual descriptors
        /// that will be applicable to a given Ped instance will depend upon the components, drawables and textures of that instance.
        /// </summary>
        public List<PedDescriptionProperty> Properties { get; protected set; }

        /// <summary>
        /// Given an individual &lt;Ped&gt; XML node from a PedModelMeta XML file, build a PedModelMeta object and load its Properties.
        /// </summary>
        /// <param name="metaNode"></param>
        public PedModelMeta(XmlNode metaNode)
        {
            try
            {
                XmlElement element = (XmlElement)metaNode;

                if (!element.HasAttribute("Model"))
                {
                    throw new ArgumentException("The passed XML node does not have a Model attribute.");
                }

                Model = element.GetAttribute("Model").ToUpperInvariant();

                Properties = new List<PedDescriptionProperty>();

                // get direct Property child nodes
                XmlNodeList propertyChildren = metaNode.SelectNodes("./Property");

                foreach (XmlNode propertyChild in propertyChildren)
                {
                    try
                    {
                        PedDescriptionProperty newProperty = new PedDescriptionProperty(propertyChild);
                        Properties.Add(newProperty);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Skipping setting up a Property for {Model} as an exception occurred.");
                        Log.Exception(e);
                    }
                }

                // get overrides and add them to our Properties list
                XmlNodeList overrideChildren = metaNode.SelectNodes("./Override");

                foreach (XmlNode overrideChild in overrideChildren)
                {
                    // for each override, get the component, drawable and texture, and then get child properties to add to those
                    try
                    {
                        XmlElement overrideElement = (XmlElement)overrideChild;
                        string componentString = overrideElement.GetAttribute("Component");
                        string drawableString = overrideElement.GetAttribute("Drawable");
                        string textureString = overrideElement.GetAttribute("Texture");

                        int component, drawable, texture = -1;

                        if (!Int32.TryParse(componentString, out component))
                        {
                            Log.Error($"Unable to add an Override set of Properties for {Model} -- could not parse Component as an int");
                        }

                        if (!Int32.TryParse(drawableString, out drawable))
                        {
                            Log.Error($"Unable to add an Override set of Properties for {Model} -- could not parse Drawable as an int");
                        }

                        if (!Int32.TryParse(textureString, out texture))
                        {
                            Log.Error($"Unable to add an Override set of Properties for {Model} -- could not parse Texture as an int");
                        }

                        if (component < 0 || drawable < 0 || texture < 0)
                        {
                            Log.Error($"Unable to add an Override set of Properties for {Model} -- component, drawable, texture are less than 0");
                            continue;
                        }

                        XmlNodeList overrideProperties = overrideChild.SelectNodes("./Property");
                        foreach (XmlNode propertyChild in overrideProperties)
                        {
                            try
                            {
                                PedDescriptionProperty newProperty = new PedDescriptionProperty(propertyChild, component, drawable, texture);
                                Properties.Add(newProperty);
                            }
                            catch (Exception e)
                            {
                                Log.Error($"Skipping setting up a Property for {Model} as an exception occurred.");
                                Log.Exception(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("Unable to add an Override set of Properties");
                        Log.Exception(e);
                    }
                }

            }
            catch (Exception e)
            {
                Log.Error("Exception when trying to set up PedModelMeta.");
                Log.Exception(e);
            }
        }
    }
}
