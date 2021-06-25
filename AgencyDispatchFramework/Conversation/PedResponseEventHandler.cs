namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Delegate to handle a <see cref="FlowSequenceEvent"/>
    /// </summary>
    /// <param name="sender">The <see cref="FlowSequence"/> that triggered the <see cref="PedResponse"/></param>
    /// <param name="r">The <see cref="PedResponse"/> instance</param>
    /// <param name="d">The displayed <see cref="Dialog"/> to the player in game</param>
    public delegate void PedResponseEventHandler(FlowSequence sender, Question q, PedResponse r, Dialog d);
}
