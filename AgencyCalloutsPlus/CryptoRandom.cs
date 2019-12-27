using System;
using System.Security.Cryptography;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// A Random number generator that uses the <see cref="RNGCryptoServiceProvider"/> 
    /// to randomize the numbers.
    /// </summary>
    public class CryptoRandom : Random
    {
        private static RNGCryptoServiceProvider RNG = new RNGCryptoServiceProvider();

        private byte[] _uint32Buffer = new byte[4];

        public CryptoRandom() { }
        public CryptoRandom(Int32 ignoredSeed) { }

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
            if (maxValue <= 0)
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

        public override void NextBytes(byte[] b)
        {
            RNG.GetBytes(b);
        }
    }
}
