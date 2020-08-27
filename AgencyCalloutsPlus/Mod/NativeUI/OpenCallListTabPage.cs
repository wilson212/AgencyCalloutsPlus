using AgencyCalloutsPlus.API;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using RAGENativeUI.PauseMenu;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AgencyCalloutsPlus.Mod.NativeUI
{
    /// <summary>
    /// Represents the "Open Calls" tab in the CAD pause menu
    /// </summary>
    internal class OpenCallListTabPage : TabItem // TabSubmenuItem
    {
        /// <summary>
        /// Defines the maximum calls to display in the list at once before needing to scroll
        /// to see further items
        /// </summary>
        const int MaxItemsToDisplay = 16;

        /// <summary>
        /// Contains a list of sub menu Tab items
        /// </summary>
        private List<PriorityCallTabItem> Items { get; set; }

        /// <summary>
        /// Our current selected index
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets a range of indexes in view
        /// </summary>
        public Range<int> IndexesInView { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="OpenCallListTabPage"/>
        /// </summary>
        /// <param name="name"></param>
        public OpenCallListTabPage(string name) : base(name)
        {
            Items = new List<PriorityCallTabItem>();
            IndexesInView = new Range<int>(0, MaxItemsToDisplay - 1);

            // Do not draw background
            DrawBg = false;

            // Allow focusing of this tab
            CanBeFocused = true;

            // Register for dispatching events
            Dispatch.OnCallAdded += Dispatch_OnCallAdded;
            Dispatch.OnCallCompleted += Dispatch_OnCallCompleted;
        }

        public void RefreshIndex()
        {
            foreach (TabItem item in Items)
            {
                item.Focused = false;
                item.Active = false;
                item.Visible = false;
            }

            Index = (Items.Count == 0) ? 0 : ((1000 - (1000 % Items.Count)) % Items.Count);
        }

        private void MoveListItemsUp()
        {
            // Set new index
            Index = (1000 - (1000 % Items.Count) + Index - 1) % Items.Count;

            // If we can't fill the entire list, then we are fine
            if (Items.Count < MaxItemsToDisplay) return;

            // If we are out of range, increase the range
            if (Index < IndexesInView.Minimum)
            {
                IndexesInView.Maximum--;
                IndexesInView.Minimum--;
            }

            // If we are not yet at maximum
            if (Index != Items.Count - 1)
                return;

            IndexesInView.Minimum = Items.Count - MaxItemsToDisplay;
            IndexesInView.Maximum = Items.Count - 1;
        }

        private void MoveListItemsDown()
        {
            // Set new index
            Index = (1000 - (1000 % Items.Count) + Index + 1) % Items.Count;

            // If we can't fill the entire list, then we are fine
            if (Items.Count < MaxItemsToDisplay) return;

            // If we are out of range, increase the range
            if (Index > IndexesInView.Maximum)
            {
                IndexesInView.Minimum = Index - (MaxItemsToDisplay - 1);
                IndexesInView.Maximum = Index;
            }

            if (Index == 0)
            {
                IndexesInView.Minimum = 0;
                IndexesInView.Maximum = (MaxItemsToDisplay - 1);
            }
        }

        /// <summary>
        /// Processes the control input in this instance
        /// </summary>
        public override void ProcessControls()
        {
            if (JustOpened)
            {
                JustOpened = false;
                return;
            }

            // If we aren't focused
            if (!Focused) return;

            // Reset index if out of range
            if (Items.Count > 0 && !Index.InRange(0, Items.Count - 1))
                Index = 0;

            // Do we have an actual sub menu item focused within index range?
            if (Items.Count > 0 && Items[Index].Focused)
            {
                // If select is clicked while the call is focused, now we invoke the callout for the player
                if (Common.IsDisabledControlJustPressed(0, GameControl.CellphoneCancel))
                {
                    Common.PlaySound("CANCEL", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                    if (Items[Index].CanBeFocused && Items[Index].Focused)
                    {
                        Parent.FocusLevel--;
                        Items[Index].Focused = false;
                    }
                }

                Items[Index].ProcessControls();
            }
            else
            {
                // Focus level equals one, so the menu item is not yet
                if (Common.IsDisabledControlJustPressed(0, GameControl.CellphoneSelect) && Focused && Parent.FocusLevel == 1)
                {
                    Common.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");

                    if (Items[Index].CanBeFocused && !Items[Index].Focused)
                    {
                        Parent.FocusLevel++;
                        Items[Index].JustOpened = true;
                        Items[Index].Focused = true;
                    }
                    else
                    {
                        Items[Index].OnActivated();
                    }
                }

                // Only process up and down if we have items to scroll through
                if (Items.Count > 0)
                {
                    // Move up button pressed
                    if (Common.IsDisabledControlJustPressed(0, GameControl.FrontendUp) || Common.IsDisabledControlJustPressed(0, GameControl.MoveUpOnly) || Common.IsDisabledControlJustPressed(0, GameControl.CursorScrollUp) && Parent.FocusLevel == 1)
                    {
                        Common.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        MoveListItemsUp();
                    }

                    // Move down button pressed
                    else if (Common.IsDisabledControlJustPressed(0, GameControl.FrontendDown) || Common.IsDisabledControlJustPressed(0, GameControl.MoveDownOnly) || Common.IsDisabledControlJustPressed(0, GameControl.CursorScrollDown) && Parent.FocusLevel == 1)
                    {
                        Common.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        MoveListItemsDown();
                    }
                }
            }
        }

        /// <summary>
        /// Draws this instance.
        /// </summary>
        public override void Draw()
        {
            if (!Visible) return;
            base.Draw();

            var res = UIMenu.GetScreenResolutionMantainRatio();

            var blackAlpha = Focused ? 200 : 100;
            var fullAlpha = Focused ? 255 : 150;

            var activeWidth = res.Width - SafeSize.X * 2;
            var submenuWidth = (int)(activeWidth * 0.6818f);
            var statusSize = new Size(10, 40);
            var itemSize = new Size(((int)activeWidth - (submenuWidth + 3)) - statusSize.Width, 40);

            // Draw each call in the list within the index range
            for (int i = IndexesInView.Minimum; i < IndexesInView.Maximum; i++)
            {
                // If we are at our item count, exit
                if (Items.Count <= i) break;

                // Draw call status color box
                ResRectangle.Draw(SafeSize.AddPoints(new Point(0, (itemSize.Height + 3) * i)), statusSize, GetCallItemColor(Items[i]));
                
                // Draw item outline box
                ResRectangle.Draw(SafeSize.AddPoints(new Point(10, (itemSize.Height + 3) * i)), itemSize, (Index == i && Focused) ? Color.FromArgb(fullAlpha, Color.White) : Color.FromArgb(blackAlpha, Color.Black));

                // Draw item title in the box
                ResText.Draw(Items[i].Title, SafeSize.AddPoints(new Point(16, 5 + (itemSize.Height + 3) * i)), 0.35f, Color.FromArgb(fullAlpha, (Index == i && Focused) ? Color.Black : Color.White), Common.EFont.ChaletLondon, false);
            }

            // Draw only if in range
            if (Items.Count > 0 && Index.InRange(0, Items.Count - 1))
            {
                var focusedItem = Items[Index];
                focusedItem.Visible = true;
                //focusedItem.FadeInWhenFocused = true;
                focusedItem.UseDynamicPositionment = false;
                focusedItem.SafeSize = SafeSize.AddPoints(new Point((int)activeWidth - submenuWidth, 0));
                focusedItem.TopLeft = SafeSize.AddPoints(new Point((int)activeWidth - submenuWidth, 0));
                focusedItem.BottomRight = new Point((int)res.Width - SafeSize.X, (int)res.Height - SafeSize.Y);
                focusedItem.Dimensions = new Size(focusedItem.BottomRight.SubtractPoints(focusedItem.TopLeft));
                focusedItem.Draw();
            }
        }

        private Color GetCallItemColor(PriorityCallTabItem priorityCallTabItem)
        {
            PriorityCall call = priorityCallTabItem.Call;
            switch (call.CallStatus)
            {
                default:
                case CallStatus.Created: return Color.DodgerBlue;
                case CallStatus.Dispatched: return Color.Purple;
                case CallStatus.OnScene: return Color.LimeGreen;
                case CallStatus.Waiting: return Color.Orange;
            }
        }

        #region Event Callback Methods

        private void Dispatch_OnCallCompleted(PriorityCall call)
        {
            int i = Items.Select((callItem, index) => new { callItem, index }).First(x => x.callItem.Call == call).index;
            Items.RemoveAt(i);

            // Always refresh
            RefreshIndex();
        }

        private void Dispatch_OnCallAdded(PriorityCall call)
        {
            var tItem = new PriorityCallTabItem(call);
            tItem.Activated += CallListItem_Activated;
            Items.Add(tItem);

            // Apply ordering
            Items = Items.OrderBy(x => x.Call.Priority).ThenBy(x => x.Call.CallCreated).ToList();

            // Always refresh
            RefreshIndex();
        }

        private void CallListItem_Activated(object sender, EventArgs e)
        {
            var call = Items[Index].Call;
            if (Dispatch.InvokeCalloutForPlayer(call))
            {
                Game.DisplaySubtitle("~b~You will be dispatched to this call once you exit the menu", 5000);
            }
            else
            {
                Game.DisplaySubtitle("~o~You are already on an active callout", 5000);
            }
        }

        #endregion
    }
}
