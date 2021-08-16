using Rage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// A class used to play a sequence of animations on a <see cref="Ped"/>
    /// </summary>
    public class AnimationSequence : IEnumerable<AnimationData>
    {
        /// <summary>
        /// Gets or sets an animation <see cref="TaskSequence"/> to be played
        /// on the <see cref="Speaker"/> while the text displays on the screen.
        /// </summary>
        protected List<AnimationData> Animations { get; set; }

        /// <summary>
        /// Gets or sets whether to loop the animation <see cref="TaskSequence"/>
        /// </summary>
        public bool LoopAnimations { get; set; }

        /// <summary>
        /// Gets the number of animations in the sequence
        /// </summary>
        public int Count => Animations.Count;

        /// <summary>
        /// Indicates whether this <see cref="AnimationSequence"/> is currently playing
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Propagates a notification that the <see cref="AnimationSequence"/> should be canceled.
        /// </summary>
        private CancellationTokenSource TokenSource { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public AnimationSequence()
        {
            Animations = new List<AnimationData>();
        }

        /// <summary>
        /// Adds a new <see cref="AnimationData"/> to the sequence
        /// </summary>
        /// <param name="animation"></param>
        /// <exception cref="ArgumentException">thrown if the <see cref="AnimationData"/> is invalid</exception>
        public void Add(AnimationData animation)
        {
            // Validate
            if (!animation.AppearsValid)
            {
                throw new ArgumentException("AnimationData has a null or empty property.");
            }

            // Add
            Animations.Add(animation);
        }

        /// <summary>
        /// Adds a new animation to play in the <see cref="AnimationSequence"/>
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="name"></param>
        /// <exception cref="ArgumentException">thrown if the <see cref="AnimationData"/> is invalid</exception>
        public void Add(AnimationDictionary dictionary, string name)
        {
            // Create
            var animation = new AnimationData(dictionary, name);

            // Validate
            if (!animation.AppearsValid)
            {
                throw new ArgumentException("AnimationData has a null or empty property.");
            }

            // Add
            Animations.Add(animation);
        }

        /// <summary>
        /// Plays the sequence of animations on the <see cref="Ped"/> in the game
        /// </summary>
        /// <param name="ped">The <see cref="Ped"/> to play the animation on</param>
        /// <returns></returns>
        public GameFiber Play(Ped ped)
        {
            if (Animations.Count > 0 && (ped?.Exists() ?? false) && ped.IsAlive)
            {
                // Create a token cancellation source
                TokenSource = new CancellationTokenSource();

                // Invoke animaitons
                return GameFiber.StartNew(() => DoPlayAnimationSequence(ped, false));
            }

            return null;
        }

        /// <summary>
        /// Cancels this <see cref="AnimationSequence"/> if playing. The current
        /// playing animation will still be finished, so you will need to additionally
        /// call <see cref="Ped.Tasks.ClearSecondary()"/> method to stop playing the
        /// current animation immediately.
        /// </summary>
        public void Cancel()
        {
            if (IsPlaying)
                TokenSource.Cancel();
        }

        /// <summary>
        /// To be used in a brand new GameFiber
        /// </summary>
        /// <param name="ped"></param>
        private void DoPlayAnimationSequence(Ped ped, bool transition)
        {
            try
            {
                // Play animation
                Log.Debug($"Executing {Animations.Count} animation tasks on ped during a conversation");

                // Call back for looping
                Play:
                {
                    // Flag
                    IsPlaying = true;

                    // Loop animations. Use .ToArray() to create a new Enumerator,
                    // so that items added while playing don't cause an exception
                    foreach (var anim in Animations.ToArray())
                    {
                        // Clear task if using a transition
                        ped.Tasks.ClearSecondary();
                        GameFiber.Yield();

                        // Did the user cancel?
                        if (TokenSource.Token.IsCancellationRequested)
                        {
                            IsPlaying = false;
                            return;
                        }

                        // Play task on ped
                        var task = ped.Tasks.PlayAnimation(anim.Dictionary, anim.Name, 1f, AnimationFlags.SecondaryTask);

                        // Wait for the task to start playing, and timeout after 2 seconds
                        GameFiber.WaitUntil(() => task.IsPlaying, 2000);
                        if (!task.IsPlaying)
                        {
                            // If we still arent playing, log it
                            Log.Error($"Animation (dictionary='{anim.Dictionary}' name='{anim.Name}') timed-out!");
                            continue;
                        }

                        // Wait for the animation to stop playing
                        var animationLengthMS = (int)Math.Round(task.Length * 1000, 0);
                        if (transition)
                        {
                            GameFiber.WaitUntil(() => task.CurrentTimeRatio > 0.90f, animationLengthMS);
                        }
                        else
                        {
                            GameFiber.WaitWhile(() => task.IsPlaying, animationLengthMS);
                        }
                    }
                }

                // Looping?
                if (LoopAnimations)
                    goto Play;

                // Dispose
                TokenSource.Dispose();
                TokenSource = null;

                // Flag
                IsPlaying = false;
            }
            catch (Exception e)
            {
                // Log it
                Log.Exception(e);

                // Dispose
                TokenSource.Dispose();
                TokenSource = null;

                // Flag
                IsPlaying = false;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the list of <see cref="AnimationData"/>
        /// </summary>
        public IEnumerator<AnimationData> GetEnumerator()
        {
            return Animations.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the list of <see cref="AnimationData"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Animations.GetEnumerator();
        }
    }
}
