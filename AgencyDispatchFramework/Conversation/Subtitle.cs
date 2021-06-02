using Rage;
using System;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a line to be displayed in game in the <see cref="SubtitleQueue"/>
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
        /// Gets or sets the <see cref="Ped"/> to run the <see cref="AnimationTask"/> on if any
        /// </summary>
        public Ped Speaker { get; set; }

        /// <summary>
        /// Gets or sets the animation dictionary name
        /// </summary>
        public string AnimationDictionaryName { get; set; }

        /// <summary>
        /// Gets or sets an animation to play while this <see cref="Subtitle"/> is displayed
        /// </summary>
        public string AnimationName { get; set; }

        /// <summary>
        /// Event fired as the message is being displayed on screen
        /// </summary>
        public event SubtitleEventHandler OnDisplayed;

        /// <summary>
        /// Event fired after the message has displayed for the time specified on screen
        /// </summary>
        public event SubtitleEventHandler Elapsed;

        /// <summary>
        /// Determines whether a animation name and dictionary are set
        /// </summary>
        private bool AnimationAppearsValid
        {
            get => (!String.IsNullOrWhiteSpace(AnimationDictionaryName) && !String.IsNullOrWhiteSpace(AnimationName));
        }

        /// <summary>
        /// Determines whether the <see cref="Ped"/> is valid to play an animation on
        /// </summary>
        private bool SpeakerAppearsValid
        {
            get => (Speaker?.Exists() ?? false && Speaker.IsAlive && Speaker.IsVisible);
        }

        /// <summary>
        /// Displays this sentance on screen and then waits
        /// for the time specified in <see cref="Sentance.Time"/>.
        /// ONLY CALL THIS METHOD FROM A CHILD <see cref="GameFiber"/>
        /// </summary>
        public void Display()
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

            // Invoke animaiton?
            if (AnimationAppearsValid && SpeakerAppearsValid)
            {
                try
                {
                    // Clear animation if its still playing
                    //Speaker.Tasks.ClearSecondary();
                    Speaker.Tasks.PlayAnimation(AnimationDictionaryName, AnimationName, 1f, AnimationFlags.SecondaryTask);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
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
