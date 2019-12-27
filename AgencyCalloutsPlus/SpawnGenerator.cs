using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// Guaranteed spawn generator. The cumulative probability is set to the max
    /// threshold, not 1000!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SpawnGenerator<T> where T : ISpawnable
    {
        private CryptoRandom Randomizer = new CryptoRandom();
        private ICollection<SpawnableWrapper> SpawnableEntities;
        private bool TypeIsCloneable = false;

        /// <summary>
        /// The Cumulative Probability of all the spawnable objects
        /// </summary>
        public int CumulativeProbability { get; protected set; }

        public SpawnGenerator()
        {
            TypeIsCloneable = typeof(ICloneable).IsAssignableFrom(typeof(T));
            SpawnableEntities = new List<SpawnableWrapper>();
        }

        public SpawnGenerator(IEnumerable<T> objects)
        {
            TypeIsCloneable = typeof(ICloneable).IsAssignableFrom(typeof(T));
            SpawnableEntities = new List<SpawnableWrapper>();

            AddRange(objects);
        }

        public void Add(T obj)
        {
            var spawnable = new SpawnableWrapper(obj, CumulativeProbability);
            CumulativeProbability = spawnable.MaxThreshold;
            SpawnableEntities.Add(spawnable);
        }

        public void AddRange(IEnumerable<T> objects)
        {
            foreach (var o in objects)
            {
                var spawnable = new SpawnableWrapper(o, CumulativeProbability);
                CumulativeProbability = spawnable.MaxThreshold;
                SpawnableEntities.Add(spawnable);
            }
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> based off of the 
        /// RNG probability of that instance.
        /// </summary>
        /// <returns></returns>
        public T Spawn()
        {
            // Ensure we have at least 1 object to spawn
            if (SpawnableEntities.Count == 0)
                throw new Exception("There are no spawnable entities");

            // Generate the next random number
            var i = Randomizer.Next(0, CumulativeProbability);
            var retVal = (from s in this.SpawnableEntities
                          where (s.MaxThreshold > i && s.MinThreshold <= i)
                          select s.Spawnable).FirstOrDefault();

            // Note that it can spawn null (no spawn) if probabilities dont add up to 1000
            //return TypeIsCloneable ? (T)retVal.Clone() : retVal;
            return retVal;
        }

        private class SpawnableWrapper
        {
            public T Spawnable { get; protected set; }
            public int MinThreshold { get; protected set; }
            public int MaxThreshold { get; protected set; }

            public SpawnableWrapper(T spawnable, int minThreshold)
            {
                Spawnable = spawnable;
                MinThreshold = minThreshold;
                MaxThreshold = MinThreshold + spawnable.Probability;
            }
        }
    }
}
