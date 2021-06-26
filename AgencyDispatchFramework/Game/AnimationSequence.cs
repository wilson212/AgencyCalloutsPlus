using Rage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Game
{
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
        /// Creates a new instance
        /// </summary>
        public AnimationSequence()
        {
            Animations = new List<AnimationData>();
        }

        public void Add(AnimationData animation)
        {
            Animations.Add(animation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public TaskSequence Play(Ped ped)
        {
            // Invoke animaiton?
            TaskSequence taskSequence = null;
            if (Animations.Count > 0 && (ped?.Exists() ?? false) && ped.IsAlive)
            {
                try
                {
                    // Try and create a new sequence
                    taskSequence = new TaskSequence(ped);
                    foreach (var anim in Animations)
                    {
                        taskSequence.Tasks.PlayAnimation(anim.Dictionary, anim.Name, 1f, AnimationFlags.SecondaryTask);
                    }

                    // Play animation
                    Log.Debug($"Executing {Animations.Count} animation tasks on ped during conversation");
                    taskSequence.Execute(LoopAnimations);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            return taskSequence;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<AnimationData> GetEnumerator()
        {
            return Animations.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Animations.GetEnumerator();
        }
    }
}
