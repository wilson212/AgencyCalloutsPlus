using System;
using System.Drawing;
using static Rage.Native.NativeFunction;

namespace AgencyCalloutsPlus.Mod.UI
{
    //
    // Copyright (C) 2015 crosire & contributors
    // License: https://github.com/crosire/scripthookvdotnet#license
    //
    internal class TextElement : IElement
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TextElement" /> will be drawn.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the color of this <see cref="TextElement" />.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        public Color Color { get; set; } = Color.WhiteSmoke;

        /// <summary>
        /// Gets or sets the position of this <see cref="TextElement" />.
        /// </summary>
        /// <value>
        /// The position scaled on a 1280*720 pixel base.
        /// </value>
        /// <remarks>
        /// If ScaledDraw is called, the position will be scaled by the width returned in <see cref="Screen.ScaledWidth" />.
        /// </remarks>
        public PointF Position { get; set; }

        /// <summary>
        /// Gets or sets the scale of this <see cref="TextElement"/>.
        /// </summary>
        /// <value>
        /// The scale usually a value between ~0.5 and 3.0, Default = 1.0
        /// </value>
        public float Scale { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the text size of this <see cref="TextElement"/>.
        /// </summary>
        /// <value>
        /// The scale usually a value between ~0.5 and 3.0, Default = 1.0
        /// </value>
        public float Size { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the font of this <see cref="TextElement"/>.
        /// </summary>
        /// <value>
        /// The GTA Font use when drawing.
        /// </value>
        public Font Font { get; set; } = Font.ChaletLondon;

        /// <summary>
        /// Gets or sets the text to draw in this <see cref="TextElement"/>.
        /// </summary>
        /// <value>
        /// The caption.
        /// </value>
        public string Caption { get; set; } = String.Empty;

        /// <summary>
        /// Gets or sets the alignment of this <see cref="TextElement"/>.
        /// </summary>
        /// <value>
        /// The alignment:<c>Left</c>, <c>Center</c>, <c>Right</c> Justify
        /// </value>
        public Alignment Alignment { get; set; } = Alignment.Left;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TextElement"/> is drawn with a shadow effect.
        /// </summary>
        /// <value>
        ///   <c>true</c> if shadow; otherwise, <c>false</c>.
        /// </value>
        public bool Shadow { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TextElement"/> is drawn with an outline.
        /// </summary>
        /// <value>
        ///   <c>true</c> if outline; otherwise, <c>false</c>.
        /// </value>
        public bool Outline { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum size of the <see cref="TextElement"/> before it wraps to a new line.
        /// </summary>
        /// <value>
        /// The width of the <see cref="TextElement"/>.
        /// </value>
        public float WrapWidth { get; set; } = 0.0f;

        /// <summary>
        /// Gets or sets a value indicating whether the alignment of this <see cref="TextElement" /> is centered.
        /// See <see cref="Alignment"/>
        /// </summary>
        /// <value>
        ///   <c>true</c> if centered; otherwise, <c>false</c>.
        /// </value>
        public bool Centered => Alignment == Alignment.Center;

        /// <summary>
        /// Measures how many pixels in the horizontal axis this <see cref="TextElement"/> will use when drawn	against a 1280 pixel base
        /// </summary>
        public float Width
        {
            get
            {
                return Screen.Width * GetStringSize(Caption, Font, Scale).Width;
            }
        }

        /// <summary>
        /// Measures how many pixels in the horizontal axis this <see cref="TextElement"/> will use when drawn against a <see cref="ScaledWidth"/> pixel base
        /// </summary>
        public float ScaledWidth
        {
            get
            {
                return Screen.ScaledWidth * GetStringSize(Caption, Font, Scale).Width;
            }
        }

        /// <summary>
        /// Measures how many pixels in the vertical axis this <see cref="TextElement"/> will use when drawn against a 1280 pixel base
        /// </summary>
        public float Height
        {
            get
            {
                return Screen.Width * GetStringSize(Caption, Font, Scale).Height;
            }
        }

        /// <summary>
        /// Measures how many pixels in the vertical axis this <see cref="TextElement"/> will use when drawn against a <see cref="ScaledHeight"/> pixel base
        /// </summary>
        public float ScaledHeight
        {
            get
            {
                return Screen.ScaledWidth * GetStringSize(Caption, Font, Scale).Height;
            }
        }

        /// <summary>
        /// Gets the current text size in pixels
        /// </summary>
        public SizeF TextSize
        {
            get
            {
                return GetStringSize(Caption, Font, Scale);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextElement"/> class used for drawing text on the screen.
        /// </summary>
        /// <param name="caption">The <see cref="TextElement"/> to draw.</param>
        /// <param name="position">Set the <see cref="Position"/> on screen where to draw the <see cref="TextElement"/>.</param>
        public TextElement(string caption, PointF position)
        {
            Caption = caption;
            Position = position;
        }

        /// <summary>
        /// Measures how many pixels in the horizontal axis the string will use when drawn
        /// </summary>
        /// <param name="text">The string of text to measure.</param>
        /// <param name="font">The <see cref="GTA.UI.Font"/> of the textu to measure.</param>
        /// <param name="scale">Sets a sclae value for increasing or decreasing the size of the text, default value 1.0f - no scaling.</param>
        /// <returns>
        /// 
        /// </returns>
        public static SizeF GetStringSize(string text, Font font = Font.ChaletLondon, float scale = 1.0f, float size = 1.0f)
        {
            Natives.BeginTextCommandWidth("STRING");
            Natives.AddTextComponentSubstringPlayerName(text);

            Natives.SetTextFont((int)font);
            Natives.SetTextScale(scale, size);

            var width = Natives.EndTextCommandGetWidth<float>(true);
            var height = Natives.GetTextScaleHeight<float>(scale, (int)font);

            return new SizeF(width, height);
        }

        /// <summary>
        /// Draws the <see cref="TextElement" /> this frame.
        /// </summary>
        public virtual void Draw()
        {
            Draw(SizeF.Empty);
        }

        /// <summary>
        /// Draws the <see cref="TextElement" /> this frame at the specified offset.
        /// </summary>
        /// <param name="offset">The offset to shift the draw position of this <see cref="TextElement" /> using a 1280*720 pixel base.</param>
        public virtual void Draw(SizeF offset)
        {
            InternalDraw(offset, Screen.Width, Screen.Height);
        }

        /// <summary>
        /// Draws the <see cref="TextElement" /> this frame using the width returned in <see cref="Screen.ScaledWidth" />.
        /// </summary>
        public virtual void ScaledDraw()
        {
            ScaledDraw(SizeF.Empty);
        }

        /// <summary>
        /// Draws the <see cref="TextElement" /> this frame at the specified offset using the width returned in <see cref="Screen.ScaledWidth" />.
        /// </summary>
        /// <param name="offset">The offset to shift the draw position of this <see cref="TextElement" /> using a <see cref="Screen.ScaledWidth" />*720 pixel base.</param>
        public virtual void ScaledDraw(SizeF offset)
        {
            InternalDraw(offset, Screen.ScaledWidth, Screen.Height);
        }

        protected void InternalDraw(SizeF offset, float screenWidth, float screenHeight)
        {
            // If not enabled, quit here
            if (!Enabled) return;

            float x = (Position.X + offset.Width) / screenWidth;
            float y = (Position.Y + offset.Height) / screenHeight;
            float w = WrapWidth / screenWidth;

            if (Shadow)
            {
                Natives.SetTextDropShadow();
            }

            if (Outline)
            {
                Natives.SetTextOutline();
            }

            Natives.SetTextFont((int)Font);
            Natives.SetTextScale(Scale, Size);
            Natives.SetTextColour(Color.R, Color.G, Color.B, Color.A);
            Natives.SetTextJustification((int)Alignment);

            if (WrapWidth > 0.0f)
            {
                switch (Alignment)
                {
                    case Alignment.Center:
                        Natives.SetTextWrap(x - (w / 2), x + (w / 2));
                        break;
                    case Alignment.Left:
                        Natives.SetTextWrap(x, x + w);
                        break;
                    case Alignment.Right:
                        Natives.SetTextWrap(x - w, x);
                        break;
                }
            }
            else if (Alignment == Alignment.Right)
            {
                Natives.SetTextWrap(0.0f, x);
            }

            Natives.BeginTextCommandDisplayText("STRING");
            Natives.AddTextComponentSubstringPlayerName(Caption);
            Natives.EndTextCommandDisplayText(x, y);
        }
    }
}
