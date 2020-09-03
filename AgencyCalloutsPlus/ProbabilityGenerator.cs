using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// A Probability based random item generator.
    /// </summary>
    /// <remarks>P( T item ) = item.Probability / CumulativeProbability</remarks>
    /// <typeparam name="T"></typeparam>
    public class ProbabilityGenerator<T> where T : ISpawnable
    {
        private CryptoRandom Randomizer = new CryptoRandom();
        private List<SpawnableWrapper<T>> SpawnableEntities;
        //private bool TypeIsCloneable = false;

        public int ItemCount => SpawnableEntities.Count;

        /// <summary>
        /// The Cumulative Probability of all the spawnable objects
        /// </summary>
        public int CumulativeProbability { get; protected set; }

        public ProbabilityGenerator()
        {
            //TypeIsCloneable = typeof(ICloneable).IsAssignableFrom(typeof(T));
            SpawnableEntities = new List<SpawnableWrapper<T>>();
        }

        public ProbabilityGenerator(IEnumerable<T> objects)
        {
            //TypeIsCloneable = typeof(ICloneable).IsAssignableFrom(typeof(T));
            SpawnableEntities = new List<SpawnableWrapper<T>>();

            AddRange(objects);
        }

        public void Add(T obj)
        {
            var spawnable = new SpawnableWrapper<T>(obj, CumulativeProbability);
            CumulativeProbability = spawnable.MaxThreshold;
            SpawnableEntities.Add(spawnable);
        }

        public void AddRange(IEnumerable<T> objects)
        {
            foreach (var o in objects)
            {
                var spawnable = new SpawnableWrapper<T>(o, CumulativeProbability);
                CumulativeProbability = spawnable.MaxThreshold;
                SpawnableEntities.Add(spawnable);
            }
        }

        /// <summary>
        /// Gets all items from this <see cref="SpawnGenerator{T}"/>
        /// </summary>
        /// <returns></returns>
        public T[] GetItems()
        {
            return SpawnableEntities.Select(x => x.Spawnable).ToArray();
        }

        /// <summary>
        /// Clears all items in this <see cref="SpawnGenerator{T}"/>
        /// </summary>
        public void Clear()
        {
            SpawnableEntities.Clear();
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

            // If we have just 1 item, return that
            if (SpawnableEntities.Count == 1)
                return SpawnableEntities.First().Spawnable;

            // Generate the next random number
            var i = Randomizer.Next(0, CumulativeProbability);
            var retVal = (from s in this.SpawnableEntities
                          where (s.MaxThreshold > i && s.MinThreshold <= i)
                          select s.Spawnable).FirstOrDefault();

            // Note that it can spawn null (no spawn) if probabilities dont add up to 1000
            //return TypeIsCloneable ? (T)retVal.Clone() : retVal;
            return retVal;
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> based off of the 
        /// RNG probability of that instance.
        /// </summary>
        /// <returns></returns>
        public bool TrySpawn(out T retVal)
        {
            // Set to default
            retVal = default(T);

            // Ensure we have at least 1 object to spawn
            if (SpawnableEntities.Count == 0)
            {
                return false;
            }
            else if (SpawnableEntities.Count == 1)
            {
                // If we have just 1 item, return that
                retVal = SpawnableEntities.First().Spawnable;
                return true;
            }

            // Generate the next random number
            try
            {
                var i = Randomizer.Next(0, CumulativeProbability);
                retVal = (from s in SpawnableEntities
                             where (s.MaxThreshold > i && s.MinThreshold <= i)
                             select s.Spawnable).First();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal void Rebuild()
        {
            T[] items = GetItems();

            SpawnableEntities.Clear();
            CumulativeProbability = 0;

            AddRange(items);
        }
    }
}
