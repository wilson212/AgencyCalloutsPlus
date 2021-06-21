using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Provides a scanner radio queue for audio messages to be played in game, without the external need
    /// for checking if the scanner radio is busy, and race conditions.
    /// </summary>
    /// <remarks>
    /// Decided not to use ConcurrentQueues here due to concurrent queues having messy memory optimization
    /// </remarks>
    public static class Scanner
    {
        /// <summary>
        /// A lock object to prevent threading issues
        /// </summary>
        private static object _threadLock = new object();

        /// <summary>
        /// A value indicating whether the radio is currently blocked
        /// </summary>
        private static bool _isWaitingPlayerResponse = false;

        /// <summary>
        /// Indicates wether the <see cref="Scanner"/> is currently awaiting player response. If true,
        /// only emergency broadcasts will play until set to false. This property is thread safe and atomic.
        /// </summary>
        public static bool IsWaitingPlayerResponse
        {
            get { lock (_threadLock) return _isWaitingPlayerResponse; }
            set { lock (_threadLock) _isWaitingPlayerResponse = value; }
        }

        /// <summary>
        /// Gets the <see cref="RadioMessage"/> of the player callout, if we are waiting for the audio to play
        /// </summary>
        private static RadioMessage CalloutMessage { get; set; } = null;

        /// <summary>
        /// A queue of <see cref="RadioMessage"/> to send over the radio in game, with low priority
        /// </summary>
        private static Queue<RadioMessage> LowPriorityQueue { get; set; }

        /// <summary>
        /// A queue of <see cref="RadioMessage"/> to send over the radio in game, with high priority
        /// </summary>
        private static Queue<RadioMessage> HighPriorityQueue { get; set; }

        /// <summary>
        /// The GameFiber responsible for timing the displays
        /// </summary>
        private static GameFiber Fiber { get; set; }

        /// <summary>
        /// Gets whether the <see cref="SubtitleQueue"/> is activly running. See <see cref="Begin()"/>
        /// to start the queue.
        /// </summary>
        public static bool IsBusy { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the scanners audio engine is busy
        /// </summary>
        public static bool IsAudioEngineBusy => Functions.GetIsAudioEngineBusy();

        /// <summary>
        /// Static constructor
        /// </summary>
        static Scanner()
        {
            LowPriorityQueue = new Queue<RadioMessage>();
            HighPriorityQueue = new Queue<RadioMessage>();
        }

        /// <summary>
        /// Stops the <see cref="SubtitleQueue"/> <see cref="GameFiber"/> from displaying
        /// any futher queued items. This does NOT clear the current <see cref="Queue{T}"/>
        /// </summary>
        public static void Stop()
        {
            IsBusy = false;
            Fiber = null;
        }

        /// <summary>
        /// Initiates the <see cref="Scanner"/> and starts playing the audio messages.
        /// </summary>
        private static void Begin()
        {
            if (IsBusy) return;

            IsBusy = true;
            Fiber = GameFiber.StartNew(() =>
            {
                RadioMessage item = null;
                do
                {
                    // Check if audio engine is busy
                    if (IsWaitingPlayerResponse || IsAudioEngineBusy)
                    {
                        // Sleep until the radio clears up
                        do
                        {
                            GameFiber.Sleep(1000);
                        }
                        while (IsWaitingPlayerResponse || IsAudioEngineBusy);
                    }

                    // Is the player waiting on a callout?
                    if (CalloutMessage != null)
                    {
                        item = CalloutMessage;
                        CalloutMessage = null;
                    }
                    else
                    {
                        // Prevent race conditions
                        lock (_threadLock)
                        {
                            if (HighPriorityQueue.Count > 0)
                            {
                                item = HighPriorityQueue.Dequeue();
                            }
                            else if (LowPriorityQueue.Count > 0)
                            {
                                item = LowPriorityQueue.Dequeue();
                            }
                            else
                            {
                                IsBusy = false;
                                break;
                            }
                        }
                    }

                    // play audio
                    item.Play();

                    // Finally dispose
                    item.Dispose();

                } while (IsBusy);

                // Clear fiber instance
                Fiber = null;
            });
        }

        /// <summary>
        /// Enqueues a radion message to be played over the in game scanner radio
        /// </summary>
        /// <param name="message"></param>
        public static void PlayRadioMessage(RadioMessage message)
        {
            // Prevent race conditions!
            switch (message.Priority)
            {
                case RadioMessage.MessagePriority.Emergency:
                    message.Play();
                    break;
                case RadioMessage.MessagePriority.High:
                    lock (_threadLock)
                        HighPriorityQueue.Enqueue(message);
                    break;
                default:
                    lock (_threadLock)
                        LowPriorityQueue.Enqueue(message);
                    break;
            }

            // Always call this
            Begin();
        }

        /// <summary>
        /// Queues the callout radio message queued for the player
        /// </summary>
        /// <param name="message"></param>
        internal static void QueueCalloutAudioToPlayer(RadioMessage message)
        {
            // Cancel old
            if (CalloutMessage != null)
            {
                CalloutMessage.Cancel();
            }

            // Set new message
            CalloutMessage = message;

            // Always call this!
            Begin();
        }

        /// <summary>
        /// Clears the scanner audio message block that is set when awaiting a player response
        /// to a previos <see cref="RadioMessage"/>
        /// </summary>
        /// <param name="message"></param>
        internal static void CancelCalloutAudioMessage()
        {
            // Cancel old
            if (CalloutMessage != null)
            {
                CalloutMessage.Dispose();
                CalloutMessage = null;
            }
        }

        /// <summary>
        /// Clears the current <see cref="Queue{T}"/> of any pending radio messages.
        /// </summary>
        internal static void Clear()
        {
            // Only if we are busy!!
            if (!IsBusy) return;
            Stop();

            // Prevent race conditions
            lock (_threadLock)
            {
                RadioMessage item = null;

                // Clear all high priority items
                while (HighPriorityQueue.Count > 0)
                {
                    item = HighPriorityQueue.Dequeue();
                    item.Cancel();
                }

                // Clear all high priority items
                while (LowPriorityQueue.Count > 0)
                {
                    item = LowPriorityQueue.Dequeue();
                    item.Cancel();
                }
            }
        }
    }
}
