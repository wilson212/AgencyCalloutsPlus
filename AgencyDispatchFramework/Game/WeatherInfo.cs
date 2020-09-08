using Rage;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// A class that contains Wheather information
    /// </summary>
    public class WeatherInfo
    {
        internal static readonly string[] weatherNames = {
            "EXTRASUNNY",
            "CLEAR",
            "CLOUDS",
            "SMOG",
            "FOGGY",
            "OVERCAST",
            "RAIN",
            "THUNDER",
            "CLEARING",
            "NEUTRAL",
            "SNOW",
            "BLIZZARD",
            "SNOWLIGHT",
            "XMAS",
            "HALLOWEEN"
        };

        public bool RoadsAreWet { get; set; }

        public bool IsSnowing { get; set; }

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
