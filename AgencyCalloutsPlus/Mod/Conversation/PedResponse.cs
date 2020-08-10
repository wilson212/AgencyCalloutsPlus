namespace AgencyCalloutsPlus.Mod.Conversation
{
    /// <summary>
    /// A class that contains response data to a player's questions from a <see cref="FlowSequence"/>
    /// </summary>
    public sealed class PedResponse
    {
        /// <summary>
        /// Contains our possible responses
        /// </summary>
        private ProbabilityGenerator<LineSet> Responses { get; set; }

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
        private LineSet SelectedLineSet { get; set; }

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
            Responses = new ProbabilityGenerator<LineSet>();
            FromInputIds = fromInputIds;
            ReturnMenuId = returnMenuId;
        }

        /// <summary>
        /// Adds a lineset to the internal <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        /// <param name="set"></param>
        internal void AddLineSet(LineSet set)
        {
            Responses.Add(set);
        }

        /// <summary>
        /// Gets a random <see cref="LineSet"/> from this <see cref="PedResponse"/>
        /// </summary>
        /// <param name="cacheResponse">if true, the same response will be returned everytime this method is called</param>
        /// <returns></returns>
        public LineSet GetResponseLineSet(bool cacheResponse = true)
        {
            if (cacheResponse)
            {
                // Spawn a response if we have not selected one yet
                if (SelectedLineSet == null)
                {
                    SelectedLineSet = Responses.Spawn();
                }
            }
            else
            {
                SelectedLineSet = Responses.Spawn();
            }

            return SelectedLineSet;
        }
    }
}
