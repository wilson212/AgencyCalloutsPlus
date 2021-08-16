using System;
using System.Security.Cryptography;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A true random number generator that uses the <see cref="RNGCryptoServiceProvider"/> 
    /// to randomize the numbers.
    /// </summary>
    public class CryptoRandom : Random
    {
        /// <summary>
        /// Random service provider
        /// </summary>
        private static RNGCryptoServiceProvider RNG = new RNGCryptoServiceProvider();

        /// <summary>
        /// The Int32 byte buffer
        /// </summary>
        private byte[] _uint32Buffer = new byte[4];

        public CryptoRandom() { }
        public CryptoRandom(Int32 ignoredSeed) { }

        /// <summary>
        /// Picks a random element of <typeparamref name="T"/> that is provided in
        /// the <paramref name="items"/> list and returns it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public T PickOne<T>(params T[] items)
        {
            // Need at least 1 item
            if (items.Length == 0)
                throw new ArgumentOutOfRangeException("items");

            // If ony a single item, return that
            if (items.Length == 1)
                return items[0];

            // Grab random index
            int index = Next(1, items.Length);
            return items[index - 1];
        }

        /// <summary>
        /// Gets the next random number in the sequence
        /// </summary>
        /// <returns></returns>
        public override Int32 Next()
        {
            RNG.GetBytes(_uint32Buffer);
            return BitConverter.ToInt32(_uint32Buffer, 0) & 0x7FFFFFFF;
        }

        /// <summary>
        /// Gets the next random number in the sequence, using the max integer value
        /// </summary>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public override Int32 Next(Int32 maxValue) => Next(0, maxValue);

        /// <summary>
        /// Gets the next random number in the sequence, using the minimum and
        /// maximum integer values
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public override int Next(int minValue, int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException("maxValue");

            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException("minValue");

            if (minValue == maxValue)
                return minValue;

            Int64 diff = maxValue - minValue;
            while (true)
            {
                RNG.GetBytes(_uint32Buffer);
                UInt32 rand = BitConverter.ToUInt32(_uint32Buffer, 0);
                Int64 max = (1 + (Int64)UInt32.MaxValue);
                Int64 remainder = max % diff;
                if (rand < max - remainder)
                {
                    return (Int32)(minValue + (rand % diff));
                }
            }
        }

        /// <summary>
        /// Gets the next random number within the specified <see cref="Range{T}"/>
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Int32 Next(Range<int> range) => Next(range.Minimum, range.Maximum);

        /// <summary>
        /// Fills an array of bytes with a cryptographically strong sequence of random values.
        /// </summary>
        /// <param name="b"></param>
        public override void NextBytes(byte[] b)
        {
            RNG.GetBytes(b);
        }

        /// <summary>
        /// Gets the next random <see cref="DateTime"/> within the specified range
        /// </summary>
        /// <param name="start">The earliest <see cref="DateTime"/> in range</param>
        /// <param name="end">The latest <see cref="DateTime"/> in range</param>
        /// <returns></returns>
        public DateTime NextDateTime(DateTime start, DateTime end)
        {
            // Calculate cumulative number of seconds between two DateTimes
            double seconds = end.Subtract(start).TotalSeconds;
            if (seconds > Int32.MaxValue)
            {
                // Get a random 64 bit integer
                byte[] uint64Buffer = new byte[8];
                RNG.GetBytes(uint64Buffer);

                double numberOfSecondsToAdd = Convert.ToInt64(uint64Buffer) & 0x7FFFFFFF;
                return start.AddSeconds(numberOfSecondsToAdd);
            }
            else
            {
                // Add random number of seconds to dateTimeFrom
                int numberOfSecondsToAdd = Next((int)seconds);
                return start.AddSeconds(numberOfSecondsToAdd);
            }
        }
    }
}
