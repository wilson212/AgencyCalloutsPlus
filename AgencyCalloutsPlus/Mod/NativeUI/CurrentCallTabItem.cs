using RAGENativeUI.PauseMenu;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Mod.NativeUI
{
    internal class CurrentCallTabItem : TabItem
    {
        public string TextTitle { get; set; }

        public string Text { get; set; }

        public int WordWrap { get; set; }

        public CurrentCallTabItem(string name, string title) : base(name)
        {
            TextTitle = title;
        }

        public override void Draw()
        {
            base.Draw();

            var alpha = (Focused || !CanBeFocused) ? 255 : 200;

            if (!String.IsNullOrEmpty(TextTitle))
            {
                //ResText.Draw(TextTitle, SafeSize.AddPoints(new Point(40, 20)), 1.5f, Color.FromArgb(alpha, Color.White), GameFont.ChaletLondon, false);
            }

            if (!String.IsNullOrEmpty(Text))
            {
                var ww = WordWrap == 0 ? BottomRight.X - TopLeft.X - 40 : WordWrap;

                //ResText.Draw(Text, SafeSize.AddPoints(new Point(40, 150)), 0.4f, Color.FromArgb(alpha, Color.White), GameFont.ChaletLondon, ResText.Alignment.Left, false, false, new Size((int)ww, 0));
            }
        }
    }
}
