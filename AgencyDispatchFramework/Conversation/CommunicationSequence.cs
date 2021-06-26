using System;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a series of words spoken to display in a <see cref="Game.Subtitle"/>
    /// </summary>
    public class CommunicationSequence : ISpawnable
    {
        #region Event Fields
        internal string CallOnFirstShown = String.Empty;
        internal string CallOnLastShown = String.Empty;
        internal string CallOnElapsed = String.Empty;
        #endregion Event Fields

        /// <summary>
        /// Gets or sets the probability of this <see cref="CommunicationSequence"/> being selected against
        /// other <see cref="CommunicationSequence"/>s in the <see cref="ResponseSet"/>
        /// </summary>
        public int Probability { get; set; }

        /// <summary>
        /// Gets or sets the lines to display in the Subtitles
        /// </summary>
        public PedCommunication[] Communications { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to hide
        /// if this <see cref="CommunicationSequence"/> is displayed
        /// </summary>
        public string[] HidesQuestionIds { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to unhide
        /// if this <see cref="CommunicationSequence"/> is displayed
        /// </summary>
        public string[] ShowsQuestionIds { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="CommunicationSequence"/> with the specified probability
        /// </summary>
        /// <param name="probability"></param>
        public CommunicationSequence(int probability)
        {
            Probability = probability;
        }
    }
}
