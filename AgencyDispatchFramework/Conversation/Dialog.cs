using System;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a series of words spoken to display in a <see cref="Subtitle"/>
    /// </summary>
    public class Dialog : ISpawnable
    {
        #region Event Fields
        internal string CallOnFirstShown = String.Empty;
        internal string CallOnLastShown = String.Empty;
        internal string CallOnElapsed = String.Empty;
        #endregion Event Fields

        /// <summary>
        /// Gets or sets the probability of this <see cref="Dialog"/> being selected against
        /// other <see cref="Dialog"/>s in the <see cref="ResponseSet"/>
        /// </summary>
        public int Probability { get; set; }

        /// <summary>
        /// Gets or sets the lines to display in the Subtitles
        /// </summary>
        public Subtitle[] Subtitles { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to hide
        /// if this <see cref="Dialog"/> is displayed
        /// </summary>
        public string[] HidesQuestionIds { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to unhide
        /// if this <see cref="Dialog"/> is displayed
        /// </summary>
        public string[] ShowsQuestionIds { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="Dialog"/> with the specified probability
        /// </summary>
        /// <param name="probability"></param>
        public Dialog(int probability)
        {
            Probability = probability;
        }
    }
}
