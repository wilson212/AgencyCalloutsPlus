using Rage;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// A snapshot of Wheather information, used mostly within a <see cref="Callouts.CalloutScenario"/>
    /// </summary>
    public class WeatherInfo
    {
        /// <summary>
        /// Indicates whether the roads in game are wet
        /// </summary>
        public bool RoadsAreWet { get; set; }

        /// <summary>
        /// Indicates whether the game has snowing enabled
        /// </summary>
        public bool IsSnowing { get; set; }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal WeatherInfo()
        {
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
    }
}
