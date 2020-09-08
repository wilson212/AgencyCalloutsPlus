namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Represents a conversation flow outcome for a scenario
    /// </summary>
    public class FlowOutcome : ISpawnable
    {
        /// <summary>
        /// Gets the probability of spawning this <see cref="FlowOutcome"/>
        /// </summary>
        public int Probability { get; set; }

        /// <summary>
        /// Gets or sets the name of this <see cref="FlowOutcome"/>
        /// </summary>
        public string Id { get; set; }
    }
}
