using AgencyDispatchFramework.Game;
using Rage;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Conversation
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
        /// Gets or sets the <see cref="Ped"/> to run the <see cref="AnimationTask"/> on if any
        /// </summary>
        public Ped Speaker { get; set; }

        /// <summary>
        /// Gets or sets an animation <see cref="TaskSequence"/> to be played
        /// on the <see cref="Speaker"/> while the text displays on the screen.
        /// </summary>
        public List<PedAnimation> AnimationSequence { get; set; }

        /// <summary>
        /// Gets or sets whether to loop the animation <see cref="TaskSequence"/>
        /// </summary>
        public bool LoopAnimation { get; set; }

        /// <summary>
        /// Gets or sets whether to cancel the current animation the <see cref="Ped"/>
        /// is playing when this <see cref="Subtitle"/> has elapsed
        /// </summary>
        public bool TerminateAnimation { get; set; } = true;

        /// <summary>
        /// Event fired as the message is being displayed on screen
        /// </summary>
        public event SubtitleEventHandler OnDisplayed;

        /// <summary>
        /// Event fired after the message has displayed for the time specified on screen
        /// </summary>
        public event SubtitleEventHandler Elapsed;

        /// <summary>
        /// Determines whether the <see cref="Ped"/> is valid to play an animation on
        /// </summary>
        private bool SpeakerAppearsValid
        {
            get => (Speaker?.Exists() ?? false && Speaker.IsAlive);
        }

        /// <summary>
        /// Creates a new instance of <see cref="Subtitle"/>
        /// </summary>
        public Subtitle(string text, int duration)
        {
            Text = text;
            Duration = duration;
            AnimationSequence = new List<PedAnimation>();
        }

        /// <summary>
        /// Displays this sentance on screen and then waits
        /// for the time specified in <see cref="Subtitle.Time"/>.
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
            TaskSequence taskSequence = null;
            if (AnimationSequence.Count > 0 && SpeakerAppearsValid)
            {
                try
                {
                    // Try and create a new sequence
                    taskSequence = new TaskSequence(Speaker);
                    foreach (var anim in AnimationSequence)
                    {
                        taskSequence.Tasks.PlayAnimation(anim.Dictionary, anim.Name, 1f, AnimationFlags.SecondaryTask);
                    }

                    // Play animation
                    Log.Debug($"Executing {AnimationSequence.Count} animation tasks on ped during conversation");
                    taskSequence.Execute(LoopAnimation);
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

            // Cancel animation if its on a loop
            if (TerminateAnimation && taskSequence != null)
            {
                Speaker.Tasks.ClearSecondary();
                taskSequence.Dispose();
            }
        }
    }
}
