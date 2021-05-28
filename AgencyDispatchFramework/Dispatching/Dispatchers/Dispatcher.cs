using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Dispatching
{
    internal abstract class Dispatcher : IDisposable
    {
        /// <summary>
        /// Our lock object to prevent threading issues
        /// </summary>
        protected object _threadLock = new object();

        /// <summary>
        /// Indicates whether this instance is disposed or not
        /// </summary>
        internal bool IsDisposed { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Dispatching.Agency"/> that this
        /// instance is dispatching for
        /// </summary>
        public Agency Agency { get; set; }

        /// <summary>
        /// Gets a list of calls this instance is responsible for handling
        /// </summary>
        public List<PriorityCall> CallQueue { get; set; }

        /// <summary>
        /// Event fired when a <see cref="PriorityCall"/> needs additional resources
        /// that the <see cref="Agency"/> is unable to provide
        /// </summary>
        public abstract event CallRaisedHandler OnCallRaised;

        /// <summary>
        /// Method called every tick to manage calls
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// Creates a new instance of <see cref="Dispatcher"/>
        /// </summary>
        /// <exception cref="ArgumentException">thrown if the <paramref name="agency"/> param is null</exception>
        public Dispatcher(Agency agency)
        {
            Agency = agency ?? throw new ArgumentNullException(nameof(agency));
            CallQueue = new List<PriorityCall>(12);
        }

        /// <summary>
        /// Adds the call to the <see cref="CallQueue"/> safely
        /// </summary>
        /// <param name="call"></param>
        public virtual void AddCall(PriorityCall call)
        {
            // Stop if we are disposed
            if (IsDisposed) throw new ObjectDisposedException(nameof(Dispatcher));

            // Add call to priority Queue
            lock (_threadLock)
            {
                CallQueue.Add(call);
                Log.Debug($"Dispatcher.AddCall(): Added Call to Queue '{call.ScenarioInfo.Name}' in zone '{call.Zone.FullName}'");
            }

            // Register for the end event
            call.OnCallEnded += Call_OnCallEnded;
        }

        /// <summary>
        /// Removes the call to the <see cref="CallQueue"/> safely
        /// </summary>
        /// <param name="call"></param>
        public virtual void RemoveCall(PriorityCall call)
        {
            // Stop if we are disposed
            if (IsDisposed) throw new ObjectDisposedException(nameof(Dispatcher));

            // Unregister
            call.OnCallEnded -= Call_OnCallEnded;

            // Add call to priority Queue
            lock (_threadLock)
            {
                CallQueue.Remove(call);
                Log.Debug($"Dispatcher.AddCall(): Removed Call from Queue '{call.ScenarioInfo.Name}' in zone '{call.Zone.FullName}'");
            }
        }

        /// <summary>
        /// Event method called when a call is ended by the <see cref="Dispatch"/> class
        /// </summary>
        /// <param name="call"></param>
        /// <param name="closeFlag"></param>
        protected virtual void Call_OnCallEnded(PriorityCall call, CallCloseFlag closeFlag)
        {
            RemoveCall(call);
        }

        /// <summary>
        /// Dispatches the provided <see cref="OfficerUnit"/> to the provided
        /// <see cref="PriorityCall"/>. If the <paramref name="officer"/> is
        /// the Player, then the callout is started
        /// </summary>
        /// <param name="officer"></param>
        /// <param name="call"></param>
        public virtual void AssignUnitToCall(OfficerUnit officer, PriorityCall call)
        {
            // Stop if we are disposed
            if (IsDisposed) throw new ObjectDisposedException(nameof(Dispatcher));

            // TODO: Secondary dispatching
            // Assign the officer to the call
            officer.AssignToCall(call, !officer.IsAIUnit);

            // If player, tell dispatch so it can play the radio
            if (!officer.IsAIUnit)
            {
                Dispatch.DispatchedPlayerToCall(call);
            }
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public virtual void Dispose()
        {
            // Check
            if (IsDisposed)
                return;

            // Flag
            IsDisposed = true;

            // Remove call log, and unregister for events
            for (int i = CallQueue.Count - 1; i >= 0; i--)
            {
                var call = CallQueue[i];
                call.OnCallEnded -= Call_OnCallEnded;

                CallQueue.RemoveAt(i);
            }
        }
    }
}
