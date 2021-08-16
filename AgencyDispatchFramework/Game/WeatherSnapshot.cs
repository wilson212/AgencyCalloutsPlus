using Rage;
using System;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// A snapshot of Wheather information, used mostly within a <see cref="Callouts.CalloutScenario"/>
    /// </summary>
    public class WeatherSnapshot
    {
        /// <summary>
        /// Indicates whether the roads in game are wet
        /// </summary>
        public bool RoadsAreWet { get; internal set; }

        /// <summary>
        /// Indicates whether the game has snowing enabled
        /// </summary>
        public bool IsSnowing { get; internal set; }

        /// <summary>
        /// Gets the <see cref="Game.Weather"/> at the time of this snapshot
        /// </summary>
        public Weather Weather { get; internal set; }

        /// <summary>
        /// Gets the <see cref="DateTime"/> of this snapshot using the in Game time and date
        /// </summary>
        public DateTime DateTime { get; internal set; }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal WeatherSnapshot()
        {
            // Set datetime
            DateTime = World.DateTime;

            // Set if road is wet
            if (World.WaterPuddlesIntensity > 0.0)
            {
                RoadsAreWet = true;
            }

            switch (GameWorld.CurrentWeather)
            {
                case Weather.Blizzard:
                case Weather.Snowing:
                case Weather.Christmas:
                    IsSnowing = true;
                    break;
                case Weather.Raining:
                    RoadsAreWet = true;
                    break;
            }
        }

        /// <summary>
        /// Gets the time since this <see cref="WeatherSnapshot"/> was taken from the current
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetAge()
        {
            var now = World.DateTime;
            return now - DateTime;
        }

        public override string ToString()
        {
            return Weather.ToString();
        }
    }
}
