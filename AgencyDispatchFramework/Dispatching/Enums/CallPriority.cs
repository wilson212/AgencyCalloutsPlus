namespace AgencyDispatchFramework.Dispatching
{
    public enum CallPriority
    {
        /// <summary>
        /// Calls that are absolute emergencies where someone's
        /// life is in immediate danger.
        /// </summary>
        Immediate = 1,

        /// <summary>
        /// Calls that needs immediate attention and cannot wait,
        /// or are very dangerous and require more than 1 officer, 
        /// but do not pose a current immediate danger to someones life.
        /// </summary>
        Emergency = 2,

        /// <summary>
        /// Calls that need attention as soon as possible, are usually
        /// not dangerous, and must be taken care of!
        /// </summary>
        Expedited = 3,

        /// <summary>
        /// Calls that can be handled whenever an officer is available,
        /// and not needed anywhere more important right now.
        /// </summary>
        Routine = 4
    }
}
