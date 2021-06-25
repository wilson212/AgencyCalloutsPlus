using RAGENativeUI.Elements;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// A simple item with a label containing a generic tagged item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UIMenuItem<T> : UIMenuItem
    {
        /// <summary>
        /// Gets or sets the tagged object
        /// </summary>
        public T Tag { get; set; }

        /// <summary>
        /// Basic menu button.
        /// </summary>
        /// <param name="text">Button label.</param>
        public UIMenuItem(string text) : this(text, "")
        {
        }

        /// <summary>
        /// Basic menu button.
        /// </summary>
        /// <param name="text">Button label.</param>
        /// <param name="description">Description.</param>
        public UIMenuItem(string text, string description) : base(text, description)
        {
            Enabled = true;

            Text = text;
            Description = description;
        }
    }
}
