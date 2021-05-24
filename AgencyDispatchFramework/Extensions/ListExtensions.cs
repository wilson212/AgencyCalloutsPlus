using System.Collections.Generic;

namespace AgencyDispatchFramework.Extensions
{
    public static class ListExtensions
    {
        /// <summary>
        /// Shuffles the elements within this <see cref="IList{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this IList<T> list)
        {
            var rng = new CryptoRandom();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
