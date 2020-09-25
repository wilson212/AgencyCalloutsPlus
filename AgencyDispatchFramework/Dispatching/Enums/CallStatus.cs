namespace AgencyDispatchFramework.Dispatching
{
    public enum CallStatus
    {
        /// <summary>
        /// Indicates that the call has been created
        /// </summary>
        Created,

        /// <summary>
        /// Indcates that the call has been assigned to the player unit,
        /// but not actively dispatched yet (could be due to busy radio)
        /// </summary>
        Assigned,

        /// <summary>
        /// Indicates that the call is currently waiting for the player
        /// to accept it.
        /// </summary>
        Waiting,

        /// <summary>
        /// Indicates that the call has been dispatched to an <see cref="OfficerUnit"/>
        /// </summary>
        Dispatched,

        /// <summary>
        /// Indicates that an <see cref="OfficerUnit"/> is on scene
        /// </summary>
        OnScene,

        /// <summary>
        /// Indicates that the call has been completed
        /// </summary>
        Completed
    }
}
