using System;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A container class for <typeparamref name="T"/> in a <see cref="ProbabilityGenerator{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ProbableItem<T> where T : ISpawnable
    {
        /// <summary>
        /// Gets the <see cref="ProbabilityGenerator{T}"/> this item is contained in
        /// </summary>
        public ProbabilityGenerator<T> Generator { get; private set; }

        /// <summary>
        /// Gets the contained item that we are spawning from the <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public T Item { get; protected set; }

        /// <summary>
        /// Gets the probability range of this instance
        /// </summary>
        protected Range<int> Threshold { get; set; }

        /// <summary>
        /// Gets the lower probability threshold of this instance
        /// </summary>
        public int MinThreshold => Threshold.Minimum;

        /// <summary>
        /// Gets the upper probability threshold of this instance
        /// </summary>
        public int MaxThreshold => Threshold.Maximum;

        /// <summary>
        /// Gets the total chance as a percentage of the contained item
        /// being selected against the other items in the generator.
        /// </summary>
        public double Chance
        {
            get
            {
                double total = Generator.CumulativeProbability;
                if (total == 0) return 0;

                int range = (MaxThreshold - MinThreshold) + 1;
                return range / total;
            }
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="minThreshold"></param>
        internal ProbableItem(ProbabilityGenerator<T> generator, T item, int minThreshold)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            Generator = generator ?? throw new ArgumentNullException(nameof(generator));
            Item = item;
            Threshold = new Range<int>(minThreshold + 1, minThreshold + item.Probability);
        }

        /// <summary>
        /// Determines if this specified value is in range of the probability range of this item
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ContainsThreshold(int value)
        {
            return Threshold.ContainsValue(value);
        }
    }
}
