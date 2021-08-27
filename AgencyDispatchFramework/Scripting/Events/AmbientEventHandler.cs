using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Scripting.Events
{
    public static class AmbientEventHandler
    {
        /// <summary>
        /// An event counter used to assign unique ambient event ids
        /// </summary>
        private static int EventCounter { get; set; } = 0;

        /// <summary>
        /// Used internally to get a new id for an ambient event
        /// </summary>
        /// <returns></returns>
        internal static int GetNewEventId()
        {
            return EventCounter++;
        }

        /// <summary>
        /// Contains a hashset of active events in the <see cref="Rage.World"/> from this mod
        /// </summary>
        private static HashSet<AmbientEvent> ActiveEvents { get; set; }

        /// <summary>
        /// Removes, activates and processes active <see cref="AmbientEvent"/>s
        /// </summary>
        internal static void Process()
        {
            // If we have no events, quit
            if (ActiveEvents.Count == 0)
                return;

            // Remove old
            int removed = ActiveEvents.RemoveWhere(x => x.IsDisposed);
            if (removed > 0)
            {
                // Log how many we removed
                Log.Info($"Cleaned up {removed} AmbientEvent(s)");
            }

            // Ticking action
        }
    }
}
