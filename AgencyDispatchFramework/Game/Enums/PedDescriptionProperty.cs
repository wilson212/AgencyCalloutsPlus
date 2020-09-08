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
using System.Xml;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// The type of an individual Ped descriptor. 
    /// </summary>
    [Flags]
    public enum PedDescriptionPropertyType
    {
        /// <summary>
        /// Combination of the race and gender of a Ped model. For example, "white female". These are combined due to the stock audio
        /// files which have a single audio file for both race and sex.
        /// </summary>
        RaceSex = 1,

        /// <summary>
        /// A build type of the Ped model. For example, "thin", "athletic", or "muscular".
        /// </summary>
        Build = 2,

        /// <summary>
        /// Clothing.
        /// </summary>
        Clothing = 4,

        /// <summary>
        /// Hair color, or "no hair". Also includes head accessories like hats.
        /// </summary>
        Hair = 128,

        /// <summary>
        /// Any extra components not otherwise specified.
        /// </summary>
        Extras = 4096
    }

    /// <summary>
    /// Holds an individual Ped description property, such as "light sneakers". Allows getting audio and text descriptions of that property.
    /// </summary>
    public class PedDescriptionProperty
    {
        /// <summary>
        /// The type of this Ped description property.
        /// </summary>
        public PedDescriptionPropertyType Type { get; protected set; }
        
        /// <summary>
        /// The component index to which this Property pertains, or -1 to apply to all possible variations of this Ped Model.
        /// </summary>
        public int Component { get; protected set; }

        /// <summary>
        /// The drawable index to which this Property pertains, or -1 to apply to all possible variations of this Ped Model (**not **just
        /// all drawables in this component).
        /// </summary>
        public int Drawable { get; protected set; }

        /// <summary>
        /// The texture variation to which this Property pertains, or -1 to apply to all possible variations of this Ped Model  (**not **just
        /// all textures in this drawable).
        /// </summary>
        public int Texture { get; protected set; }

        /// <summary>
        /// Textual description of this property, e.g. "light sneakers"
        /// </summary>
        public string Text { get; protected set; }

        /// <summary>
        /// Audio description of this property as an LSDPDR Police Scanner audio file basename (e.g. "CLOTHING_LIGHT_SNEAKERS")
        /// </summary>
        public string Audio { get; protected set; }

        /// <summary>
        /// Construct a new Property from the specified arguments. Intended to be used as an override where no XML node exists.
        /// </summary>
        /// <param name="type">The type of this Ped description property.</param>
        /// <param name="text">Textual description of this property, e.g. "light sneakers"</param>
        /// <param name="audio">Audio description of this property as an LSDPDR Police Scanner audio file basename (e.g. "CLOTHING_LIGHT_SNEAKERS")</param>
        /// <param name="component">The component index to which this Property pertains, or -1 to apply to all possible variations of this Ped Model.</param>
        /// <param name="drawable">The drawable index to which this Property pertains, or -1 to apply to all possible variations of this Ped Model (**not **just all drawables in this component).</param>
        /// <param name="texture">The texture variation to which this Property pertains, or -1 to apply to all possible variations of this Ped Model  (**not **just all textures in this drawable).</param>
        public PedDescriptionProperty(PedDescriptionPropertyType type, string text, string audio, int component = -1, int drawable = -1, int texture = -1)
        {
            Type = type;

            Text = text;
            Audio = audio;

            Component = component;
            Drawable = drawable;
            Texture = texture;
        }

        /// <summary>
        /// Construct a new Property from the specifed XML node, which should be a &lt;Property&gt; tag.
        /// </summary>
        /// <param name="inputNode"></param>
        /// <param name="component"></param>
        /// <param name="drawable"></param>
        /// <param name="texture"></param>
        public PedDescriptionProperty(XmlNode inputNode, int component = -1, int drawable = -1, int texture = -1)
        {
            XmlElement propertyElement = (XmlElement)inputNode;

            if (!propertyElement.HasAttribute("Type") || !propertyElement.HasAttribute("Text") || !propertyElement.HasAttribute("Audio"))
            {
                throw new ArgumentException($"The passed XML node is malformed. It must have at least the 'Type', 'Text' and 'Audio' attributes.");
            }

            string typeString = propertyElement.GetAttribute("Type");

            switch (typeString)
            {
                case "RaceSex":
                    Type = PedDescriptionPropertyType.RaceSex;
                    break;

                case "Build":
                    Type = PedDescriptionPropertyType.Build;
                    break;

                case "Hair":
                    Type = PedDescriptionPropertyType.Hair;
                    break;

                case "Clothing":
                    Type = PedDescriptionPropertyType.Clothing;
                    break;

                default:
                    throw new ArgumentException($"The property Type {typeString} is not valid. Cannot initialize the PedDescriptionProperty.");
            }

            Text = propertyElement.GetAttribute("Text");
            Audio = propertyElement.GetAttribute("Audio");

            Component = component;
            Drawable = drawable;
            Texture = texture;
        }

        /// <summary>
        /// Return a string representing the object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Type}: \"{Text}\" (cmp{Component}, drw{Drawable}, tex{Texture}";
        }

    }
}
