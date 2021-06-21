using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Dispatching
{
    public class RadioCancelEventArgs
    {
        /// <summary>
        /// A default <see cref="RadioCancelEventArgs"/>
        /// </summary>
        public static RadioCancelEventArgs None = new RadioCancelEventArgs();

        /// <summary>
        /// If true, the <see cref="RadioMessage"/> will be cancelled and not play
        /// </summary>
        public bool Cancel = false;
    }
}
