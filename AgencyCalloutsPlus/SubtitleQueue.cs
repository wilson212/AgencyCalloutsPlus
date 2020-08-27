using AgencyCalloutsPlus.Mod.Conversation;
using Rage;
using System.Collections.Generic;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// Represents a Queue for sub titles to display on screen. Unliked <see cref="Rage.Game.DisplaySubtitle(string)"/>,
    /// A subtitle queued here will not be immediatly dismissed as more Sub titles are queued at once.
    /// </summary>
    public static class SubtitleQueue
    {
        /// <summary>
        /// A lock object to prevent threading issues
        /// </summary>
        private static object _lock = new object();

        /// <summary>
        /// Contains our subtitle lines to display.
        /// </summary>
        private static Queue<Subtitle> LineQueue { get; set; }

        /// <summary>
        /// The GameFiber responsible for timing the displays
        /// </summary>
        private static GameFiber Fiber { get; set; }

        /// <summary>
        /// Gets whether the <see cref="SubtitleQueue"/> is activly running. See <see cref="Begin()"/>
        /// to start the queue.
        /// </summary>
        public static bool IsEnabled { get; private set; }

        /// <summary>
        /// Static constructor
        /// </summary>
        static SubtitleQueue()
        {
            LineQueue = new Queue<Subtitle>();
        }

        /// <summary>
        /// Stops the <see cref="SubtitleQueue"/> <see cref="GameFiber"/> from displaying
        /// any futher queued items. This does NOT clear the current <see cref="Queue{T}"/>
        /// </summary>
        public static void Stop()
        {
            IsEnabled = false;
            Fiber = null;
        }

        /// <summary>
        /// Initiates the <see cref="SubtitleQueue"/> and starts displaying any queued sub titles,
        /// as well as any added from here on out.
        /// </summary>
        private static void Begin()
        {
            if (IsEnabled) return;

            IsEnabled = true;
            Fiber = GameFiber.StartNew(() =>
            {
                Subtitle item = null;
                while (IsEnabled)
                {
                    // Always yield in a continuous GameFiber!
                    GameFiber.Yield();

                    // Lock to prevent threading issues
                    lock (_lock)
                    {
                        // Ensure we have at least one item!
                        if (LineQueue.Count == 0)
                        {
                            IsEnabled = false;
                            Fiber = null;
                            break;
                        }

                        item = LineQueue.Dequeue();
                    }

                    // Display sentance
                    item.Display();
                }
            });
        }

        /// <summary>
        /// Adds a new <see cref="Sentance"/> to the queue
        /// </summary>
        /// <param name="line"></param>
        public static void Add(Subtitle line)
        {
            lock (_lock)
            {
                LineQueue.Enqueue(line);
            }

            if (!IsEnabled) Begin();
        }

        /// <summary>
        /// Adds a new subtitle text to the queue, and displays it for the specified time.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="timeMS"></param>
        public static void Add(string line, int timeMS)
        {
            lock (_lock)
            {
                LineQueue.Enqueue(new Subtitle() { Text = line, Duration = timeMS });
            }

            if (!IsEnabled) Begin();
        }

        /// <summary>
        /// Clears the current <see cref="Queue{T}"/> of any pending sub titles.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                LineQueue.Clear();
            }
        }
    }
}
