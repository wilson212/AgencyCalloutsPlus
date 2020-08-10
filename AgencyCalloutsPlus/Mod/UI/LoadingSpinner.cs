using static Rage.Native.NativeFunction;

namespace AgencyCalloutsPlus.Mod.UI
{
    /// <summary>
	/// Methods to manage the display of a loading spinner prompt.
	/// </summary>
    internal class LoadingSpinner
    {
        /// <summary>
		/// Gets a value indicating whether the Loading Prompt is currently being displayed
		/// </summary>
		public static bool IsActive => Natives.BusyspinnerIsOn<bool>();

        /// <summary>
        /// Creates a loading prompt at the bottom right of the screen with the given text and spinner type
        /// </summary>
        /// <param name="loadingText">The text to display next to the spinner</param>
        /// <param name="spinnerType">The style of spinner to draw</param>
        /// <remarks>
        /// <see cref="LoadingSpinnerType.Clockwise1"/>, <see cref="LoadingSpinnerType.Clockwise2"/>, <see cref="LoadingSpinnerType.Clockwise3"/> and <see cref="LoadingSpinnerType.RegularClockwise"/> all see to be the same.
        /// But Rockstar apparently always uses <see cref="LoadingSpinnerType.RegularClockwise"/> in their scripts.
        /// </remarks>
        public static void Show(string loadingText = null, LoadingSpinnerType spinnerType = LoadingSpinnerType.RegularClockwise)
        {
            Hide();

            if (loadingText == null)
            {
                Natives.BeginTextCommandBusyspinnerOn("FM_COR_AUTOD");
            }
            else
            {
                Natives.BeginTextCommandBusyspinnerOn("STRING");
                Natives.AddTextComponentSubstringPlayerName(loadingText);
            }

            Natives.EndTextCommandBusyspinnerOn(spinnerType);
        }

        /// <summary>
        /// Remove the loading prompt at the bottom right of the screen
        /// </summary>
        public static void Hide()
        {
            if (IsActive)
            {
                Natives.BusyspinnerOff();
            }
        }
    }
}
