using AgencyCalloutsPlus.Mod;
using Rage;
using System;
using static Rage.Native.NativeFunction;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="https://github.com/crosire/scripthookvdotnet/blob/main/source/scripting_v3/GTA/World.cs"/>
    public static class GameWorld
    {
        #region Weather & Effects

        internal static readonly string[] WeatherNames = {
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

        /// <summary>
        /// Gets or sets the weather in game
        /// </summary>
        public static Weather CurrentWeather
        {
            get
            {
                var weatherHash = Natives.GetPrevWeatherTypeHashName<uint>();
                for (int i = 0; i < WeatherNames.Length; i++)
                {
                    if (weatherHash == Game.GetHashKey(WeatherNames[i]))
                    {
                        return (Weather)i;
                    }
                }

                return Weather.Unknown;
            }
            set
            {
                if (Enum.IsDefined(typeof(Weather), value) && value != Weather.Unknown)
                {
                    Natives.SetWeatherTypeNow(WeatherNames[(int)value]);
                }
            }
        }

        /// <summary>
        /// Gets the next weather to happen in game
        /// </summary>
        public static Weather NextWeather
        {
            get
            {
                var weatherHash = Natives.GetNextWeatherTypeHashName<uint>();
                for (int i = 0; i < WeatherNames.Length; i++)
                {
                    if (weatherHash == Game.GetHashKey(WeatherNames[i]))
                    {
                        return (Weather)i;
                    }
                }

                return Weather.Unknown;
            }
        }

        #endregion

        /// <summary>
		/// Transitions to the specified weather.
		/// </summary>
		/// <param name="weather">The weather to transition to</param>
		/// <param name="duration">The duration of the transition. If set to zero, the weather 
        /// will transition immediately</param>
		public static void TransitionToWeather(Weather weather, float duration)
        {
            if (Enum.IsDefined(typeof(Weather), weather) && weather != Weather.Unknown)
            {
                Natives.SetWeatherTypeOvertimePersist(WeatherNames[(int)weather], duration);
            }
        }

        /// <summary>
        /// Gets the current world <see cref="WeatherInfo" />
        /// </summary>
        /// <returns></returns>
        public static WeatherInfo GetWeatherInfo()
        {
            return new WeatherInfo();
        }
    }
}
