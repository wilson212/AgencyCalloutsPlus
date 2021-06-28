using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Game
{
    public class AnimationMeta
    {
        public AnimationData Data { get; set; }

        // @todo

        /// <summary>
        /// Enables casting an <see cref="AnimationMeta"/> instance to a <see cref="AnimationData"/>
        /// </summary>
        /// <param name="p">The <see cref="AnimationMeta"/> instance</param>
        public static implicit operator AnimationData(AnimationMeta p)
        {
            return p.Data;
        }
    }
}
