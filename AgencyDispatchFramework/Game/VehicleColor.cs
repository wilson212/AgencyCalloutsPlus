using AgencyDispatchFramework.Extensions;
using System;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// Credits to Albo1125: https://github.com/Albo1125/Albo1125-Common/blob/master/Albo1125.Common/CommonLibrary/ExtensionMethods.cs#L298
    /// </summary>
    public class VehicleColor
    {
        /// <summary>
        /// The primary color paint index 
        /// </summary>
        public EPaint PrimaryColor { get; set; }

        /// <summary>
        /// The secondary color paint index 
        /// </summary>
        public EPaint SecondaryColor { get; set; }

        /// <summary>
        /// Gets the primary color name
        /// </summary>
        public string PrimaryColorName
        {
            get => GetColorName(PrimaryColor);
        }
        /// <summary>
        /// Gets the secondary color name
        /// </summary>
        public string SecondaryColorName
        {
            get => GetColorName(SecondaryColor);
        }

        /// <summary>
        /// Creates a new instance of <see cref="VehicleColor"/>
        /// </summary>
        /// <param name="primary"></param>
        /// <param name="secondary"></param>
        public VehicleColor(EPaint primary, EPaint secondary)
        {
            PrimaryColor = primary;
            SecondaryColor = secondary;
        }

        /// <summary>
        /// Gets the color name
        /// </summary>
        /// <param name="paint">Color to get the name from</param>
        /// <returns></returns>
        public static string GetColorName(EPaint paint)
        {
            string name = Enum.GetName(typeof(EPaint), paint);
            return name.Replace("_", " ");
        }
    }
}
