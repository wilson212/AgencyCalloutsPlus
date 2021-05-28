namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// A delegate to handle the closing of a call
    /// </summary>
    /// <param name="call">The call that has ended</param>
    /// <param name="closeFlag">How the call was ended</param>
    internal delegate void CallEndedHandler(PriorityCall call, CallCloseFlag closeFlag);
}
