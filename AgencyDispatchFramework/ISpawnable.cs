namespace AgencyDispatchFramework
{
    /// <summary>
    /// Represents a spawnable item from a <see cref="ProbabilityGenerator{T}"/>
    /// </summary>
    public interface ISpawnable
    {
        /// <summary>
        /// Gets the probability of this item be spawned against other
        /// items within the item pool
        /// </summary>
        int Probability { get; }
    }
}
