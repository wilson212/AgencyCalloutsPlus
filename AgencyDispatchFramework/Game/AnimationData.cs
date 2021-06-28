using Rage;
using System;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// A container that contains both an animation name and dictionary
    /// </summary>
    public class AnimationData
    {
        /// <summary>
        /// Gets or sets the animation dictionary name
        /// </summary>
        public AnimationDictionary Dictionary { get; set; }

        /// <summary>
        /// Gets or sets an animation to play while this <see cref="Subtitle"/> is displayed
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether this instance appears to be a valid animation
        /// </summary>
        public bool AppearsValid { get => !String.IsNullOrEmpty(Name) && !String.IsNullOrEmpty(Dictionary.Name); }

        /// <summary>
        /// Creates a new instance of <see cref="AnimationData"/>
        /// </summary>
        public AnimationData()
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="AnimationData"/> using the
        /// specified <see cref="AnimationDictionary"/> and name.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="name"></param>
        public AnimationData(AnimationDictionary dictionary, string name)
        {
            Dictionary = dictionary;
            Name = name;
        }

        /// <summary>
        /// Gets the <see cref="AnimationDictionary"/> name
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Dictionary.Name;
    }
}
