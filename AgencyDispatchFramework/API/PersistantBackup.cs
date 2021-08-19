using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Simulation;
using System.Collections.Generic;

namespace AgencyDispatchFramework.API
{
    /// <summary>
    /// A Backup interface to be used by UltimateBackup and other "backup" related mods. Using the
    /// methods in this class, backup units will be spawned in game based on the players <see cref="Agency"/>
    /// and staffing availability, and await tasks given by said mods. 
    /// </summary>
    /// <remarks>
    /// Mods are expected to make the <see cref="PersistentAIOfficerUnit"/>s work
    /// in side <see cref="Rage.GameFiber"/>s, and <see cref="AgencyDispatchFramework"/> will not preform any
    /// controlling actions on these units until the <see cref="Dismiss(PersistentAIOfficerUnit[])"/> method is called.
    /// </remarks>
    public static class PersistantBackup
    {
        /// <summary>
        /// Requests backup units for the player. The units returned by this method will be spawned in game at
        /// thier current location, but will not be controlled by <see cref="AgencyDispatchFramework"/>.
        /// </summary>
        /// <param name="type">Indicates the type of units being requested.</param>
        /// <param name="emergency">If true, <see cref="AIOfficerUnit"/>s will leave thier calls to respond to the player.</param>
        /// <param name="count">The number of units needed. You may get less units than requested if none are available.</param>
        /// <param name="useStateOnly">If true, only state level agencies will respond</param>
        /// <returns></returns>
        public static PersistentAIOfficerUnit[] Request(BackupType type, bool emergency,  int count, bool useStateOnly)
        {
            // @todo
            return new PersistentAIOfficerUnit[0];
        }

        /// <summary>
        /// Dismisses and releases the <see cref="PersistentAIOfficerUnit"/> back to <see cref="AgencyDispatchFramework"/>
        /// </summary>
        /// <param name="aiOfficer"></param>
        public static void Dismiss(params PersistentAIOfficerUnit[] aiOfficer)
        {
            // @todo Hand this instance over to GameWorld class, dismiss the ped and wait for
            // the ped to be out of sight of the player before deleting!
        }

        /// <summary>
        /// Dismisses and releases a group of <see cref="PersistentAIOfficerUnit"/>s back to 
        /// <see cref="AgencyDispatchFramework"/>
        /// </summary>
        /// <param name="aiOfficer"></param>
        public static void Dismiss(IEnumerable<PersistentAIOfficerUnit> aiOfficers)
        {
            // @todo Hand this instance over to GameWorld class, dismiss the ped and wait for
            // the ped to be out of sight of the player before deleting!
        }
    }
}
