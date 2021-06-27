namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// A class that represents a collection of similar <see cref="CommunicationSequence"/> 
    /// instances that can be used as a response or inquery by a <see cref="Rage.Ped"/>
    /// </summary>
    public abstract class SequenceCollection
    {
        /// <summary>
        /// Gets the id of this <see cref="SequenceCollection"/>
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Contains our possible responses that we will randomly select from
        /// </summary>
        protected ProbabilityGenerator<CommunicationSequence> SequencePool { get; set; }

        /// <summary>
        /// Contains the selected response to the question. This does not
        /// change once selected!
        /// </summary>
        protected CommunicationSequence SelectedSequence { get; set; }

        /// <summary>
        /// Gets the number of <see cref="CommunicationSequence"/> instances in this container
        /// </summary>
        public int Count => SequencePool.ItemCount;

        /// <summary>
        /// Creates a new instance of <see cref="SequenceCollection"/>
        /// </summary>
        public SequenceCollection(string id)
        {
            SequencePool = new ProbabilityGenerator<CommunicationSequence>();
            Id = id;
        }

        /// <summary>
        /// Adds a lineset to the internal <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        /// <param name="sequence"></param>
        public virtual void AddSequence(CommunicationSequence sequence)
        {
            SequencePool.Add(sequence);
        }

        /// <summary>
        /// Gets a random <see cref="CommunicationSequence"/> from this <see cref="SequenceCollection"/>, and caches
        /// the selected <see cref="CommunicationSequence"/>. Everytime this method is called, the same <see cref="CommunicationSequence"/>
        /// will be returned.
        /// </summary>
        /// <returns></returns>
        public virtual CommunicationSequence GetPersistantSequence()
        {
            // Spawn a response if we have not selected one yet
            if (SelectedSequence == null)
            {
                SelectedSequence = SequencePool.Spawn();
            }

            return SelectedSequence;
        }

        /// <summary>
        /// Gets a random <see cref="CommunicationSequence"/> from this <see cref="SequenceCollection"/>
        /// </summary>
        /// <returns></returns>
        public virtual CommunicationSequence GetRandomSequence()
        {
            return SequencePool.Spawn();
        }
    }
}
