using AgencyDispatchFramework.Dispatching;
using RAGENativeUI;
using RAGENativeUI.Elements;
using RAGENativeUI.PauseMenu;
using System.Drawing;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// A submenu <see cref="TabItem"/> that represents displays current <see cref="PriorityCall"/>
    /// information if the player is currently on a callout
    /// </summary>
    internal class CurrentCallTabPage : TabItem
    {
        /// <summary>
        /// Gets the message to display when the player is not on a <see cref="AgencyCallout"/>
        /// </summary>
        public static readonly string NoAssingnmentMessage = "You have no active assignment";

        /// <summary>
        /// Sets the bounds for the "no assignment" message
        /// </summary>
        public int WordWrap { get; set; }

        /// <summary>
        /// Contains the active <see cref="PriorityCall"/> the player is on
        /// </summary>
        internal PriorityCall Call { get; private set; }

        /// <summary>
        /// Creates a new instance of this Tab Page
        /// </summary>
        /// <param name="name"></param>
        public CurrentCallTabPage(string name) : base(name)
        {
            Dispatch.OnPlayerCallAccepted += Dispatch_OnPlayerCallAccepted;
            Dispatch.OnPlayerCallCompleted += Dispatch_OnPlayerCallCompleted;
        }

        /// <summary>
        /// Method called when the player completes a call
        /// </summary>
        /// <param name="call"></param>
        private void Dispatch_OnPlayerCallCompleted(PriorityCall call)
        {
            Call = null;
        }

        /// <summary>
        /// Method called when the player accepts a callout
        /// </summary>
        /// <param name="call"></param>
        private void Dispatch_OnPlayerCallAccepted(PriorityCall call)
        {
            Call = call;
        }

        /// <summary>
        /// Draws this instance
        /// </summary>
        public override void Draw()
        {
            // Draw base
            base.Draw();

            // Alpha of the entire tab's contents
            var alpha = (Focused || !CanBeFocused) ? 255 : 200;
            var dimmensions = new Size(BottomRight.SubtractPoints(TopLeft));
            var center = dimmensions.Width / 2;

            if (Call == null)
            {
                var ww = WordWrap == 0 ? BottomRight.X - TopLeft.X - 40 : WordWrap;
                ResText.Draw(NoAssingnmentMessage, SafeSize.AddPoints(new Point(center, 150)), 0.6f, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletLondon, ResText.Alignment.Centered, true, true, new Size((int)ww, 0));
            }
        }
    }
}
