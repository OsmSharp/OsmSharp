﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2013 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using OsmSharp.Math.Primitives;
namespace OsmSharp.UI.Renderer.Primitives
{
    /// <summary>
    /// A simple 2D line.
    /// </summary>
    public class LineText2D : Primitive2D
    {
        /// <summary>
        /// Creates a new line2D.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="lineJoin"></param>
        /// <param name="dashes"></param>
        public LineText2D(double[] x, double[] y, int color, float size, string text)
        {
            this.X = x;
            this.Y = y;
            this.Color = color;
            this.Size = size;
            this.Text = text;

            MinX = int.MaxValue;
            MaxX = int.MinValue;
            for (int idx = 0; idx < x.Length; idx++)
            {
                if (x[idx] > MaxX)
                {
                    MaxX = x[idx];
                }
                if (x[idx] < MinX)
                {
                    MinX = x[idx];
                }
            }
            MinY = int.MaxValue;
            MaxY = int.MinValue;
            for (int idx = 0; idx < y.Length; idx++)
            {
                if (y[idx] > MaxY)
                {
                    MaxY = y[idx];
                }
                if (y[idx] < MinY)
                {
                    MinY = y[idx];
                }
            }

            this.MinZoom = float.MinValue;
            this.MaxZoom = float.MaxValue;
        }

        /// <summary>
        /// Creates a new line2D.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="lineJoin"></param>
        /// <param name="dashes"></param>
        /// <param name="minZoom"></param>
        /// <param name="maxZoom"></param>
        public LineText2D(double[] x, double[] y, int color, float size, string text, int? haloColor, int? haloRadius,
            float minZoom, float maxZoom)
        {
            this.X = x;
            this.Y = y;
            this.Color = color;
            this.Size = size;
            this.Text = text;
            this.HaloColor = haloColor;
            this.HaloRadius = haloRadius;

            MinX = int.MaxValue;
            MaxX = int.MinValue;
            for (int idx = 0; idx < x.Length; idx++)
            {
                if (x[idx] > MaxX)
                {
                    MaxX = x[idx];
                }
                if (x[idx] < MinX)
                {
                    MinX = x[idx];
                }
            }
            MinY = int.MaxValue;
            MaxY = int.MinValue;
            for (int idx = 0; idx < y.Length; idx++)
            {
                if (y[idx] > MaxY)
                {
                    MaxY = y[idx];
                }
                if (y[idx] < MinY)
                {
                    MinY = y[idx];
                }
            }

            this.MinZoom = minZoom;
            this.MaxZoom = maxZoom;
        }

        /// <summary>
        /// Creates a new line2D.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="lineJoin"></param>
        /// <param name="minX"></param>
        /// <param name="maxX"></param>
        /// <param name="minY"></param>
        /// <param name="maxY"></param>
        public LineText2D(double[] x, double[] y, int color, float size, string text,
            int minX, int maxX, int minY, int maxY)
        {
            this.X = x;
            this.Y = y;
            this.Color = color;
            this.Size = size;
            this.Text = text;

            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;

            this.MinZoom = float.MinValue;
            this.MaxZoom = float.MaxValue;
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// Returns the type of this primitive.
        /// </summary>
        public override Primitive2DType Primitive2DType
        {
            get { return Primitives.Primitive2DType.LineText2D; }
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the x.
        /// </summary>
        /// <value>The x.</value>
        public double[] X { get; set; }

        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        public double[] Y { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public int Color { get; set; }

        /// <summary>
        /// Gets or sets the halo size.
        /// </summary>
        public int? HaloRadius { get; set; }

        /// <summary>
        /// Gets or sets the halo color.
        /// </summary>
        public int? HaloColor { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public float Size { get; set; }

        /// <summary>
        /// Gets or sets the font name.
        /// </summary>
        public string Font { get; set; }

        /// <summary>
        /// The minimum zoom.
        /// </summary>
        public float MinZoom { get; set; }

        /// <summary>
        /// The maximum zoom.
        /// </summary>
        public float MaxZoom { get; set; }

        #region IScene2DPrimitive implementation

        internal double MinX { get; set; }
        internal double MaxX { get; set; }
        internal double MinY { get; set; }
        internal double MaxY { get; set; }

        /// <summary>
        /// Returns true if the object is visible on the view.
        /// </summary>
        /// <returns>true</returns>
        /// <c>false</c>
        /// <param name="view">View.</param>
        /// <param name="zoom"></param>
        public override bool IsVisibleIn(View2D view, float zoom)
        {
            if (this.MinZoom > zoom || this.MaxZoom < zoom)
            { // outside of zoom bounds!
                return false;
            }

            if (view.OverlapsWithBox(MinX, MinY, MaxX, MaxY))
            {
                return true; // maybe a better hittest?
            }
            return false;
        }

        /// <summary>
        /// Returns true if the object is visible on the view.
        /// </summary>
        /// <returns>true</returns>
        /// <c>false</c>
        /// <param name="view">View.</param>
        /// <param name="zoom"></param>
        public override bool IsVisibleIn(View2D view)
        {
            if (view.OverlapsWithBox(MinX, MinY, MaxX, MaxY))
            {
                return true; // maybe a better hittest?
            }
            return false;
        }

        /// <summary>
        /// Returns the bounding box for this primitive.
        /// </summary>
        /// <returns></returns>
        public override BoxF2D GetBox()
        {
            return new BoxF2D(MinX, MinY, MaxX, MaxY);
        }

        #endregion
    }
}