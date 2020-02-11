using Rage;
using System;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// A class that translate game time to real time and vise-versa using the game's TimeScale
    /// </summary>
    public static class TimeScale
    {
        /// <summary>
        /// Converts seconds in real life to seconds in game
        /// </summary>
        /// <param name="realSeconds"></param>
        /// <returns></returns>
        public static double GameSecondsFromRealSeconds(int realSeconds)
        {
            return realSeconds * Settings.TimeScale;
        }

        /// <summary>
        /// Converts seconds in game to real life seconds
        /// </summary>
        /// <param name="gameSeconds"></param>
        /// <returns></returns>
        public static double RealSecondsFromGameSeconds(int gameSeconds)
        {
            if (gameSeconds == 0) return 0;
            return Math.Round(gameSeconds / (double)Settings.TimeScale, 5);
        }

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> in real life to
        /// a <see cref="TimeSpan"/> in game
        /// </summary>
        /// <param name="realTime"></param>
        /// <returns></returns>
        public static TimeSpan ToGameTime(TimeSpan realTime)
        {
            var total = realTime.TotalSeconds * Settings.TimeScale;
            return TimeSpan.FromSeconds(total);
        }

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> in game time to
        /// a <see cref="TimeSpan"/> in real time
        /// </summary>
        /// <param name="realTime"></param>
        /// <returns></returns>
        public static TimeSpan ToRealTime(TimeSpan gameTime)
        {
            var total = gameTime.TotalSeconds / Settings.TimeScale;
            return TimeSpan.FromSeconds(total);
        }
    }
}
