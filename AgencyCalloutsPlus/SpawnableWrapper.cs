using System;

namespace AgencyCalloutsPlus
{
    internal class SpawnableWrapper<T> where T : ISpawnable
    {
        public T Spawnable { get; protected set; }
        public int MinThreshold { get; protected set; }
        public int MaxThreshold { get; protected set; }

        public SpawnableWrapper(T spawnable, int minThreshold)
        {
            if (spawnable == null)
                throw new ArgumentNullException("spawnable");

            Spawnable = spawnable;
            MinThreshold = minThreshold;
            MaxThreshold = MinThreshold + spawnable.Probability;
        }
    }
}
