﻿using AgencyDispatchFramework.Game;
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
        private Dictionary<Tuple<TimePeriod, WeatherCatagory>, int> Probabilities { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldStateMultipliers"/>
        /// </summary>
        public WorldStateMultipliers(int baseProbability = 1)
        {
            BaseProbability = Math.Max(baseProbability, 1);
            Probabilities = new Dictionary<Tuple<TimePeriod, WeatherCatagory>, int>(20);
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                foreach (WeatherCatagory catagory in Enum.GetValues(typeof(WeatherCatagory)))
                {
                    Probabilities.Add(new Tuple<TimePeriod, WeatherCatagory>(period, catagory), 0);
                }
            }
        }

        /// <summary>
        /// Sets the probability of a <see cref="TimePeriod"/> and <see cref="Weather"/> combination
        /// </summary>
        /// <param name="period"></param>
        /// <param name="catagory"></param>
        /// <param name="value"></param>
        public void SetProbability(TimePeriod period, Weather weather, int value)
        {
            var key = new Tuple<TimePeriod, WeatherCatagory>(period, GetWeatherCatagory(weather));
            Probabilities[key] = value;
        }

        /// <summary>
        /// Sets the probability of a <see cref="TimePeriod"/> and <see cref="WeatherCatagory"/> combination
        /// </summary>
        /// <param name="period"></param>
        /// <param name="catagory"></param>
        /// <param name="value"></param>
        internal void SetProbability(TimePeriod period, WeatherCatagory catagory, int value)
        {
            var key = new Tuple<TimePeriod, WeatherCatagory>(period, catagory);
            Probabilities[key] = value;
        }

        /// <summary>
        /// Gets the current probability
        /// </summary>
        /// <returns></returns>
        public int Calculate()
        {
            var key = new Tuple<TimePeriod, WeatherCatagory>(
                GameWorld.CurrentTimePeriod, 
                GetWeatherCatagory(GameWorld.CurrentWeather)
            );
            return BaseProbability * Probabilities[key];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="period"></param>
        /// <param name="weather"></param>
        /// <returns></returns>
        public int Calculate(TimePeriod period, Weather weather)
        {
            var key = new Tuple<TimePeriod, WeatherCatagory>(period, GetWeatherCatagory(weather));
            return BaseProbability * Probabilities[key];
        }

        /// <summary>
        /// Converts a <see cref="Weather"/> to a <see cref="WeatherCatagory"/>
        /// </summary>
        /// <returns></returns>
        private WeatherCatagory GetWeatherCatagory(Weather weather)
        {
            // Weather
            switch (weather)
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
