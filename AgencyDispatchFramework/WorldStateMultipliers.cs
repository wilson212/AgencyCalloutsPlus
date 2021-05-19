using AgencyDispatchFramework.Game;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// 
    /// </summary>
    public class WorldStateMultipliers
    {
        /// <summary>
        /// Gets or sets the base probability
        /// </summary>
        public int BaseProbability { get; set; }

        /// <summary>
        /// Gets the probabilities for each time of day and weather category
        /// </summary>
        private Dictionary<Tuple<TimeOfDay, WeatherCatagory>, int> Probabilities { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldStateMultipliers"/>
        /// </summary>
        public WorldStateMultipliers(int baseProbability = 1)
        {
            BaseProbability = baseProbability;
            Probabilities = new Dictionary<Tuple<TimeOfDay, WeatherCatagory>, int>(20);
            foreach (TimeOfDay tod in Enum.GetValues(typeof(TimeOfDay)))
            {
                foreach (WeatherCatagory catagory in Enum.GetValues(typeof(WeatherCatagory)))
                {
                    Probabilities.Add(new Tuple<TimeOfDay, WeatherCatagory>(tod, catagory), 0);
                }
            }
        }

        /// <summary>
        /// Sets the probability of a <see cref="TimeOfDay"/> and <see cref="WeatherCatagory"/> combination
        /// </summary>
        /// <param name="tod"></param>
        /// <param name="catagory"></param>
        /// <param name="value"></param>
        internal void SetProbability(TimeOfDay tod, WeatherCatagory catagory, int value)
        {
            var key = new Tuple<TimeOfDay, WeatherCatagory>(tod, catagory);
            Probabilities[key] = value;
        }

        /// <summary>
        /// Gets the current probability
        /// </summary>
        /// <returns></returns>
        public int Calculate()
        {
            var key = new Tuple<TimeOfDay, WeatherCatagory>(GameWorld.CurrentTimeOfDay, GetWeatherCatagory());
            return BaseProbability * Probabilities[key];
        }

        /// <summary>
        /// Converts a <see cref="Weather"/> to a <see cref="WeatherCatagory"/>
        /// </summary>
        /// <returns></returns>
        private WeatherCatagory GetWeatherCatagory()
        {
            // Weather
            switch (GameWorld.CurrentWeather)
            {
                default:
                case Weather.Clear:
                case Weather.ExtraSunny:
                case Weather.Neutral:
                    return WeatherCatagory.Clear;
                case Weather.Blizzard:
                case Weather.Christmas:
                case Weather.Snowing:
                case Weather.Snowlight:
                    return WeatherCatagory.Snow;
                case Weather.Raining:
                    return WeatherCatagory.Rain;
                case Weather.Clearing:
                case Weather.Clouds:
                case Weather.Foggy:
                case Weather.Overcast:
                case Weather.Smog:
                    return WeatherCatagory.Overcast;
                case Weather.ThunderStorm:
                    return WeatherCatagory.Storm;
            }
        }
    }
}
