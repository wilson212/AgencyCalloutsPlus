using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A Probability based random item generator.
    /// </summary>
    /// <remarks>P( T item ) = item.Probability / CumulativeProbability</remarks>
    /// <typeparam name="T"></typeparam>
    public sealed class ProbabilityGenerator<T> where T : ISpawnable
    {
        /// <summary>
        /// A true random number generator
        /// </summary>
        private CryptoRandom Randomizer = new CryptoRandom();

        /// <summary>
        /// Internal list of items that can be generated using the <see cref="Spawn()"/> 
        /// or <see cref="TrySpawn(out T)"/> methods
        /// </summary>
        private List<ProbableItem<T>> Items;

        /// <summary>
        /// Gets the number of items stored in this <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public int ItemCount => Items.Count;

        /// <summary>
        /// The Cumulative Probability of all the spawnable objects
        /// </summary>
        public int CumulativeProbability { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public ProbabilityGenerator()
        {
            Items = new List<ProbableItem<T>>();
        }

        /// <summary>
        /// Creates a new instance of <see cref="ProbabilityGenerator{T}"/>, and adds
        /// the <paramref name="objects"/> to the internal pool
        /// </summary>
        /// <param name="objects"></param>
        public ProbabilityGenerator(IEnumerable<T> objects)
        {
            Items = new List<ProbableItem<T>>();

            AddRange(objects);
        }

        /// <summary>
        /// Adds the item to the item pool
        /// </summary>
        /// <param name="obj"></param>
        public void Add(T obj)
        {
            var spawnable = new ProbableItem<T>(this, obj, CumulativeProbability);
            CumulativeProbability = spawnable.MaxThreshold;
            Items.Add(spawnable);
        }

        /// <summary>
        /// Adds a range of items to the item pool
        /// </summary>
        /// <param name="objects"></param>
        public void AddRange(IEnumerable<T> objects)
        {
            foreach (var o in objects)
            {
                var spawnable = new ProbableItem<T>(this, o, CumulativeProbability);
                CumulativeProbability = spawnable.MaxThreshold;
                Items.Add(spawnable);
            }
        }

        /// <summary>
        /// Gets all items from this <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        /// <returns></returns>
        public ProbableItem<T>[] GetItemPool()
        {
            return Items.ToArray();
        }

        /// <summary>
        /// Gets all items from this <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        /// <returns></returns>
        public T[] GetItems()
        {
            return Items.Select(x => x.Item).ToArray();
        }

        /// <summary>
        /// Clears all items in this <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public void Clear()
        {
            Items.Clear();
        }

        /// <summary>
        /// Returns an instance of <typeparamref name="T"/> based off of the 
        /// RNG probability of that instance.
        /// </summary>
        /// <returns></returns>
        public T Spawn()
        {
            // Ensure we have at least 1 object to spawn
            if (Items.Count == 0)
                throw new Exception("There are no spawnable entities");

            // If we have just 1 item, return that
            if (Items.Count == 1)
                return Items.First().Item;

            // Generate the next random number
            var i = Randomizer.Next(1, CumulativeProbability);
            return (from s in Items where s.ContainsThreshold(i) select s.Item).First();
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
            if (Items.Count == 0)
            {
                return false;
            }
            else if (Items.Count == 1)
            {
                // If we have just 1 item, return that
                retVal = Items.First().Item;
                return true;
            }

            // Generate the next random number
            try
            {
                var i = Randomizer.Next(1, CumulativeProbability);
                retVal = (from s in Items where s.ContainsThreshold(i) select s.Item).First();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Rebuilds the internal item pool
        /// </summary>
        internal void Rebuild()
        {
            T[] items = GetItems();

            Items.Clear();
            CumulativeProbability = 0;

            AddRange(items);
        }
    }
}
