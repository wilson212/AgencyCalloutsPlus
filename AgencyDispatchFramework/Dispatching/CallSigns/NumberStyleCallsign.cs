using System;

namespace AgencyDispatchFramework.Dispatching
{
    public class NumberStyleCallsign : CallSign
    {
        /// <summary>
        /// Gets or sets the unit number for this callsign
        /// </summary>
        public int Number { get; internal set; }

        /// <summary>
        /// Gets the audio string for the dispatch radio
        /// </summary>
        internal string RadioCallSign { get; set; } = String.Empty;

        /// <summary>
        /// Defines the <see cref="CallSignStyle"/>
        /// </summary>
        public override CallSignStyle Style => CallSignStyle.Numeric;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        public NumberStyleCallsign(int number)
        {
            // Ensure number is in range
            if (!number.InRange(1, 999))
                throw new ArgumentException("Callsign unit number out of range", nameof(number));

            // Set
            Number = number;
            Value = number.ToString();

            // Create radio call sign string
            int j = 0;
            foreach (char e in Value)
            {
                RadioCallSign += (j == 0) ? $"DIV_{e}" : $" DIV_{e}";
                j++;
            }
        }

        /// <summary>
        /// Gets the audio string for the dispatch radio
        /// </summary>
        /// <returns></returns>
        public override string GetRadioString()
        {
            return RadioCallSign;
        }

        public override string ToString() => Value;
    }
}
