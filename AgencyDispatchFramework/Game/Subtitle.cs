using Rage;
using System;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// Represents a line of text to be displayed in game in the <see cref="SubtitleQueue"/>
    /// </summary>
    public class Subtitle
    {
        /// <summary>
        /// Gets or sets the time in milliseconds to display this message
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Gets or sets the text to display on screen
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the prefix text to display on screen
        /// </summary>
        public string PrefixText { get; set; }

        /// <summary>
        /// Event fired as the message is being displayed on screen
        /// </summary>
        public virtual event SubtitleEventHandler OnDisplayed;

        /// <summary>
        /// Event fired after the message has displayed for the time specified on screen
        /// </summary>
        public virtual event SubtitleEventHandler Elapsed;

        /// <summary>
        /// Creates a new instance of <see cref="Subtitle"/>
        /// </summary>
        public Subtitle(string text, int duration)
        {
            Text = text;
            Duration = duration;
        }

        /// <summary>
        /// Displays this sentance on screen and then waits
        /// for the time specified in <see cref="Subtitle.Time"/>.
        /// ONLY CALL THIS METHOD FROM A CHILD <see cref="GameFiber"/>
        /// </summary>
        public virtual void Display()
        {
            // Display item
            if (!String.IsNullOrEmpty(PrefixText))
            {
                Rage.Game.DisplaySubtitle($"{PrefixText} {Text}", Duration);
            }
            else
            {
                Rage.Game.DisplaySubtitle(Text, Duration);
            }
            
            // Fire event
            OnDisplayed?.Invoke(this);

            // Now wait
            GameFiber.Wait(Duration);

            // Fire event
            Elapsed?.Invoke(this);
        }
    }
}
