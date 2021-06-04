namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// An event fired when the <see cref="Weather"/> changes in game.
    /// </summary>
    /// <param name="oldWeather"></param>
    /// <param name="newWeather"></param>
    public delegate void WeatherChangedEventHandler(Weather oldWeather, Weather newWeather);
}
