using System;
using System.Text.RegularExpressions;

namespace AgencyDispatchFramework.Dispatching
{
    public abstract class CallSign
    {
        /// <summary>
        /// Gets the formatted Division-UnitType-Beat for this unit to be used in strings
        /// </summary>
        public string Value { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns></returns>
        public static bool TryParse(string input, out CallSign callSign)
        {
            // Default
            callSign = null;

            try
            {
                // Check for number style first
                if (Int32.TryParse(input, out int num))
                {
                    callSign = new NumberStyleCallsign(num);
                    return true;
                }
                else
                {
                    var match = Regex.Match(input, "^(?<name>[1-9]{1,2})(?:[-]?)(?<unit>[A-Za-z]+)(?:[-]?)(?<beat>[1-9]{1,2})$");
                    if (match.Success)
                    {
                        // Important to remember that match.Groups[0] is the entire string! //

                        // Parse division
                        if (!Int32.TryParse(match.Groups[1].Value, out int div))
                        {
                            return false;
                        }

                        // Parse Beat
                        if (!Int32.TryParse(match.Groups[3].Value, out int beat))
                        {
                            return false;
                        }

                        // Get alpha index of the unit
                        var unit = match.Groups[2].Value;
                        int index = (int)unit[0] % 32;

                        // Create callsign
                        callSign = new LAPDStyleCallsign(div, index, beat);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return false;
            }
            
            return false;
        }

        /// <summary>
        /// Gets the audio string for the dispatch radio
        /// </summary>
        /// <returns></returns>
        public abstract string GetRadioString();

        /// <summary>
        /// Gets the string representation of this callsign
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Value;
    }
}
