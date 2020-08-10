using AgencyCalloutsPlus.Mod;
using System.Collections.Generic;

namespace AgencyCalloutsPlus
{
    public class TimeOfDayProbabilityGenerator<T> where T : ISpawnable
    {
        /// <summary>
        /// A dictionary containing our spawn generators
        /// </summary>
        private Dictionary<TimeOfDay, ProbabilityGenerator<T>> Generators { get; set; }

        public TimeOfDayProbabilityGenerator()
        {
            Generators = new Dictionary<TimeOfDay, ProbabilityGenerator<T>>()
            {
                { TimeOfDay.Morning, new ProbabilityGenerator<T>() },
                { TimeOfDay.Day, new ProbabilityGenerator<T>() },
                { TimeOfDay.Evening, new ProbabilityGenerator<T>() },
                { TimeOfDay.Night, new ProbabilityGenerator<T>() },
            };
        }

        public void Add(TimeOfDay time, T obj)
        {
            Generators[time].Add(obj);
        }

        public void AddRange(TimeOfDay time, IEnumerable<T> objects)
        {
            Generators[time].AddRange(objects);
        }

        /// <summary>
        /// Gets all items from this <see cref="SpawnGenerator{T}"/>
        /// </summary>
        /// <returns></returns>
        public T[] GetItems(TimeOfDay time)
        {
            return Generators[time].GetItems();
        }

        /// <summary>
        /// Clears all items in this <see cref="SpawnGenerator{T}"/>
        /// </summary>
        public void Clear()
        {
            Generators[TimeOfDay.Morning].Clear();
            Generators[TimeOfDay.Day].Clear();
            Generators[TimeOfDay.Evening].Clear();
            Generators[TimeOfDay.Night].Clear();
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> based off of the 
        /// RNG probability of that instance.
        /// </summary>
        /// <returns></returns>
        public T Spawn(TimeOfDay time)
        {
            return Generators[time].Spawn();
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> based off of the 
        /// RNG probability of that instance.
        /// </summary>
        /// <returns></returns>
        public bool TrySpawn(TimeOfDay time, out T value)
        {
            return Generators[time].TrySpawn(out value);
        }
    }
}
