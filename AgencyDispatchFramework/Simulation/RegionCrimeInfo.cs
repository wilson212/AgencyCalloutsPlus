namespace AgencyDispatchFramework.Simulation
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
        /// Gets the average number of calls per In game hour
        /// </summary>
        public double AverageCallsPerGameHour => (AverageCrimeCalls / 6d);

        /// <summary>
        /// Gets the average milliseconds in real time per call
        /// </summary>
        public int AverageMillisecondsPerCall { get; set; }
    }
}
