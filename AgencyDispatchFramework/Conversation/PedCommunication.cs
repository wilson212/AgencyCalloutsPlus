using AgencyDispatchFramework.Game;
using Rage;
using System;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents an advanced <see cref="Subtitle"/> with animation support
    /// </summary>
    public class PedCommunication : Subtitle
    {
        /// <summary>
        /// Gets or sets the <see cref="Ped"/> to run the <see cref="AnimationTask"/> on if any
        /// </summary>
        public Ped Speaker { get; set; }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        public string AmbientSound { get; set; }

        /// <summary>
        /// Gets or sets an animation <see cref="TaskSequence"/> to be played
        /// on the <see cref="Speaker"/> while the text displays on the screen.
        /// </summary>
        public AnimationSequence Animations { get; set; }

        /// <summary>
        /// Gets or sets whether to cancel the current animation the <see cref="Ped"/>
        /// is playing when this <see cref="Subtitle"/> has elapsed
        /// </summary>
        public bool StopAnimationsOnElapsed { get; set; } = true;

        /// <summary>
        /// Event fired as the message is being displayed on screen
        /// </summary>
        public override event SubtitleEventHandler OnDisplayed;

        /// <summary>
        /// Event fired after the message has displayed for the time specified on screen
        /// </summary>
        public override event SubtitleEventHandler Elapsed;

        /// <summary>
        /// Creates a new instance of <see cref="PedCommunication"/>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="duration"></param>
        public PedCommunication(string text, int duration) : base(text, duration)
        {
            Animations = new AnimationSequence();
        }

        /// <summary>
        /// Displays this sentance on screen and then waits
        /// for the time specified in <see cref="Subtitle.Time"/>.
        /// ONLY CALL THIS METHOD FROM A CHILD <see cref="GameFiber"/>
        /// </summary>
        public override void Display()
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

            // Play animations
            var taskSequence = Animations.Play(Speaker);

            /* Play ambient sound?
            if (!String.IsNullOrEmpty(AmbientSound))
            {
                Speaker.PlayAmbientSpeech()
            }
            */

            // Fire event
            OnDisplayed?.Invoke(this);

            // Now wait
            GameFiber.Wait(Duration);

            // Fire event
            Elapsed?.Invoke(this);

            // Cancel animation if its on a loop
            if (StopAnimationsOnElapsed && taskSequence != null)
            {
                Speaker.Tasks.ClearSecondary();
                taskSequence.Dispose();
            }
        }
    }
}
