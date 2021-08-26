using AgencyDispatchFramework.Game;
using System;
using System.Linq;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A wrapper class for a <see cref="ProbabilityGenerator{T}"/> where the <see cref="ISpawnable.Probability"/>
    /// of <see cref="T"/> changes based on the in game <see cref="Weather"/> and <see cref="Game.TimePeriod"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WorldStateProbabilityGenerator<T> : IDisposable
    {
        /// <summary>
        /// Our lock object to prevent threading issues
        /// </summary>
        private System.Object _lock = new System.Object();

        /// <summary>
        /// A cached <see cref="ProbabilityGenerator{T}"/> that is current to our last know 
        /// <see cref="GameWorld.CurrentTimePeriod"/> and <see cref="GameWorld.CurrentWeather"/>
        /// </summary>
        private ProbabilityGenerator<WorldStateSpawnable<T>> Generator { get; set; }

        /// <summary>
        /// Indicates whether this instance is disposed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// The Cumulative Probability of all the spawnable objects
        /// </summary>
        public int CumulativeProbability => Generator.CumulativeProbability;

        /// <summary>
        /// Creates a new instance of <see cref="WorldStateProbabilityGenerator{T}"/>
        /// </summary>
        public WorldStateProbabilityGenerator()
        {
            Generator = new ProbabilityGenerator<WorldStateSpawnable<T>>();
            GameWorld.OnWeatherChange += GameWorld_OnWeatherChange;
            GameWorld.OnTimePeriodChanged += GameWorld_OnTimePeriodChanged;
        }

        /// <summary>
        /// Rebuilds the internal <see cref="ProbabilityGenerator{T}"/> with current probabilities based on current
        /// <see cref="GameWorld"/> <see cref="Weather"/> and <see cref="TimePeriod"/>
        /// </summary>
        private void Rebuild()
        {
            if (Disposed) return;

            lock (_lock)
            {
                // Clear the old generator and re-evaluate indexes
                Generator.Rebuild();
            }
        }

        /// <summary>
        /// Adds the object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="multipliers"></param>
        public void Add(T obj, WorldStateMultipliers multipliers)
        {
            if (Disposed) throw new ObjectDisposedException("this");

            var value = new WorldStateSpawnable<T>(obj, multipliers);
            Generator.Add(value);
        }

        /// <summary>
        /// Clears all items in this <see cref="WorldStateProbabilityGenerator{T}"/>
        /// </summary>
        public void Clear()
        {
            if (Disposed) throw new ObjectDisposedException("this");

            Generator.Clear();
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> based off of the 
        /// RNG probability of that instance.
        /// </summary>
        /// <returns></returns>
        public T Spawn()
        {
            if (Disposed) throw new ObjectDisposedException("this");

            return Generator.Spawn().Item;
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> based off of the 
        /// RNG probability of that instance.
        /// </summary>
        /// <returns></returns>
        public bool TrySpawn(out T value)
        {
            if (Disposed) throw new ObjectDisposedException("this");

            bool success = Generator.TrySpawn(out WorldStateSpawnable<T> val);
            value = success ? val.Item : default(T);
            return success;
        }

        /// <summary>
        /// Gets the <see cref="WorldStateMultipliers"/> of an item based on the predicate
        /// </summary>
        /// <param name="predicate">The condition to apply</param>
        internal WorldStateSpawnable<T>[] GetItems()
        {
            return Generator.GetItems().ToArray();
        }

        /// <summary>
        /// Gets the <see cref="WorldStateMultipliers"/> of an item based on the predicate
        /// </summary>
        /// <param name="predicate">The condition to apply</param>
        public WorldStateMultipliers[] GetMultipliersWhere(Func<WorldStateSpawnable<T>, bool> predicate)
        {
            return Generator.GetItems().Where(predicate).Select(x => x.Multipliers).ToArray();
        }

        /// <summary>
        /// Disposes the current instance
        /// </summary>
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;

            lock (_lock)
            {
                // Unregister events
                GameWorld.OnTimePeriodChanged -= GameWorld_OnTimePeriodChanged;
                GameWorld.OnWeatherChange -= GameWorld_OnWeatherChange;

                // Clear generator
                Generator?.Clear();
                Generator = null;
            }
        }

        /// <summary>
        /// Method called when the weather changes in game
        /// </summary>
        /// <param name="oldWeather">The old <see cref="Weather"/> we transitioned from</param>
        /// <param name="newWeather">The new <see cref="Weather"/> we transitioned to</param>
        private void GameWorld_OnWeatherChange(Weather oldWeather, Weather newWeather) => Rebuild();

        /// <summary>
        /// Method called whenever the <see cref="TimePeriod"/> changes in game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameWorld_OnTimePeriodChanged(TimePeriod oldPeriod, TimePeriod period) => Rebuild();
    }
}
