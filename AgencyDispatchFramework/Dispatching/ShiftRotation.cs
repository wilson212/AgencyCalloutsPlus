namespace AgencyDispatchFramework.Dispatching
{
    internal enum ShiftRotation
    {
        /// <summary>
        /// Day shift ranges from 6am to 4pm
        /// </summary>
        Day,

        /// <summary>
        /// Swing shift ranges from 2pm to midnight
        /// </summary>
        Swing,

        /// <summary>
        /// Night shift ranges from 10pm to 8am
        /// </summary>
        Night,
    }
}