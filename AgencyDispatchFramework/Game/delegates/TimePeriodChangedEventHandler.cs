namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// An event fired when the <see cref="TimePeriod"/> changes in game.
    /// </summary>
    /// <param name="oldPeriod"></param>
    /// <param name="newPeriod"></param>
    public delegate void TimePeriodChangedEventHandler(TimePeriod oldPeriod, TimePeriod newPeriod);
}
