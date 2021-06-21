using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Text;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Contains information to be played over the radio from dispatch
    /// </summary>
    public class RadioMessage  : IDisposable
    {
        /// <summary>
        /// Gets or sets the priority of the message
        /// </summary>
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;

        /// <summary>
        /// Gets or sets the main audio string to play over the radio
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        /// The prefixed call sign that the dispatcher will use
        /// </summary>
        public string TargetCallsign { get; protected set; } = String.Empty;

        /// <summary>
        /// Gets or sets the location to suffix the message with, if any
        /// </summary>
        public Vector3 LocationInfo { get; set; } = Vector3.Zero;

        /// <summary>
        /// Event fired before the message is played using the Audio Engine
        /// </summary>
        public event BeforeRadioPlayedHandler BeforePlayed;

        /// <summary>
        /// Event fired after the message has begun using the Audio Engine
        /// </summary>
        public event RadioMessageEventHandler OnPlaying;

        /// <summary>
        /// Event fired if the message is cancelled
        /// </summary>
        public event RadioMessageEventHandler OnCancelled;

        /// <summary>
        /// Creates a new instance of <see cref="RadioMessage"/>
        /// </summary>
        /// <param name="message"></param>
        public RadioMessage(string message)
        {
            if (String.IsNullOrWhiteSpace(message))
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
            if (String.IsNullOrWhiteSpace(message))
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

        /// <summary>
        /// Plays the message over the radio scanner. This method does not check to see
        /// if the Audio Engine is busy first!
        /// </summary>
        /// <returns></returns>
        internal bool Play()
        {
            // Check for disposed!
            if (Message == null) throw new ObjectDisposedException(nameof(Message));

            // Call event
            var args = new RadioCancelEventArgs();
            BeforePlayed?.Invoke(this, args);

            // Check for cancellation
            if (args.Cancel)
            {
                Cancel();
                return false;
            }

            // Append radio message
            if (LocationInfo != Vector3.Zero)
            {
                Functions.PlayScannerAudioUsingPosition(ToString(), LocationInfo);
            }
            else
            {
                Functions.PlayScannerAudio(ToString());
            }

            return true;
        }

        /// <summary>
        /// This method must be called if the radio message is cancelled internally
        /// </summary>
        internal void Cancel()
        {
            // Call event
            OnCancelled?.Invoke(this);
            Dispose();
        }

        /// <summary>
        /// Disposes this isntance
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public void Dispose()
        {
            Message = null;
            TargetCallsign = null;
        }

        /// <summary>
        /// Gets the completed scanner string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Append target of message
            StringBuilder builder = new StringBuilder();
            if (String.IsNullOrEmpty(TargetCallsign))
            {
                builder.Append("ATTENTION_ALL_UNITS_02 ");
            }
            else
            {
                builder.Append($"DISP_ATTENTION_UNIT {TargetCallsign}");
            }

            // Append radio message
            builder.Append(Message);
            return builder.ToString();
        }

        public override int GetHashCode() => ToString().GetHashCode();

        public override bool Equals(object obj) => Message.Equals(obj);

        public enum MessagePriority
        {
            /// <summary>
            /// Plays the message after all important messages are played, and the radio
            /// is not currently busy
            /// </summary>
            Normal,

            /// <summary>
            /// Plays the message as soon as the radio is available, before any normal
            /// priority messages
            /// </summary>
            High,

            /// <summary>
            /// Plays the radio immediately, interupting any <see cref="RadioMessage"/>
            /// that is currently playing
            /// </summary>
            Emergency
        }
    }
}
