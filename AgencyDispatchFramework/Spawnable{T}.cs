namespace AgencyDispatchFramework
{
    /// <summary>
    /// A class that provides basic functionality to be used in the
    /// <see cref="ProbabilityGenerator{T}"/> class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Spawnable<T> : ISpawnable
    {
        /// <summary>
        /// Gets the probability of the <typeparamref name="T"/> contained in this instance to be spawned
        /// </summary>
        public int Probability { get; protected set; }

        /// <summary>
        /// Gets the value of <typeparamref name="T"/> contained in this instance
        /// </summary>
        public T Value { get; protected set; }

        /// <summary>
        /// Creates a new instance of <see cref="Spawnable{T}"/>
        /// </summary>
        /// <param name="probability">
        /// The probability of this item being randomly spawned against
        /// the other <typeparamref name="T"/> items in a <see cref="ProbabilityGenerator{T}"/>
        /// </param>
        /// <param name="item">The item being spawned</param>
        public Spawnable(int probability, T item)
        {
            Probability = probability;
            Value = item;
        }
    }
}