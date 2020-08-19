using RAGENativeUI.Elements;

namespace AgencyCalloutsPlus.Mod.NativeUI
{
    public class MyUIMenuItem<T> : UIMenuItem
    {

        public T Tag { get; set; }

        /// <summary>
        /// Basic menu button.
        /// </summary>
        /// <param name="text">Button label.</param>
        public MyUIMenuItem(string text) : this(text, "")
        {
        }

        /// <summary>
        /// Basic menu button.
        /// </summary>
        /// <param name="text">Button label.</param>
        /// <param name="description">Description.</param>
        public MyUIMenuItem(string text, string description) : base(text, description)
        {
            Enabled = true;

            Text = text;
            Description = description;
        }
    }
}
