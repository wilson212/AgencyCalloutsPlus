using RAGENativeUI.PauseMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Mod.NativeUI
{
    internal class MyTabItem : TabItem
    {
        public MyTabItem(string name) : base(name)
        {
            
        }

        public virtual void Draw()
        {
            if (!Visible) return;

            base.Draw();
        }
    }
}
