using System;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// A class that translate game time to real time and vise-versa using the game's TimeScale
    /// </summary>
    public static class TimeScale
    {
        /// <summary>
        /// Timescale is 30:1 (30 seconds in game equals 1 second in real life)
        /// </summary>
        public static readonly double GameTimeScale = 30;

        /// <summary>
        /// Converts seconds in real life to seconds in game
        /// </summary>
        /// <param name="realSeconds"></param>
        /// <returns></returns>
        public static double GameSecondsFromRealSeconds(int realSeconds)
        {
            return realSeconds * GameTimeScale;
        }

        /// <summary>
        /// Converts seconds in game to real life seconds
        /// </summary>
        /// <param name="gameSeconds"></param>
        /// <returns></returns>
        public static double RealSecondsFromGameSeconds(int gameSeconds)
        {
            if (gameSeconds == 0) return 0;
            return Math.Round(gameSeconds / GameTimeScale, 5);
        }

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> in real life to
        /// a <see cref="TimeSpan"/> in game
        /// </summary>
        /// <param name="realTime"></param>
        /// <returns></returns>
        public static TimeSpan ToGameTime(TimeSpan realTime)
        {
            var total = realTime.TotalSeconds * GameTimeScale;
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
            var total = gameTime.TotalSeconds / GameTimeScale;
            return TimeSpan.FromSeconds(total);
        }
    }
}
