using System;

namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a sequence of verbal and/or non-verbal forms of communication to be played in a particular 
    /// order by a <see cref="Rage.Ped"/> when in a <see cref="Dialogue"/> with a another <see cref="Rage.Ped"/>
    /// </summary>
    /// <remarks>
    /// Used in a <see cref="ProbabilityGenerator{T}"/> by a <see cref="SequenceCollection"/>
    /// </remarks>
    public class CommunicationSequence : ISpawnable
    {
        #region Event Fields
        internal string CallOnFirstShown = String.Empty;
        internal string CallOnLastShown = String.Empty;
        internal string CallOnElapsed = String.Empty;
        #endregion Event Fields

        /// <summary>
        /// Gets or sets the probability of this <see cref="CommunicationSequence"/> being selected against
        /// other <see cref="CommunicationSequence"/>s in a <see cref="SequenceCollection"/>
        /// </summary>
        public int Probability { get; set; }

        /// <summary>
        /// Gets or sets the lines to display in the Subtitles
        /// </summary>
        public CommunicationElement[] Elements { get; set; }

        /// <summary>
        /// Contains an array of <see cref="Question"/> Ids to hide
        /// if this <see cref="CommunicationSequence"/> is displayed
        /// </summary>
        public string[] HidesQuestionIds { get; set; }

        /// <summary>
        /// Contains an array of <see cref="Question"/> Ids to unhide
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
