using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Dispatching.Assignments;
using RAGENativeUI;
using RAGENativeUI.Elements;
using RAGENativeUI.PauseMenu;
using System.Collections.Generic;
using System.Drawing;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// A submenu <see cref="TabItem"/> that represents displays current <see cref="PriorityCall"/>
    /// information if the player is currently on a callout, otherwise shows <see cref="BaseAssignment"/>
    /// information
    /// </summary>
    internal class AssignmentTabPage : TabSubmenuItem
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
        public AssignmentTabPage(string name) : base(name, new List<TabItem>())
        {
            // Register for events
            Dispatch.OnPlayerCallAccepted += Dispatch_OnPlayerCallAccepted;
            Dispatch.OnPlayerCallCompleted += Dispatch_OnPlayerCallCompleted;

            // Background tile for when the player is not on a PriorityCall
            RockstarTile = new Sprite("pause_menu_sp_content", "rockstartilebmp", new Point(), new Size(64, 64), 0f, Color.FromArgb(40, 255, 255, 255));

            // Need to have a dummy item here for "RefreshIndex" calls
            // This item will not actually show
            Items.Add(new TabItem("There is nothing here"));
        }

        /// <summary>
        /// Method called when the player completes a call
        /// </summary>
        /// <param name="call"></param>
        private void Dispatch_OnPlayerCallCompleted(PriorityCall call)
        {
            // Set internal
            Call = null;

            // Clear old data
            Items.Clear();

            // Need to have a dummy item here for "RefreshIndex" calls
            // This item will not actually show
            Items.Add(new TabItem("There is nothing here"));
        }

        /// <summary>
        /// Method called when the player accepts a callout
        /// </summary>
        /// <param name="call"></param>
        private void Dispatch_OnPlayerCallAccepted(PriorityCall call)
        {
            // Clear old
            Items.Clear();

            // Create items
            Items = new List<TabItem>
            {
                // Add call details
                new PriorityCallTabItem(call)
                {
                    Title = "Call Details"
                }

                // Add call updates (Call Activity)

                // Add attached units (Attached Units)
            };

            // Set internal
            Call = call;

            // Always refresh index when adding items
            RefreshIndex();
        }

        /// <summary>
        /// Draws this instance
        /// </summary>
        public override void Draw()
        {
            // If call is null, draw the assignment window instead
            if (Call == null)
            {
                var res = UIMenu.GetScreenResolutionMantainRatio();

                SafeSize = new Point(300, 240);

                TopLeft = new Point(SafeSize.X, SafeSize.Y);
                BottomRight = new Point((int)res.Width - SafeSize.X, (int)res.Height - SafeSize.Y);

                var rectSize = new Size(BottomRight.SubtractPoints(TopLeft));
                ResRectangle.Draw(TopLeft, rectSize, Color.FromArgb((Focused || !FadeInWhenFocused) ? 200 : 120, 0, 0, 0));

                var tileSize = 100;
                RockstarTile.Size = new Size(tileSize, tileSize);

                var cols = rectSize.Width / tileSize;
                var fils = 4;

                for (int i = 0; i < cols * fils; i++)
                {
                    RockstarTile.Position = TopLeft.AddPoints(new Point(tileSize * (i % cols), tileSize * (i / cols)));
                    RockstarTile.Color = Color.FromArgb((int)MiscExtensions.LinearFloatLerp(40, 0, i / cols, fils), 255, 255, 255);
                    RockstarTile.Draw();
                }

                // Alpha of the entire tab's contents
                var alpha = (Focused || !CanBeFocused) ? 255 : 200;
                var dimmensions = new Size(BottomRight.SubtractPoints(TopLeft));
                var center = dimmensions.Width / 2;
                var ww = WordWrap == 0 ? BottomRight.X - TopLeft.X - 40 : WordWrap;
                ResText.Draw(NoAssingnmentMessage, SafeSize.AddPoints(new Point(center, 150)), 0.6f, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletLondon, ResText.Alignment.Centered, true, true, new Size((int)ww, 0));
            }
            else
            {
                FadeInWhenFocused = false;

                // Draw base
                base.Draw();
            }
        }
    }
}
