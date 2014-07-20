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

using System.Collections.Generic;
using OsmSharp.Collections.Tags;
using OsmSharp.Math.Geo;
using OsmSharp.Math.Geo.Simple;
using OsmSharp.Osm;
using OsmSharp.Osm.Cache;
using OsmSharp.Osm.Streams;
using OsmSharp.Osm.Streams.Filters;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Routing.Interpreter.Roads;
using OsmSharp.Routing.Osm.Interpreter;
using OsmSharp.Routing.Graph.PreProcessor;
using OsmSharp.Collections.Tags.Index;

namespace OsmSharp.Routing.Osm.Streams.Graphs
{
    /// <summary>
    /// Data Processor Target to fill a dynamic graph object.
    /// </summary>
    public abstract class DynamicGraphOsmStreamWriter<TEdgeData> : OsmStreamTarget
        where TEdgeData : IDynamicGraphEdgeData 
    {
        /// <summary>
        /// Holds the dynamic graph.
        /// </summary>
        private readonly IDynamicGraphRouterDataSource<TEdgeData> _dynamicGraph;

        /// <summary>
        /// The interpreter for osm data.
        /// </summary>
        private readonly IOsmRoutingInterpreter _interpreter;

        /// <summary>
        /// Holds the tags index.
        /// </summary>
        private readonly ITagsCollectionIndex _tagsIndex;

        /// <summary>
        /// Holds the osm data cache.
        /// </summary>
        private readonly OsmDataCache _dataCache;

        /// <summary>
        /// True when this target is in pre-index mode.
        /// </summary>
        private bool _preIndexMode;

        /// <summary>
        /// The bounding box to limit nodes if any.
        /// </summary>
        private readonly GeoCoordinateBox _box;

        /// <summary>
        /// Holds the edge comparer.
        /// </summary>
        private readonly IDynamicGraphEdgeComparer<TEdgeData> _edgeComparer;

        /// <summary>
        /// Creates a new processor target.
        /// </summary>
        /// <param name="dynamicGraph">The graph that will be filled.</param>
        /// <param name="interpreter">The interpreter to generate the edge data.</param>
        /// <param name="edgeComparer"></param>
        protected DynamicGraphOsmStreamWriter(IDynamicGraphRouterDataSource<TEdgeData> dynamicGraph,
            IOsmRoutingInterpreter interpreter, IDynamicGraphEdgeComparer<TEdgeData> edgeComparer)
            : this(dynamicGraph, interpreter, edgeComparer, new TagsTableCollectionIndex(), new Dictionary<long, uint>())
        {

        }

        /// <summary>
        /// Creates a new processor target.
        /// </summary>
        /// <param name="dynamicGraph">The graph that will be filled.</param>
        /// <param name="interpreter">The interpreter to generate the edge data.</param>
        /// <param name="edgeComparer"></param>
        /// <param name="tagsIndex"></param>
        protected DynamicGraphOsmStreamWriter(IDynamicGraphRouterDataSource<TEdgeData> dynamicGraph,
            IOsmRoutingInterpreter interpreter, IDynamicGraphEdgeComparer<TEdgeData> edgeComparer, ITagsCollectionIndex tagsIndex)
            : this(dynamicGraph, interpreter, edgeComparer, tagsIndex, new Dictionary<long, uint>())
        {

        }

        /// <summary>
        /// Creates a new processor target.
        /// </summary>
        /// <param name="dynamicGraph">The graph that will be filled.</param>
        /// <param name="interpreter">The interpreter to generate the edge data.</param>
        /// <param name="edgeComparer"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="idTransformations"></param>
        protected DynamicGraphOsmStreamWriter(IDynamicGraphRouterDataSource<TEdgeData> dynamicGraph,
            IOsmRoutingInterpreter interpreter, IDynamicGraphEdgeComparer<TEdgeData> edgeComparer, ITagsCollectionIndex tagsIndex,
            IDictionary<long, uint> idTransformations)
            : this(dynamicGraph, interpreter, edgeComparer, tagsIndex, idTransformations, null)
        {

        }

        /// <summary>
        /// Creates a new processor target.
        /// </summary>
        /// <param name="dynamicGraph">The graph that will be filled.</param>
        /// <param name="interpreter">The interpreter to generate the edge data.</param>
        /// <param name="edgeComparer"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="idTransformations"></param>
        /// <param name="box"></param>
        protected DynamicGraphOsmStreamWriter(
            IDynamicGraphRouterDataSource<TEdgeData> dynamicGraph, IOsmRoutingInterpreter interpreter, IDynamicGraphEdgeComparer<TEdgeData> edgeComparer,
            ITagsCollectionIndex tagsIndex, IDictionary<long, uint> idTransformations,
            GeoCoordinateBox box)
        {
            _dynamicGraph = dynamicGraph;
            _interpreter = interpreter;
            _edgeComparer = edgeComparer;
            _box = box;

            _tagsIndex = tagsIndex;
            _idTransformations = idTransformations;
            _preIndexMode = true;
            _preIndex = new OsmSharp.Collections.HugeHashSet<long>();
            _usedTwiceOrMore = new OsmSharp.Collections.HugeHashSet<long>();

            _dataCache = new OsmDataCacheMemory();
        }

        /// <summary>
        /// Returns the tags index.
        /// </summary>
        public ITagsCollectionIndex TagsIndex
        {
            get
            {
                return _tagsIndex;
            }
        }

        /// <summary>
        /// Returns the target graph.
        /// </summary>
        public IDynamicGraphRouterDataSource<TEdgeData> DynamicGraph
        {
            get { return _dynamicGraph; }
        }

        /// <summary>
        /// Returns the osm routing interpreter.
        /// </summary>
        public IOsmRoutingInterpreter Interpreter
        {
            get { return _interpreter; }
        }

        /// <summary>
        /// Returns the edge comparer.
        /// </summary>
        public IDynamicGraphEdgeComparer<TEdgeData> EdgeComparer
        {
            get { return _edgeComparer; }
        }

        /// <summary>
        /// Holds the bounds of the nodes that have been added up until now.
        /// </summary>
        private GeoCoordinateBox _bounds = null;

        /// <summary>
        /// Holds the coordinates.
        /// </summary>
        private OsmSharp.Collections.HugeDictionary<long, GeoCoordinateSimple> _coordinates;

        /// <summary>
        /// Holds the index of all relevant nodes.
        /// </summary>
        private OsmSharp.Collections.HugeHashSet<long> _preIndex;

        /// <summary>
        /// Holds the id transformations.
        /// </summary>
        private readonly IDictionary<long, uint> _idTransformations;

        /// <summary>
        /// Initializes the processing.
        /// </summary>
        public override void Initialize()
        {
            _coordinates = new OsmSharp.Collections.HugeDictionary<long, GeoCoordinateSimple>();
        }

        /// <summary>
        /// Adds the given node.
        /// </summary>
        /// <param name="node"></param>
        public override void AddNode(Node node)
        {
            if (!_preIndexMode)
            {
                if (_nodesToCache != null &&
                    _nodesToCache.Contains(node.Id.Value))
                { // cache this node?
                    _dataCache.AddNode(node);
                }

                if (_preIndex != null && _preIndex.Contains(node.Id.Value))
                { // only save the coordinates for relevant nodes.
                    // save the node-coordinates.
                    // add the relevant nodes.

                    if (_box == null || _box.Contains(new GeoCoordinate((float)node.Latitude.Value, (float)node.Longitude.Value)))
                    { // the coordinate is acceptable.
                        _coordinates[node.Id.Value] = new GeoCoordinateSimple() {
                            Latitude = (float)node.Latitude.Value, 
                            Longitude = (float)node.Longitude.Value};
                        if (_coordinates.Count == _preIndex.Count)
                        {
                            _preIndex.Clear();
                            _preIndex = null;
                        }

                        if (_bounds == null)
                        { // create bounds.
                            _bounds = new GeoCoordinateBox(
                                new GeoCoordinate(node.Latitude.Value, node.Longitude.Value),
                                new GeoCoordinate(node.Latitude.Value, node.Longitude.Value));
                        }
                        else
                        { // expand bounds.
                            _bounds.ExpandWith(
                                new GeoCoordinate(node.Latitude.Value, node.Longitude.Value));
                        }

                        // add the node as a possible restriction.
                        if (_interpreter.IsRestriction(OsmGeoType.Node, node.Tags))
                        { // tests quickly if a given node is possibly a restriction.
                            List<Vehicle> vehicles = _interpreter.CalculateRestrictions(node);
                            if(vehicles != null &&
                                vehicles.Count > 0)
                            { // add all the restrictions.
                                uint vertexId = this.AddRoadNode(node.Id.Value).Value; // will always exists, has just been added to coordinates.
                                uint[] restriction = new uint[] { vertexId };
                                if (vehicles.Contains(null))
                                { // restriction is valid for all vehicles.
                                    _dynamicGraph.AddRestriction(restriction);
                                }
                                else
                                { // restriction is restricted to some vehicles only.
                                    foreach (Vehicle vehicle in vehicles)
                                    {
                                        _dynamicGraph.AddRestriction(vehicle, restriction);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Holds a list of nodes used twice or more.
        /// </summary>
        private readonly OsmSharp.Collections.HugeHashSet<long> _usedTwiceOrMore;

        /// <summary>
        /// Returns the boundingbox of all accepted nodes.
        /// </summary>
        public GeoCoordinateBox Box
        {
            get
            {
                return _bounds;
            }
        }

        /// <summary>
        /// Adds a given way.
        /// </summary>
        /// <param name="way"></param>
        public override void AddWay(Way way)
        {
            if (!_preIndexMode && _waysToCache != null &&
                _waysToCache.Contains(way.Id.Value))
            { // cache this way?
               _dataCache.AddWay(way);
            }

            // initialize the way interpreter.
            if (_interpreter.EdgeInterpreter.IsRoutable(way.Tags))
            { // the way is a road.
                if (_preIndexMode)
                { // index only relevant nodes.
                    if (way.Nodes != null)
                    {
                        foreach (long node in way.Nodes)
                        {
                            if (_preIndex.Contains(node))
                            {
                                _usedTwiceOrMore.Add(node);
                            }
                            else
                            {
                                _preIndex.Add(node); // node is relevant.
                            }
                        }
                    }
                }
                else
                {
                // add the forward edges.
                //if (!interpreter.IsOneWayReverse())
                    if (true) // add backward edges too!
                    { // loop over all edges.
                        if (way.Nodes != null && way.Nodes.Count > 1)
                        { // way has at least two nodes.
                            if (this.CalculateIsTraversable(_interpreter.EdgeInterpreter, _tagsIndex,
                                way.Tags))
                            { // the edge is traversable, add the edges.
                                uint? from = this.AddRoadNode(way.Nodes[0]);
                                for (int idx = 1; idx < way.Nodes.Count; idx++)
                                { // the to-node.
                                    uint? to = this.AddRoadNode(way.Nodes[idx]);
                                    // add the edge(s).
                                    if (from.HasValue && to.HasValue)
                                    { // add a road edge.
                                        if (!this.AddRoadEdge(way.Tags, true, from.Value, to.Value))
                                        { // add the reverse too if it has been indicated that this was needed.
                                            this.AddRoadEdge(way.Tags, false, to.Value, from.Value);
                                        }
                                    }
                                    from = to; // the to node becomes the from.
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a node that is at least part of one road.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private uint? AddRoadNode(long nodeId)
        {
            uint id;
            // try and get existing node.
            if (!_idTransformations.TryGetValue(nodeId, out id))
            {
                // get coordinates.
                GeoCoordinateSimple coordinates;
                if (_coordinates.TryGetValue(nodeId, out coordinates))
                { // the coordinate is present.
                    id = _dynamicGraph.AddVertex(
                        coordinates.Latitude, coordinates.Longitude);
                    _coordinates.Remove(nodeId); // free the memory again!

                    if (_usedTwiceOrMore.Contains(nodeId))
                    {
                        _idTransformations[nodeId] = id;
                    }
                    return id;
                }
                return null;
            }
            return id;
        }

        /// <summary>
        /// Adds an edge.
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="tags"></param>
        protected virtual bool AddRoadEdge(TagsCollectionBase tags, bool forward, uint from, uint to)
        {
            float latitude;
            float longitude;
            GeoCoordinate fromCoordinate = null;
            if (_dynamicGraph.GetVertex(from, out latitude, out longitude))
            { // 
                fromCoordinate = new GeoCoordinate(latitude, longitude);
            }
            GeoCoordinate toCoordinate = null;
            if (_dynamicGraph.GetVertex(to, out latitude, out longitude))
            { // 
                toCoordinate = new GeoCoordinate(latitude, longitude);
            }

            if (fromCoordinate != null && toCoordinate != null)
            { // calculate the edge data.
                TEdgeData edgeData = this.CalculateEdgeData(_interpreter.EdgeInterpreter, _tagsIndex, tags, forward, fromCoordinate, toCoordinate);

                _dynamicGraph.AddArc(from, to, edgeData, _edgeComparer);
            }
            return false;
        }

        /// <summary>
        /// Calculates the edge data.
        /// </summary>
        /// <returns></returns>
        protected abstract TEdgeData CalculateEdgeData(IEdgeInterpreter edgeInterpreter, ITagsCollectionIndex tagsIndex, TagsCollectionBase tags,
            bool directionForward, GeoCoordinate from, GeoCoordinate to);

        /// <summary>
        /// Returns true if the edge can be traversed.
        /// </summary>
        /// <param name="edgeInterpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        protected abstract bool CalculateIsTraversable(IEdgeInterpreter edgeInterpreter, ITagsCollectionIndex tagsIndex,
                                              TagsCollectionBase tags);

        /// <summary>
        /// Holds the ways to cache to complete the restriction reations.
        /// </summary>
        private HashSet<long> _waysToCache;

        /// <summary>
        /// Holds the node to cache to complete the restriction relations.
        /// </summary>
        private HashSet<long> _nodesToCache;

        /// <summary>
        /// Adds a given relation.
        /// </summary>
        /// <param name="relation"></param>
        public override void AddRelation(Relation relation)
        {
            if (_interpreter.IsRestriction(OsmGeoType.Relation, relation.Tags))
            {
                // add the node as a possible restriction.
                if (!_preIndexMode)
                { // tests quickly if a given node is possibly a restriction.
                    // this relation is a relation that represents a restriction all members should have been cached.
                    CompleteRelation completeRelation = CompleteRelation.CreateFrom(relation, _dataCache);
                    if (completeRelation == null) 
                    {
                        return;
                    }

                    // interpret the restriction using the complete object.
                    List<KeyValuePair<Vehicle, long[]>> vehicleRestrictions = _interpreter.CalculateRestrictions(completeRelation);
                    if (vehicleRestrictions != null &&
                        vehicleRestrictions.Count > 0)
                    { // add all the restrictions.
                        foreach (KeyValuePair<Vehicle, long[]> vehicleRestriction in vehicleRestrictions)
                        {
                            // build the restricted route.
                            uint[] restriction = new uint[vehicleRestriction.Value.Length];
                            for (int idx = 0; idx < vehicleRestriction.Value.Length; idx++)
                            {
                                restriction[idx] = this.AddRoadNode(vehicleRestriction.Value[idx]).Value;
                            }
                            if (vehicleRestriction.Key == null)
                            { // this restriction is for all vehicles.
                                _dynamicGraph.AddRestriction(restriction);
                            }
                            else
                            { // this restriction is just for the given vehicle.
                                _dynamicGraph.AddRestriction(vehicleRestriction.Key, restriction);
                            }
                        }
                    }
                }
                else
                { // pre-index mode.
                    if (relation.Members != null && relation.Members.Count > 0)
                    { // there are members, keep them!
                        foreach (RelationMember member in relation.Members)
                        {
                            switch (member.MemberType.Value)
                            {
                                case OsmGeoType.Node:
                                    if (_nodesToCache == null)
                                    {
                                        _nodesToCache = new HashSet<long>();
                                    }
                                    _nodesToCache.Add(member.MemberId.Value);
                                    break;
                                case OsmGeoType.Way:
                                    if (_waysToCache == null)
                                    {
                                        _waysToCache = new HashSet<long>();
                                    }
                                    _waysToCache.Add(member.MemberId.Value);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a pre-processor if needed.
        /// </summary>
        /// <returns></returns>
        public virtual IPreProcessor GetPreprocessor()
        {
            return null;
        }

        /// <summary>
        /// Registers the source for this target.
        /// </summary>
        /// <param name="source"></param>
        public override void RegisterSource(OsmStreamSource source)
        {
            // add filter to remove all irrelevant tags.
            OsmStreamFilterTagsFilter tagsFilter = new OsmStreamFilterTagsFilter((TagsCollectionBase tags) =>
            {
                List<Tag> tagsToRemove = new List<Tag>();
                foreach (Tag tag in tags)
                {
                    if (!_interpreter.IsRelevant(tag.Key, tag.Value))
                    {
                        tagsToRemove.Add(tag);
                    }
                }
                foreach (Tag tag in tagsToRemove)
                {
                    tags.RemoveKeyValue(tag.Key, tag.Value);
                }
            });
            tagsFilter.RegisterSource(source);

            base.RegisterSource(tagsFilter);
            //base.RegisterSource(source);
        }

        /// <summary>
        /// Called right before pull and right after initialization.
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforePull()
        {
            // do the pull.
            this.DoPull(true, false, false);

            // reset the source.
            this.Source.Reset();

            // move out of pre-index mode.
            _preIndexMode = false;

            return true;
        }

        /// <summary>
        /// Called right after pull.
        /// </summary>
        public override void OnAfterPull()
        {
            base.OnAfterPull();

            // execute pre-processor.
            var preProcessor = this.GetPreprocessor();
            if(preProcessor != null)
            { // there is a pre-processor, trigger execution.
                preProcessor.Start();
            }
        }
    }
}