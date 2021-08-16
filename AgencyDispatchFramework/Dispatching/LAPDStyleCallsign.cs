using System;

namespace AgencyDispatchFramework.Dispatching
{
    public class LAPDStyleCallsign : CallSign
    {
        /// <summary>
        /// Gets the division
        /// </summary>
        public int Division { get; internal set; }

        /// <summary>
        /// Gets the unit name
        /// </summary>
        public string Unit { get; internal set; }

        /// <summary>
        /// Gets the beat
        /// </summary>
        public int Beat { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="division"></param>
        /// <param name="phoneticUnitId"></param>
        /// <param name="beat"></param>
        public LAPDStyleCallsign(int division, int phoneticUnitId, int beat)
        {
            // Ensure division ID is in range
            if (!division.InRange(1, 10))
                throw new ArgumentException("Callsign division number out of range", nameof(division));

            // Ensure division ID is in range
            if (!phoneticUnitId.InRange(1, 26))
                throw new ArgumentException("Callsign phoneticUnitId number out of range", nameof(phoneticUnitId));

            // Ensure division ID is in range
            if (!beat.InRange(1, 24))
                throw new ArgumentException("Callsign beat number out of range", nameof(beat));

            Division = division;
            Unit = Dispatch.LAPDphonetic[phoneticUnitId - 1];
            Beat = beat;

            char unit = char.ToUpper(Unit[0]);
            Value = $"{Division}{unit}-{Beat}";
        }

        /// <summary>
        /// Gets the audio string for the dispatch radio
        /// </summary>
        /// <returns></returns>
        public override string GetRadioString()
        {
            // Pad zero
            var divString = Division.ToString("D2");
            var beatString = Beat.ToString("D2");
            return $"DIV_{divString} {Unit} BEAT_{beatString}";
        }

        public override string ToString() => Value;
    }
}
