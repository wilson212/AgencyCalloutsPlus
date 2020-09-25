using Rage;
using System;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Contains information to be played over the radio from dispatch
    /// </summary>
    public class RadioMessage
    {
        /// <summary>
        /// Gets or sets the main audio string to play over the radio
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The prefixed call sign that the dispatcher will use
        /// </summary>
        public string TargetCallsign { get; protected set; } = String.Empty;

        /// <summary>
        /// Gets or sets the location to suffix the message with
        /// </summary>
        public Vector3 LocationInfo { get; set; } = Vector3.Zero;

        /// <summary>
        /// Creates a new instance of <see cref="RadioMessage"/>
        /// </summary>
        /// <param name="message"></param>
        public RadioMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("message", nameof(message));
            }

            Message = message;
        }

        /// <summary>
        /// Creates a new instance of <see cref="RadioMessage"/> targeting the specified <see cref="OfficerUnit"/>
        /// </summary>
        /// <param name="message"></param>
        public RadioMessage(string message, OfficerUnit officerUnit)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("message", nameof(message));
            }

            Message = message;
            SetTarget(officerUnit);
        }

        /// <summary>
        /// Sets the target <see cref="OfficerUnit"/>
        /// </summary>
        /// <param name="unit"></param>
        public void SetTarget(OfficerUnit unit)
        {
            TargetCallsign = unit?.RadioCallSign ?? String.Empty;
        }
    }
}
