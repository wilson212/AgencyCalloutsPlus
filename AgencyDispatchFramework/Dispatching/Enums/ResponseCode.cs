namespace AgencyDispatchFramework.Dispatching
{
    public enum ResponseCode
    {
        /// <summary>
        /// Respond like normal, following all speed limit signs. 
        /// No lights and no sirens.
        /// </summary>
        Code1 = 1,

        /// <summary>
        /// Lights and sirens to get through congested areas
        /// only. Follow speed limits, but be haste.
        /// </summary>
        Code2 = 2,

        /// <summary>
        /// Full lights and sirens. Do not obey speed limits.
        /// </summary>
        Code3 = 3
    }
}