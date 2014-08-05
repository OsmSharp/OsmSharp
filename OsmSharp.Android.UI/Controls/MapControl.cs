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

using Android.Views;
using Android.Widget;
using OsmSharp.Math.Geo;
using OsmSharp.Math.Geo.Projections;
using OsmSharp.UI.Renderer;
using System;

namespace OsmSharp.Android.UI.Controls
{
    /// <summary>
    /// A wrapper around a view that can appear on top of the MapView.
    /// </summary>
    public abstract class MapControl : IDisposable
    {
        /// <summary>
        /// Returns the view baseclass.
        /// </summary>
        public abstract View BaseView
        {
            get;
        }

        /// <summary>
        /// Returns the handle.
        /// </summary>
        public abstract IntPtr Handle
        {
            get;
        }


        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        public abstract GeoCoordinate Location
        {
            get;
            set;
        }

        /// <summary>
        /// holds pointer to user-object
        /// </summary>
        public virtual object Tag { get; set; }

        /// <summary>
        /// Attaches this control to the given control host.
        /// </summary>
        /// <param name="controlHost">Map view.</param>
        internal abstract void AttachTo(IMapControlHost controlHost);

        /// <summary>
        /// Sets layout.
        /// </summary>
        /// <param name="pixelsWidth"></param>
        /// <param name="pixelsHeight"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <returns></returns>
        internal abstract bool SetLayout(double pixelsWidth, double pixelsHeight, View2D view, IProjection projection);

        /// <summary>
        /// Disposes of all native resources.
        /// </summary>
        public abstract void Dispose();
    }

    /// <summary>
    /// A wrapper around a view that can appear on top of the MapView.
    /// </summary>
    /// <typeparam name="TView"></typeparam>
    public class MapControl<TView> : MapControl
        where TView : View
    {
        /// <summary>
        /// Holds the view being displayed.
        /// </summary>
        private TView _view;

        /// <summary>
        /// Holds the control host where this 
        /// </summary>
        private IMapControlHost _controlHost;

        /// <summary>
        /// Holds the default alignment.
        /// </summary>
        private MapControlAlignmentType _alignment;

        /// <summary>
        /// Creates a MapControl based on the given view.
        /// </summary>
        /// <param name="location">The location the view has to stay at.</param>
        /// <param name="view">The view being wrapped.</param>
        /// <param name="alignment">The alignment.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public MapControl(TView view, GeoCoordinate location, MapControlAlignmentType alignment, int width, int height)
        {
            _view = view;
            _location = location;
            _alignment = alignment;

            _view.SetMinimumWidth(width);
            _view.SetMinimumHeight(height);

            var layoutParams = new FrameLayout.LayoutParams(width, height + 5);
            layoutParams.LeftMargin = -1;
            layoutParams.TopMargin = -1;
            layoutParams.Gravity = GravityFlags.Top | GravityFlags.Left;
            _view.LayoutParameters = layoutParams;
        }

        /// <summary>
        /// Returns the view.
        /// </summary>
        public TView View
        {
            get
            {
                return _view;
            }
        }

        /// <summary>
        /// Returns the base view.
        /// </summary>
        public override View BaseView
        {
            get
            {
                return this.View;
            }
        }

        /// <summary>
        /// Returns the handle.
        /// </summary>
        public override IntPtr Handle
        {
            get
            {
                return _view.Handle;
            }
        }

        /// <summary>
        /// Holds this markers location.
        /// </summary>
        private GeoCoordinate _location;

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        public override GeoCoordinate Location
        {
            get
            {
                return _location;
            }
            set
            {
                _location = value;

                if (_controlHost != null && _location != null)
                {
                    _controlHost.NotifyControlChange(this);
                }
            }
        }

        /// <summary>
        /// Gets or sets the alignment.
        /// </summary>
        public MapControlAlignmentType Alighnment
        {
            get
            {
                return _alignment;
            }
            set
            {
                _alignment = value;

                if (_controlHost != null && _location != null)
                {
                    _controlHost.NotifyControlChange(this);
                }
            }
        }

        /// <summary>
        /// Attaches this control to the given control host.
        /// </summary>
        /// <param name="controlHost">Map view.</param>
        internal override void AttachTo(IMapControlHost controlHost)
        {
            _controlHost = controlHost;
        }

        /// <summary>
        /// Returns the current control host.
        /// </summary>
        protected IMapControlHost Host
        {
            get
            {
                return _controlHost;
            }
        }

        /// <summary>
        /// Sets the layout.
        /// </summary>
        /// <param name="pixelsWidth">Pixels width.</param>
        /// <param name="pixelsHeight">Pixels height.</param>
        /// <param name="view">View.</param>
        /// <param name="projection">Projection.</param>
        internal override bool SetLayout(double pixelsWidth, double pixelsHeight, View2D view, IProjection projection)
        {
            if (this.Location != null)
            { // only set layout if there is a location set.
                var projected = projection.ToPixel(this.Location);
                var locationPixel = view.ToViewPort(pixelsWidth, pixelsHeight, projected[0], projected[1]);

                // set the new location depending on the size of the image and the alignment parameter.
                double leftMargin = locationPixel[0];
                double topMargin = locationPixel[1];

                leftMargin = locationPixel[0] - (this.View.LayoutParameters as FrameLayout.LayoutParams).Width / 2.0;

                switch (_alignment)
                {
                    case MapControlAlignmentType.Center:
                        topMargin = locationPixel[1] - (this.View.LayoutParameters as FrameLayout.LayoutParams).Height / 2.0;
                        break;
                    case MapControlAlignmentType.CenterTop:
                        break;
                    case MapControlAlignmentType.CenterBottom:
                        topMargin = locationPixel[1] - (this.View.LayoutParameters as FrameLayout.LayoutParams).Height;
                        break;
                }

                (this.View.LayoutParameters as FrameLayout.LayoutParams).LeftMargin = (int)leftMargin;
                (this.View.LayoutParameters as FrameLayout.LayoutParams).TopMargin = (int)topMargin;
            }
            return true;
        }

        public override void Dispose()
        {

        }
    }
}