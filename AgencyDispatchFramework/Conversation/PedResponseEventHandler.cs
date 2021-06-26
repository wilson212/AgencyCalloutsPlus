namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// Delegate to handle a <see cref="FlowSequenceEvent"/>
    /// </summary>
    /// <param name="sender">The <see cref="Dialogue"/> that triggered the <see cref="PedResponse"/></param>
    /// <param name="r">The <see cref="PedResponse"/> instance</param>
    /// <param name="d">The displayed <see cref="CommunicationSequence"/> to the player in game</param>
    public delegate void PedResponseEventHandler(Dialogue sender, Question q, PedResponse r, CommunicationSequence d);
}
