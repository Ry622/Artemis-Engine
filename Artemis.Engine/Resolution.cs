﻿#region Using Statements

using Microsoft.Xna.Framework;

using System.Windows.Forms;

#endregion

namespace Artemis.Engine
{
    /// <summary>
    /// A valid screen resolution.
    /// </summary>
    public struct Resolution
    {
        public readonly int Width, Height;

        /// <summary>
        /// The aspect ratio of the screen.
        /// </summary>
        public double AspectRatio { get { return (float)Width / Height; } }

        /// <summary>
        /// The center of the screen.
        /// </summary>
        public Vector2 Center { get { return new Vector2(Width / 2, Height / 2); } }

        /// <summary>
        /// The minimum value of the width and height. This is used to properly scale
        /// sprites relative to the maximum valid resolution height.
        /// </summary>
        public int Min { get { return Width < Height ? Width : Height; }}

        public int Max { get { return Width > Height ? Width : Height; } }

        public bool IsLandscape { get { return Width > Height; } }

        public bool IsPortrait { get { return Width < Height; } }

        public Resolution(int w, int h)
        {
            Width = w;
            Height = h;
        }

        /// <summary>
        /// The native screen resolution.
        /// </summary>
        public static Resolution Native = new Resolution(Screen.PrimaryScreen.Bounds.Width,
                                                         Screen.PrimaryScreen.Bounds.Height);

        public override string ToString()
        {
            return Width.ToString() + "x" + Height.ToString();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Resolution resItem = (Resolution)obj;

            return resItem.Width == Width && resItem.Height == Height;
        }

        public static bool operator ==(Resolution a, Resolution b)
        {
            return a.Width == b.Width && a.Height == b.Height;
        }

        public static bool operator !=(Resolution a, Resolution b)
        {
            return !(a == b);
        }

        public static Vector2 operator /(Resolution a, Resolution b)
        {
            return new Vector2((float)a.Width / b.Width, (float)a.Height / b.Height);
        }

        public static explicit operator Rectangle(Resolution r)
        {
            return new Rectangle(0, 0, r.Width, r.Height);
        }
    }
}
