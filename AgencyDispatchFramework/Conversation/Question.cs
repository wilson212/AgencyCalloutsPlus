namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a question the Player will ask a <see cref="Game.GamePed"/>
    /// during a <see cref="Dialogue"/>
    /// </summary>
    public class Question : SequenceCollection
    {
        /// <summary>
        /// Creates a new instance of <see cref="Question"/>
        /// </summary>
        public Question(string id) : base(id)
        {
            
        }
    }
}
