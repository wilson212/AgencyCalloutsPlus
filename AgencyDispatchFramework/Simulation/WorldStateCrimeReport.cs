using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using System;
using System.Linq;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// An object used to determine the current crime statistics of a <see cref="WorldZone"/> based on a 
    /// <see cref="TimePeriod"/> and <see cref="Game.Weather"/>
    /// </summary>
    /// <remarks>
    /// This class is uesd to determine how many 
    /// </remarks>
    public class WorldStateCrimeReport
    {
        /// <summary>
        /// Gets the <see cref="TimePeriod"/> of this report
        /// </summary>
        public TimePeriod Period { get; private set; }

        /// <summary>
        /// Gets the <see cref="Game.Weather"/> of this report
        /// </summary>
        public Weather Weather { get; private set; }

        /// <summary>
        /// Gets the <see cref="WorldZone"/> for this report
        /// </summary>
        public WorldZone Zone { get; private set; }

        /// <summary>
        /// Contains our multipliers
        /// </summary>
        protected WorldStateSpawnable<CallCategory>[] Items { get; set; }

        /// <summary>
        /// Gets the total probabilities of our <see cref="WorldStateMultipliers"/>
        /// </summary>
        protected double TotalCallProbability { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldStateCrimeReport"/>
        /// </summary>
        /// <param name="period"></param>
        /// <param name="weather"></param>
        /// <param name="zone"></param>
        public WorldStateCrimeReport(TimePeriod period, Weather weather, WorldZone zone)
        {
            Period = period;
            Weather = weather;
            Zone = zone;

            // Get items and total probability
            Items = zone.CallCategoryGenerator.GetItems();
            TotalCallProbability = Items.Sum(x => x.Multipliers.Calculate(Period, Weather));
        }

        /// <summary>
        /// Gets the percentage chance of that a call with the specified <see cref="CallCategory"/> types
        /// will spawn
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public double GetCallPercentageOf(params CallCategory[] items)
        {
            // Get total probability
            var sum = Items.Where(x => items.Contains(x.Item)).Sum(x => x.Multipliers.Calculate(Period, Weather));
            return Math.Round(sum / TotalCallProbability, 2);
        }

        /// <summary>
        /// Gets the expected call count of the specified <see cref="CallCategory"/> types
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public double GetExpectedCallCountsOf(params CallCategory[] items)
        {
            // Calculate
            var sum = GetCallPercentageOf(items);
            return Math.Round(Zone.AverageCalls[Period] * sum, 2);
        }
    }
}