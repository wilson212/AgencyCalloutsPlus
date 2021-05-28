using System;

namespace AgencyDispatchFramework.Extensions
{
    public static class CharExtensions
    {
        /// <summary>
        /// Converts a latin character to the corresponding letter's unit type string
        /// </summary>
        /// <param name="value">An upper- or lower-case Latin character</param>
        /// <returns></returns>
        public static string GetUnitStringFromChar(this char value)
        {
            // Uses the uppercase character unicode code point. 'A' = U+0042 = 65, 'Z' = U+005A = 90
            char upper = char.ToUpper(value);
            if (upper < 'A' || upper > 'Z')
            {
                throw new ArgumentOutOfRangeException("value", "This method only accepts standard Latin characters.");
            }

            int index = (int)upper - (int)'A';
            return Dispatch.LAPDphonetic[index];
        }
    }
}
