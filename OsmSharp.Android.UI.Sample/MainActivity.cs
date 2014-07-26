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

using Android.App;
using Android.OS;
using Android.Widget;
using OsmSharp.Android.UI.Data.SQLite;
using OsmSharp.Android.UI.Renderer.Images;
using OsmSharp.Collections.Tags;
using OsmSharp.Math.Geo;
using OsmSharp.Routing;
using OsmSharp.Routing.CH;
using OsmSharp.Routing.Navigation;
using OsmSharp.Routing.Osm.Interpreter;
using OsmSharp.UI;
using OsmSharp.UI.Animations.Navigation;
using OsmSharp.UI.Map;
using OsmSharp.UI.Map.Layers;
using OsmSharp.UI.Renderer.Scene;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Timers;
using Android.Graphics;

namespace OsmSharp.Android.UI.Sample
{
    /// <summary>
    /// The main activity.
    /// </summary>
    // [Activity(ConfigurationChanges = global::Android.Content.PM.ConfigChanges.Orientation | global::Android.Content.PM.ConfigChanges.ScreenLayout)]
    [Activity]
    public class MainActivity : Activity
    {
        /// <summary>
        /// Holds the router.
        /// </summary>
        private Router _router;

        /// <summary>
        /// Holds the route layer.
        /// </summary>
        private LayerRoute _routeLayer;

        /// <summary>
        /// Holds the text view.
        /// </summary>
        private TextView _textView;

        /// <summary>
        /// Holds the map view.
        /// </summary>
        private MapView _mapView;

        /// <summary>
        /// Holds the router tracker animator.
        /// </summary>
        private RouteTrackerAnimator _routeTrackerAnimator;

        /// <summary>
        /// Holds the coordinate enumerator.
        /// </summary>
        private IEnumerator<GeoCoordinate> _enumerator;

        /// <summary>
        /// Raises the create event.
        /// </summary>
        /// <param name="bundle">Bundle.</param>
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // hide title bar.
            this.RequestWindowFeature(global::Android.Views.WindowFeatures.NoTitle);

            // initialize map.
            var map = new Map();
            //map.AddLayer(new LayerMBTile(SQLiteConnection.CreateFrom(
            //    Assembly.GetExecutingAssembly().GetManifestResourceStream(@"OsmSharp.Android.UI.Sample.kempen.mbtiles"), "map")));
            // add a tile layer.

            var layer = new LayerTile(@"http://a.tiles.mapbox.com/v3/osmsharp.i8ckml0l/{0}/{1}/{2}.png");
            map.AddLayer(layer);
            //map.AddLayer(new LayerTile(@"http://a.tiles.mapbox.com/v3/osmsharp.i8ckml0l/{0}/{1}/{2}.png"));
            //map.AddLayerGpx(Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Android.UI.Sample.regression1.gpx"));
            // 
            // add an on-line osm-data->mapCSS translation layer.
            //map.AddLayer(new OsmLayer(dataSource, mapCSSInterpreter));
            // add a preprocessed vector data file.
            //var sceneStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"OsmSharp.Android.UI.Sample.default.map");
            //map.AddLayer(new LayerScene(Scene2D.Deserialize(sceneStream, true)));

            // define dummy from and to points.
            //var from = new GeoCoordinate(51.261203, 4.780760);
            //var to = new GeoCoordinate(51.267797, 4.801362);

            //// deserialize the preprocessed graph.
            //var routingSerializer = new CHEdgeDataDataSourceSerializer(false);
            //TagsCollectionBase metaData = null;
            //var graphStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Android.UI.Sample.kempen-big.osm.pbf.routing");
            //var graphDeserialized = routingSerializer.Deserialize(graphStream, out metaData, true);

            //// initialize router.
            //_router = Router.CreateCHFrom(graphDeserialized, new CHRouter(), new OsmRoutingInterpreter());

            //// resolve points.
            //var routerPoint1 = _router.Resolve(Vehicle.Car, from);
            //var routerPoint2 = _router.Resolve(Vehicle.Car, to);
            
            //// calculate route.
            //var route = _router.Calculate(Vehicle.Car, routerPoint1, routerPoint2);
            //var routeTracker = new RouteTracker(route, new OsmRoutingInterpreter());
            //_enumerator = route.GetRouteEnumerable(10).GetEnumerator();

            //// add a router layer.
            //_routeLayer = new LayerRoute(map.Projection);
            //_routeLayer.AddRoute (route, SimpleColor.FromKnownColor(KnownColor.Blue, 125).Value, 12);
            //map.AddLayer(_routeLayer);

            // define the mapview.
            _mapView = new MapView(this, new MapViewSurface(this));
            //_mapView = new MapView(this, new MapViewGLSurface(this));
            //_mapView.MapTapEvent += new MapViewEvents.MapTapEventDelegate(_mapView_MapTapEvent);
            _mapView.MapTapEvent += _mapView_MapTapEvent;
            _mapView.Map = map;
            //_mapView.MapMaxZoomLevel = 20;
            //_mapView.MapMinZoomLevel = 10;
            _mapView.MapTilt = 0;
            _mapView.MapCenter = new GeoCoordinate(51.261203, 4.780760);
            _mapView.MapZoom = 16;
            _mapView.MapAllowTilt = false;
            _mapView.MapScaleFactor = 2;

            AddMarkers();
            //_mapView.AddMarker(from);
            //_mapView.AddMarker(to);

            // initialize a text view to display routing instructions.
            _textView = new TextView(this);
            _textView.SetBackgroundColor(global::Android.Graphics.Color.White);
            _textView.SetTextColor(global::Android.Graphics.Color.Black);

            // add the mapview to the linear layout.
            var layout = new RelativeLayout(this);
            //layout.Orientation = Orientation.Vertical;
            //layout.AddView(_textView);
            layout.AddView(_mapView);

            // create the route tracker animator.
            //_routeTrackerAnimator = new RouteTrackerAnimator(_mapView, routeTracker, 5, 17);

            //// simulate a mapzoom change every 5 seconds.
            //Timer timer = new Timer(200);
            //timer.Elapsed += new ElapsedEventHandler(TimerHandler);
            //timer.Start();

            SetContentView(layout);
        }

        void AddMarkers()
        {
            var from = new GeoCoordinate(51.261203, 4.780760);
            var to = new GeoCoordinate(51.267797, 4.801362);

            _mapView.ClearMarkers();

            var img = BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.marker);
            MapMarker marker1 = new MapMarker(this, from, MapMarkerAlignmentType.CenterBottom, img);
            _mapView.AddMarker(marker1);

            _mapView.AddMarker(to);
        }

        void _mapView_MapTapEvent(GeoCoordinate coordinate)
        {
            //if (_mapView.IsPaused)
            //{
            //    _mapView.Map.Resume();
            //    _mapView.Resume();
            //}
            //else
            //{
            //    _mapView.Pause();
            //    _mapView.Map.Pause();
            //}
            _mapView.Map[0].IsVisible = !_mapView.Map[0].IsVisible;
            _mapView.Map[1].IsVisible = !_mapView.Map[1].IsVisible;
        }

        public override void OnConfigurationChanged(global::Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            if (_mapView != null)
            {
                _mapView.Invalidate();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _mapView.Dispose();

            GC.Collect();
        }

        /// <summary>
        /// Handles the timer event from the timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerHandler(object sender, ElapsedEventArgs e)
        {
            _mapView.MapZoom = _mapView.MapZoom - 0.05f;
        }
    }
}
