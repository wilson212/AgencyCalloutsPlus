namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Event fired when a <see cref="Dispatcher"/> needs additional resources to complete a call
    /// </summary>
    /// <param name="agency"></param>
    /// <param name="call"></param>
    /// <param name="args"></param>
    internal delegate void CallRaisedHandler(Agency agency, PriorityCall call, CallRaisedEventArgs args);

    public class CallRaisedEventArgs
    {
        public bool NeedsPolice { get; internal set; }

        public bool NeedsEms { get; internal set; }

        public bool NeedsFire { get; internal set; }
    }
}
