﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Mod.NativeUI.Elements
{
    internal interface IElement
    {
        /// <summary>
		/// Gets or sets a value indicating whether this <see cref="IElement"/> will be drawn.
		/// </summary>
		/// <value>
		///   <c>true</c> if enabled; otherwise, <c>false</c>.
		/// </value>
		bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the color of this <see cref="IElement"/>.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        Color Color { get; set; }

        /// <summary>
        /// Gets or sets the position of this <see cref="IElement"/>.
        /// </summary>
        /// <value>
        /// The position scaled on a 1280*720 pixel base.
        /// </value>
        /// <remarks>
        /// If ScaledDraw is called, the position will be scaled by the width returned in <see cref="Screen.ScaledWidth"/>.
        /// </remarks>
        PointF Position { get; set; }

        /// <summary>
        /// Draws this <see cref="IElement"/> this frame.
        /// </summary>
        void Draw();

        /// <summary>
        /// Draws this <see cref="IElement"/> this frame at the specified offset.
        /// </summary>
        /// <param name="offset">The offset to shift the draw position of this <see cref="IElement"/> using a 1280*720 pixel base.</param>
        void Draw(SizeF offset);
    }
}
