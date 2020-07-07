using System;
using System.Collections.Generic;
using System.Drawing;
using static Rage.Native.NativeFunction;


namespace AgencyCalloutsPlus.Mod.UI
{
    //
    // Copyright (C) 2015 crosire & contributors
    // License: https://github.com/crosire/scripthookvdotnet#license
    //
    internal class ContainerElement : IElement
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ContainerElement"/> will be drawn.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public virtual bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the color of this <see cref="ContainerElement"/>.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        public virtual Color Color { get; set; } = Color.Transparent;

        /// <summary>
        /// Gets or sets the position of this <see cref="ContainerElement"/>.
        /// </summary>
        /// <value>
        /// The position scaled on a 1280*720 pixel base.
        /// </value>
        /// <remarks>
        /// If ScaledDraw is called, the position will be scaled by the width returned in <see cref="Screen.ScaledWidth"/>.
        /// </remarks>
        public virtual PointF Position { get; set; }

        /// <summary>
        /// Gets or sets the size to draw the <see cref="ContainerElement"/>
        /// </summary>
        /// <value>
        /// The size on a 1280*720 pixel base
        /// </value>
        /// <remarks>
        /// If ScaledDraw is called, the size will be scaled by the width returned in <see cref="Screen.ScaledWidth"/>.
        /// </remarks>
        public SizeF Size { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ContainerElement"/> should be positioned based on its center or top left corner
        /// </summary>
        /// <value>
        ///   <c>true</c> if centered; otherwise, <c>false</c>.
        /// </value>
        public virtual bool Centered { get; set; } = false;

        /// <summary>
        /// The <see cref="IElement"/>s Contained inside this <see cref="ContainerElement"/>
        /// </summary>
        public List<IElement> Items { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerElement"/> class used for grouping items on screen.
        /// </summary>
        /// <param name="position">Set the <see cref="Position"/> on screen where to draw the <see cref="ContainerElement"/>.</param>
        /// <param name="size">Set the <see cref="Size"/> of the <see cref="ContainerElement"/>.</param>
        public ContainerElement(PointF position, SizeF size)
        {
            Position = position;
            Size = size;
            Items = new List<IElement>();
        }

        /// <summary>
        /// Draws this <see cref="ContainerElement" /> this frame.
        /// </summary>
        public virtual void Draw()
        {
            Draw(SizeF.Empty);
        }

        /// <summary>
        /// Draws this <see cref="ContainerElement" /> this frame at the specified offset.
        /// </summary>
        /// <param name="offset">The offset to shift the draw position of this <see cref="ContainerElement" /> using a 1280*720 pixel base.</param>
        public virtual void Draw(SizeF offset)
        {
            if (!Enabled)
            {
                return;
            }

            InternalDraw(offset, Screen.Width, Screen.Height);

            offset += new SizeF(Position);

            if (Centered)
            {
                offset -= new SizeF(Size.Width * 0.5f, Size.Height * 0.5f);
            }

            foreach (var item in Items)
            {
                item.Draw(offset);
            }
        }

        /// <summary>
        /// Draws this <see cref="ContainerElement" /> this frame using the width returned in <see cref="Screen.ScaledWidth" />.
        /// </summary>
        public virtual void ScaledDraw()
        {
            ScaledDraw(SizeF.Empty);
        }

        /// <summary>
        /// Draws this <see cref="ContainerElement" /> this frame at the specified offset using the width returned in <see cref="Screen.ScaledWidth" />.
        /// </summary>
        /// <param name="offset">The offset to shift the draw position of this <see cref="ContainerElement" /> using a <see cref="Screen.ScaledWidth" />*720 pixel base.</param>
        public virtual void ScaledDraw(SizeF offset)
        {
            if (!Enabled)
            {
                return;
            }

            InternalDraw(offset, Screen.ScaledWidth, Screen.Height);

            offset += new SizeF(Position);

            if (Centered)
            {
                offset -= new SizeF(Size.Width * 0.5f, Size.Height * 0.5f);
            }

            foreach (var item in Items)
            {
                item.ScaledDraw(offset);
            }
        }

        protected void InternalDraw(SizeF offset, float screenWidth, float screenHeight)
        {
            float w = Size.Width / screenWidth;
            float h = Size.Height / screenHeight;
            float x = (Position.X + offset.Width) / screenWidth;
            float y = (Position.Y + offset.Height) / screenHeight;

            if (!Centered)
            {
                x += w * 0.5f;
                y += h * 0.5f;
            }

            Natives.DrawRect(x, y, w, h, Color.R, Color.G, Color.B, Color.A);
        }
    }
}
