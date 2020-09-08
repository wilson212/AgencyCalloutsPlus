namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// A class that contains response data to a player's questions from a <see cref="FlowSequence"/>
    /// </summary>
    public sealed class PedResponse
    {
        /// <summary>
        /// Contains our possible responses
        /// </summary>
        private ProbabilityGenerator<Statement> Statements { get; set; }

        /// <summary>
        /// Gets the button ID's that prompts this <see cref="PedResponse" />
        /// </summary>
        public string[] FromInputIds { get; private set; }

        /// <summary>
        /// Gets the name of the menu to display once this <see cref="PedResponse"/> is displayed
        /// </summary>
        public string ReturnMenuId { get; private set; }

        /// <summary>
        /// Contains the selected response to the question. This does not
        /// change once selected!
        /// </summary>
        private Statement SelectedStatement { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to hide
        /// once this <see cref="PedResponse"/> is displayed
        /// </summary>
        public string[] HidesMenuItems { get; set; }

        /// <summary>
        /// Contains an array of <see cref="RAGENativeUI.Elements.UIMenuItem"/> names to unhide
        /// once this <see cref="PedResponse"/> is displayed
        /// </summary>
        public string[] ShowMenuItems { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="PedResponse"/>
        /// </summary>
        /// <param name="fromInputId"></param>
        /// <param name="returnMenuId"></param>
        public PedResponse(string[] fromInputIds, string returnMenuId)
        {
            Statements = new ProbabilityGenerator<Statement>();
            FromInputIds = fromInputIds;
            ReturnMenuId = returnMenuId;
        }

        /// <summary>
        /// Adds a lineset to the internal <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        /// <param name="set"></param>
        internal void AddStatement(Statement set)
        {
            Statements.Add(set);
        }

        /// <summary>
        /// Gets a random <see cref="Statement"/> from this <see cref="PedResponse"/>
        /// </summary>
        /// <param name="cacheResponse">if true, the same response will be returned everytime this method is called</param>
        /// <returns></returns>
        public Statement GetStatement(bool cacheResponse = true)
        {
            if (cacheResponse)
            {
                // Spawn a response if we have not selected one yet
                if (SelectedStatement == null)
                {
                    SelectedStatement = Statements.Spawn();
                }
            }
            else
            {
                SelectedStatement = Statements.Spawn();
            }

            return SelectedStatement;
        }
    }
}
