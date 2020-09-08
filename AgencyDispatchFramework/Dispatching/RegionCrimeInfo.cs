namespace AgencyDispatchFramework.Dispatching
{
    internal class RegionCrimeInfo
    {
        /// <summary>
        /// Gets the maximum amout of calls to expect from this region
        /// </summary>
        public int MaxCrimeCalls { get; set; }

        /// <summary>
        /// Gets the average crime level index of this Region
        /// </summary>
        public int AverageCrimeCalls { get; set; }

        /// <summary>
        /// Gets the minimum amout of calls to expect from this region
        /// </summary>
        public int MinCrimeCalls { get; set; }

        /// <summary>
        /// Gets the optimum number of patrols to handle the crime load
        /// </summary>
        public int OptimumPatrols { get; set; }

        /// <summary>
        /// Gets the average number of calls per In game hour
        /// </summary>
        public double AverageCallsPerHour => (AverageCrimeCalls / 6d);

        /// <summary>
        /// Gets the average number of calls per In game hour
        /// </summary>
        public int AverageMillisecondsPerCall { get; set; }
    }
}
