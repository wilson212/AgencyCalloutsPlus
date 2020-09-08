using System;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// The Range class
    /// </summary>
    /// <typeparam name="T">Generic parameter.</typeparam>
    public class Range<T> where T : IComparable<T>
    {
        /// <summary>Minimum value of the range.</summary>
        public T Minimum { get; set; }

        /// <summary>Maximum value of the range.</summary>
        public T Maximum { get; set; }

        /// <summary>
        /// Creates a new range using the specified minimum and maximum values
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public Range(T min, T max)
        {
            Minimum = min;
            Maximum = max;
        }

        /// <summary>
        /// Compares the range of <typeparamref name="T"/> with a value
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        /// Returns 0 if the value is within this range.
        /// Returns -1 if the value is greater than this range.
        /// Returns 1 if the value is less than this range.
        /// </returns>
        public int CompareTo(T value)
        {
            if (ContainsValue(value)) return 0;
            else return (Maximum.CompareTo(value) > 0) ? 1 : -1;
        }

        /// <summary>
        /// Determines if the range is valid
        /// </summary>
        /// <returns>True if range is valid, else false</returns>
        public bool IsValid()
        {
            return Minimum.CompareTo(Maximum) <= 0;
        }

        /// <summary>
        /// Determines if the provided value is inside the range
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public bool ContainsValue(T value)
        {
            return (Minimum.CompareTo(value) <= 0) && (value.CompareTo(Maximum) <= 0);
        }

        /// <summary>
        /// Determines if this Range is inside the bounds of another range
        /// </summary>
        /// <param name="Range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        public bool IsInsideRange(Range<T> range)
        {
            return IsValid() && range.IsValid() && range.ContainsValue(Minimum) && range.ContainsValue(Maximum);
        }

        /// <summary>
        /// Determines if another range is inside the bounds of this range
        /// </summary>
        /// <param name="Range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        public bool ContainsRange(Range<T> range)
        {
            return this.IsValid() && range.IsValid() && ContainsValue(range.Minimum) && ContainsValue(range.Maximum);
        }

        /// <summary>
        /// Presents the Range in readable format
        /// </summary>
        /// <returns>String representation of the Range</returns>
        public override string ToString()
        {
            return string.Format("[{0} - {1}]", Minimum, Maximum);
        }

        public bool Equals(Range<T> other)
        {
            if (other == null) return false;
            return (other.Minimum.Equals(Minimum) && other.Maximum.Equals(Maximum));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Range<T>);
        }

        public override int GetHashCode()
        {
            return (Minimum, Maximum).GetHashCode();
        }
    }
}
