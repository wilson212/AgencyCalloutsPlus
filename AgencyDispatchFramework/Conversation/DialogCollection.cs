namespace AgencyDispatchFramework.Conversation
{
    /// <summary>
    /// A class that represents a single collection of <see cref="Dialog"/> instances
    /// </summary>
    public abstract class DialogCollection
    {
        /// <summary>
        /// Gets the question id of this <see cref="DialogCollection"/>
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Contains our possible responses
        /// </summary>
        protected ProbabilityGenerator<Dialog> Dialogs { get; set; }

        /// <summary>
        /// Contains the selected response to the question. This does not
        /// change once selected!
        /// </summary>
        protected Dialog SelectedDialog { get; set; }

        /// <summary>
        /// Gets the number of <see cref="Dialog"/> instances in this container
        /// </summary>
        public int DialogCount => Dialogs.ItemCount;

        /// <summary>
        /// Creates a new instance of <see cref="DialogCollection"/>
        /// </summary>
        public DialogCollection(string id)
        {
            Dialogs = new ProbabilityGenerator<Dialog>();
            Id = id;
        }

        /// <summary>
        /// Adds a lineset to the internal <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        /// <param name="discourse"></param>
        public virtual void AddDialog(Dialog discourse)
        {
            Dialogs.Add(discourse);
        }

        /// <summary>
        /// Gets a random <see cref="Dialog"/> from this <see cref="DialogCollection"/>, and caches
        /// the selected <see cref="Dialog"/>. Everytime this method is called, the same <see cref="Dialog"/>
        /// will be returned.
        /// </summary>
        /// <returns></returns>
        public virtual Dialog GetPersistantDialog()
        {
            // Spawn a response if we have not selected one yet
            if (SelectedDialog == null)
            {
                SelectedDialog = Dialogs.Spawn();
            }

            return SelectedDialog;
        }

        /// <summary>
        /// Gets a random <see cref="Dialog"/> from this <see cref="DialogCollection"/>
        /// </summary>
        /// <returns></returns>
        public virtual Dialog GetRandomDialog()
        {
            return Dialogs.Spawn();
        }
    }
}
