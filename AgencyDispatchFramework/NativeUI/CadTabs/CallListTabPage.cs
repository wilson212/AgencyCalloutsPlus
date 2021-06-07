﻿using AgencyDispatchFramework.Dispatching;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using RAGENativeUI.PauseMenu;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AgencyDispatchFramework.NativeUI
{
    /// <summary>
    /// Represents the "Open Calls" tab in the CAD pause menu
    /// </summary>
    internal class CallListTabPage : TabItem // TabSubmenuItem
    {
        /// <summary>
        /// Our lock object to prevent multi-threading issues
        /// </summary>
        private static object _threadLock = new object();

        /// <summary>
        /// Gets the message to display when the player is not on a <see cref="AgencyCallout"/>
        /// </summary>
        public static readonly string NoAssingnmentMessage = "There are currently no open calls";

        /// <summary>
        /// Sets the bounds for the "no assignment" message
        /// </summary>
        public int WordWrap { get; set; }

        /// <summary>
        /// Defines the maximum calls to display in the list at once before needing to scroll
        /// to see further items
        /// </summary>
        const int MaxItemsToDisplay = 14;

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
        /// Creates a new instance of <see cref="CallListTabPage"/>
        /// </summary>
        /// <param name="name"></param>
        public CallListTabPage(string name) : base(name)
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
            Dispatch.OnCallExpired += Dispatch_OnCallCompleted;
        }

        /// <summary>
        /// Resets the screen and index
        /// </summary>
        public void RefreshIndex()
        {
            foreach (TabItem item in Items)
            {
                item.Focused = false;
                item.Active = false;
                item.Visible = false;
            }

            Index = 0;
        }

        /// <summary>
        /// Moves the list of calls UP one space if
        /// the player is the the BOTTOM of the list
        /// </summary>
        private void MoveListItemsUp()
        {
            // Keep index in range
            Index = CorrectIndex(Index - 1);

            // If we can't fill the entire list, then we are fine
            if (Items.Count <= MaxItemsToDisplay) return;

            // If we are out of range, increase the range
            if (Index < IndexesInView.Minimum)
            {
                IndexesInView.Minimum = Index;
                IndexesInView.Maximum = Index + (MaxItemsToDisplay - 1);
            }
        }

        /// <summary>
        /// Moves the list of calls DOWN one space if
        /// the player is the the TOP of the list
        /// </summary>
        private void MoveListItemsDown()
        {
            // Keep index in range
            Index = CorrectIndex(Index + 1);

            // If we can't fill the entire list, then we are fine
            if (Items.Count <= MaxItemsToDisplay) return;

            // If we are out of range, increase the range
            if (Index > IndexesInView.Maximum)
            {
                IndexesInView.Minimum = Index - (MaxItemsToDisplay - 1);
                IndexesInView.Maximum = Index;
            }
        }

        /// <summary>
        /// Keeps the index in range of the collection
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int CorrectIndex(int index)
        {
            if (index < 0) return 0;
            return (index >= Items.Count) ? Items.Count - 1 : index;
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
                Index = CorrectIndex(Index);

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

            if (Items.Count > 0)
            {
                DrawBg = false;

                var blackAlpha = Focused ? 200 : 100;
                var fullAlpha = Focused ? 255 : 150;
                var redAlpha = Focused ? 100 : 50;
                var activeWidth = res.Width - SafeSize.X * 2;
                var submenuWidth = (int)(activeWidth * 0.6818f);
                var statusSize = new Size(10, 40);
                var itemSize = new Size(((int)activeWidth - (submenuWidth + 3)) - statusSize.Width, 40);

                // Draw each call in the list within the index range
                int j = 0;
                for (int i = IndexesInView.Minimum; i <= IndexesInView.Maximum; i++)
                {
                    // If we are at our item count, exit
                    if (Items.Count <= i) break;

                    // Draw call status color box
                    ResRectangle.Draw(SafeSize.AddPoints(new Point(0, (itemSize.Height + 3) * j)), statusSize, GetCallItemColor(Items[i]));

                    // Draw item outline box
                    if (Dispatch.CanAssignAgencyToCall(Dispatch.PlayerAgency, Items[i].Call))
                    {
                        ResRectangle.Draw(SafeSize.AddPoints(new Point(10, (itemSize.Height + 3) * j)), itemSize, (Index == i && Focused) ? Color.FromArgb(fullAlpha, Color.White) : Color.FromArgb(blackAlpha, Color.Black));
                    }
                    else
                    {
                        ResRectangle.Draw(SafeSize.AddPoints(new Point(10, (itemSize.Height + 3) * j)), itemSize, (Index == i && Focused) ? Color.FromArgb(fullAlpha, Color.White) : Color.FromArgb(redAlpha, Color.Crimson));
                    }

                    // Draw item title in the box
                    ResText.Draw(Items[i].Title, SafeSize.AddPoints(new Point(16, 5 + (itemSize.Height + 3) * j)), 0.35f, Color.FromArgb(fullAlpha, (Index == i && Focused) ? Color.Black : Color.White), Common.EFont.ChaletLondon, false);

                    // Increase draw index 
                    j++;
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
                    focusedItem.Draw();
                }
            }
            else
            {
                DrawBg = true;

                // Alpha of the entire tab's contents
                var alpha = (Focused || !CanBeFocused) ? 255 : 200;
                var dimmensions = new Size(BottomRight.SubtractPoints(TopLeft));
                var center = dimmensions.Width / 2;

                var ww = WordWrap == 0 ? BottomRight.X - TopLeft.X - 40 : WordWrap;
                ResText.Draw(NoAssingnmentMessage, SafeSize.AddPoints(new Point(center, 150)), 0.6f, Color.FromArgb(alpha, Color.White), Common.EFont.ChaletLondon, ResText.Alignment.Centered, true, true, new Size((int)ww, 0));
            }
        }

        /// <summary>
        /// Fills the small rectangle with a color depending on the call status
        /// </summary>
        /// <param name="priorityCallTabItem"></param>
        /// <returns></returns>
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
            lock (_threadLock)
            {
                int i = Items.FindIndex(x => x.Call.CallId == call.CallId);
                if (i == -1)
                {
                    Log.Error($"OpenCallListTabPage.Dispatch_OnCallCompleted(): Unable to remove call '{call.ScenarioInfo.Name}' with id {call.CallId} as it does not exist in the list");
                    return;
                }

                // Always refresh
                Items.RemoveAt(i);
                RefreshIndex();
            }
        }

        private void Dispatch_OnCallAdded(PriorityCall call)
        {
            lock (_threadLock)
            {
                var tItem = new PriorityCallTabItem(call);
                tItem.Activated += CallListItem_Activated;
                Items.Add(tItem);

                // Apply ordering
                Items = Items.OrderByDescending(x => Dispatch.CanAssignAgencyToCall(Dispatch.PlayerAgency, x.Call))
                    .ThenBy(x => (int)x.Call.Priority)
                    .ThenBy(x => x.Call.CallCreated).ToList();

                // Always refresh
                RefreshIndex();
            }
        }

        private void CallListItem_Activated(object sender, EventArgs e)
        {
            var call = Items[Index].Call;

            // Check agency
            if (!Dispatch.CanAssignAgencyToCall(Dispatch.PlayerAgency, call))
            {
                Rage.Game.DisplaySubtitle("~o~This call is outside your Agency's Jurisdiction", 5000);
            }
            else if (Dispatch.InvokeCallForPlayer(call))
            {
                Rage.Game.DisplaySubtitle("~b~You will be dispatched to this call once you exit the menu", 5000);
            }
            else
            {
                Rage.Game.DisplaySubtitle("~o~You are already on an active Callout or Assignment", 5000);
            }
        }

        #endregion
    }
}
