namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// A class that contains response data to a player's questions from a <see cref="FlowSequence"/>
    /// </summary>
    public sealed class PedResponse : DialogCollection
    {
        /// <summary>
        /// Gets the name of the menu to display once this <see cref="PedResponse"/> is displayed
        /// </summary>
        public string ReturnMenuId { get; private set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to hide
        /// once this <see cref="PedResponse"/> is displayed
        /// </summary>
        public string[] HideQuestionIds { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to unhide
        /// once this <see cref="PedResponse"/> is displayed
        /// </summary>
        public string[] ShowQuestionIds { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="PedResponse"/>
        /// </summary>
        /// <param name="fromInputId"></param>
        /// <param name="returnMenuId"></param>
        public PedResponse(string questionId, string returnMenuId) : base(questionId)
        {
            ReturnMenuId = returnMenuId;
        }
    }
}
