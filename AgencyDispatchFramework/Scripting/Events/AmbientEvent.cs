using System;

namespace AgencyDispatchFramework.Scripting.Events
{
    /// <summary>
    /// Base class for an ambient event that happens in the world near the player
    /// </summary>
    public abstract class AmbientEvent : IDisposable, IEquatable<AmbientEvent>
    {
        /// <summary>
        /// Gets the unique event ID. This value is set when added to the <see cref="AmbientEventHandler"/>
        /// </summary>
        public int EventId { get; internal set; }

        /// <summary>
        /// Gets a bool indicating whether this instance is disposed
        /// </summary>
        public bool IsDisposed { get; protected set; }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public virtual void Dispose()
        {
            IsDisposed = true;
        }

        public override int GetHashCode() => EventId.GetHashCode();

        public override bool Equals(object obj) => Equals(obj as AmbientEvent);

        public bool Equals(AmbientEvent other) => (other == null) ? false : other.EventId == EventId;
    }
}
