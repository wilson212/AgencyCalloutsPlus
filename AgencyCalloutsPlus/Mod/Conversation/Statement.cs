using Rage;

namespace AgencyCalloutsPlus.Mod.Conversation
{
    /// <summary>
    /// Represents a series of lines to display in a Subtitle
    /// </summary>
    public class Statement : ISpawnable
    {
        /// <summary>
        /// 
        /// </summary>
        public int Probability { get; set; }

        /// <summary>
        /// Gets or sets the lines to display in the Subtitles
        /// </summary>
        public Subtitle[] Subtitles { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to hide
        /// if this <see cref="Statement"/> is displayed
        /// </summary>
        public string[] HidesMenuItems { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to unhide
        /// if this <see cref="Statement"/> is displayed
        /// </summary>
        public string[] ShowMenuItems { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="Statement"/> with the specified probability
        /// </summary>
        /// <param name="probability"></param>
        public Statement(int probability)
        {
            Probability = probability;
        }
    }
}
