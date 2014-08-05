// OsmSharp - OpenStreetMap (OSM) SDK
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

using System;
using OsmSharp.UI.Map.Layers;
using OsmSharp.UI.Map;
using OsmSharp.Math.Geo;
using OsmSharp.UI.Renderer;
using OsmSharp.Math.Geo.Projections;
using OsmSharp.UI.Renderer.Scene;
using OsmSharp.UI.Renderer.Primitives;
using System.Collections.Generic;

namespace OsmSharp.UI.Map.Layers
{
    /// <summary>
    /// A layer containing several simple primitives.
    /// </summary>
    public class LayerPrimitives : Layer
    {
        /// <summary>
        /// Holds the projection for this layer.
        /// </summary>
        private IProjection _projection;
        /// <summary>
        /// Holds the scene.
        /// </summary>
        private Scene2D _scene;

        /// <summary>
        /// Creates a new primitives layer.
        /// </summary>
        public LayerPrimitives(IProjection projection)
        {
            _projection = projection;

            _scene = new Scene2D(projection, 16);
        }

        /// <summary>
        /// Adds a point.
        /// </summary>
        /// <returns>The point.</returns>
        /// <param name="coordinate">Coordinate.</param>
        /// <param name="sizePixels">Size pixels.</param>
        /// <param name="color"></param>
        public void AddPoint(GeoCoordinate coordinate, float sizePixels, int color)
        {
            double[] projectedCoordinates = _projection.ToPixel(
                                       coordinate);
            uint pointId = _scene.AddPoint(projectedCoordinates[0], projectedCoordinates[1]);
            _scene.AddStylePoint(pointId, 0, float.MinValue, float.MaxValue, color, sizePixels);
            this.RaiseLayerChanged();
        }

        /// <summary>
        /// Adds a line.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="sizePixels"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public void AddLine(GeoCoordinate point1, GeoCoordinate point2, float sizePixels, int color)
        {
            double[] projected1 = _projection.ToPixel(point1);
            double[] projected2 = _projection.ToPixel(point2);

            double[] x = new double[] { projected1[0], projected2[0] };
            double[] y = new double[] { projected1[1], projected2[1] };

            uint? pointsId = _scene.AddPoints(x, y);
            if (pointsId.HasValue)
            {
                _scene.AddStyleLine(pointsId.Value, 0, float.MinValue, float.MaxValue, color, sizePixels, Renderer.Primitives.LineJoin.Round, Renderer.Primitives.LineCap.Round, null);
                this.RaiseLayerChanged();
            }
        }

        /// <summary>
        /// Adds a polyline.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="sizePixels"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public void AddPolyline(GeoCoordinate[] points, float sizePixels, int color)
        {
            var x = new double[points.Length];
            var y = new double[points.Length];
            for(int idx = 0; idx < points.Length; idx++)
            {
                var projected =_projection.ToPixel(points[idx]);
                x[idx] = projected[0];
                y[idx] = projected[1];
            }

            uint? pointsId = _scene.AddPoints(x, y);
            if (pointsId.HasValue)
            {
                _scene.AddStyleLine(pointsId.Value, 0, float.MinValue, float.MaxValue, color, sizePixels, Renderer.Primitives.LineJoin.Round, Renderer.Primitives.LineCap.Round, null);
                this.RaiseLayerChanged();
            }
        }

        /// <summary>
        /// Adds a polyline.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="color"></param>
        /// <param name="width"></param>
        /// <param name="fill"></param>
        /// <returns></returns>
        public void AddPolygon(GeoCoordinate[] points, int color, float width, bool fill)
        {
            var x = new double[points.Length];
            var y = new double[points.Length];
            for (int idx = 0; idx < points.Length; idx++)
            {
                var projected = _projection.ToPixel(points[idx]);
                x[idx] = projected[0];
                y[idx] = projected[1];
            }

            var pointsId = _scene.AddPoints(x, y);
            if (pointsId.HasValue)
            {
                _scene.AddStylePolygon(pointsId.Value, 0, float.MinValue, float.MaxValue, color, width, fill);
                this.RaiseLayerChanged();
            }
        }

        /// <summary>
        /// Clears all data from this layer.
        /// </summary>
        public void Clear()
        {
            _scene.Clear();
        }

        /// <summary>
        /// Called when the view on the map has changed.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="zoomFactor"></param>
        /// <param name="center"></param>
        /// <param name="view"></param>
        /// <param name="extraView"></param>
        protected internal override void ViewChanged(Map map, float zoomFactor, GeoCoordinate center, View2D view, View2D extraView)
        {
            // all data is preloaded for now.

            // when displaying huge amounts of GPX-data use another approach.
        }

        /// <summary>
        /// Returns all the object from this layer visible for the given parameters.
        /// </summary>
        /// <param name="zoomFactor"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        protected internal override IEnumerable<Primitive2D> Get(float zoomFactor, View2D view)
        {
            return _scene.Get(view, zoomFactor);
        }

        /// <summary>
        /// Closes this layer.
        /// </summary>
        public override void Close()
        {
            base.Close();

            // nothing to stop, even better!
        }
    }
}