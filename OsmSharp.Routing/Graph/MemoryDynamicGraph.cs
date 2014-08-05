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

using OsmSharp.Collections.Arrays;
using OsmSharp.Math.Geo.Simple;
using System;
using System.Collections.Generic;

namespace OsmSharp.Routing.Graph
{
    /// <summary>
    /// An implementation of an in-memory dynamic graph.
    /// </summary>
    public class MemoryDynamicGraph<TEdgeData> : IDynamicGraph<TEdgeData>
        where TEdgeData : IDynamicGraphEdgeData
    {
        private const int EDGE_SIZE = 4;
        private const uint NO_EDGE = uint.MaxValue;
        private const int NODEA = 0;
        private const int NODEB = 1;
        private const int NEXTNODEA = 2;
        private const int NEXTNODEB = 3;

        /// <summary>
        /// Holds the next id.
        /// </summary>
        private uint _nextVertexId;

        /// <summary>
        /// Holds the next edge id.
        /// </summary>
        private uint _nextEdgeId;

        /// <summary>
        /// Holds the coordinates of the vertices.
        /// </summary>
        private IHugeArray<GeoCoordinateSimple> _coordinates;

        /// <summary>
        /// Holds all vertices pointing to it's first edge.
        /// </summary>
        private IHugeArray<uint> _vertices;

        /// <summary>
        /// Holds all edges (meaning vertex1-vertex2)
        /// </summary>
        private IHugeArray<uint> _edges;

        /// <summary>
        /// Holds all data associated with edges.
        /// </summary>
        private IHugeArray<TEdgeData> _edgeData;

        /// <summary>
        /// Holds all shapes associated with edges.
        /// </summary>
        private IHugeArray<GeoCoordinateSimple[]> _edgeShapes;

        /// <summary>
        /// Creates a new in-memory graph.
        /// </summary>
        public MemoryDynamicGraph()
            : this(1000)
        {

        }

        /// <summary>
        /// Creates a new in-memory graph.
        /// </summary>
        public MemoryDynamicGraph(long sizeEstimate)
            : this(sizeEstimate, new HugeArray<GeoCoordinateSimple>(sizeEstimate), new HugeArray<uint>(sizeEstimate), new HugeArray<uint>(sizeEstimate * 3 * EDGE_SIZE), new HugeArray<TEdgeData>(sizeEstimate * 3), new HugeArray<GeoCoordinateSimple[]>(sizeEstimate * 3))
        {

        }

        /// <summary>
        /// Creates a new in-memory graph.
        /// </summary>
        /// <param name="sizeEstimate"></param>
        /// <param name="coordinateArray"></param>
        /// <param name="vertexArray"></param>
        /// <param name="edgesArray"></param>
        /// <param name="edgeDataArray"></param>
        /// <param name="edgeShapeArray"></param>
        protected MemoryDynamicGraph(long sizeEstimate, IHugeArray<GeoCoordinateSimple> coordinateArray, IHugeArray<uint> vertexArray, IHugeArray<uint> edgesArray, IHugeArray<TEdgeData> edgeDataArray, IHugeArray<GeoCoordinateSimple[]> edgeShapeArray)
        {
            _nextVertexId = 1;
            _nextEdgeId = 0;
            _vertices = vertexArray;
            _vertices.Resize(sizeEstimate);
            for (int idx = 0; idx < sizeEstimate; idx++)
            {
                _vertices[idx] = NO_EDGE;
            }
            _coordinates = coordinateArray;
            _coordinates.Resize(sizeEstimate);
            _edges = edgesArray;
            _edges.Resize(sizeEstimate * 3 * EDGE_SIZE);
            for (int idx = 0; idx < sizeEstimate * 3 * EDGE_SIZE; idx++)
            {
                _edges[idx] = NO_EDGE;
            }
            _edgeData = edgeDataArray;
            _edgeData.Resize(sizeEstimate * 3);
            _edgeShapes = edgeShapeArray;
            _edgeShapes.Resize(sizeEstimate * 3);
        }

        /// <summary>
        /// Increases the memory allocation.
        /// </summary>
        private void IncreaseVertexSize()
        {
            this.IncreaseVertexSize(_coordinates.Length + 10000);
        }

        /// <summary>
        /// Increases the memory allocation.
        /// </summary>
        /// <param name="size"></param>
        private void IncreaseVertexSize(long size)
        {
            var oldLength = _coordinates.Length;
            _coordinates.Resize(size);
            _vertices.Resize(size);
            for (long idx = oldLength; idx < size; idx++)
            {
                _vertices[idx] = NO_EDGE;
            }
        }

        /// <summary>
        /// Increases the memory allocation.
        /// </summary>
        private void IncreaseEdgeSize()
        {
            this.IncreaseEdgeSize(_edges.Length + 10000);
        }

        /// <summary>
        /// Increases the memory allocation.
        /// </summary>
        private void IncreaseEdgeSize(long size)
        {
            var oldLength = _edges.Length;
            _edges.Resize(size);
            for (long idx = oldLength; idx < size; idx++)
            {
                _edges[idx] = NO_EDGE;
            }
            _edgeData.Resize(size / EDGE_SIZE);
            _edgeShapes.Resize(size / EDGE_SIZE);
        }

        /// <summary>
        /// Adds a new vertex.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public uint AddVertex(float latitude, float longitude)
        {
            // make sure vertices array is large enough.
            if (_nextVertexId >= _vertices.Length)
            {
                this.IncreaseVertexSize();
            }

            // create vertex.
            uint newId = _nextVertexId;
            _coordinates[newId] = new GeoCoordinateSimple()
            {
                Latitude = latitude,
                Longitude = longitude
            };
            _nextVertexId++; // increase for next vertex.
            return newId;
        }

        /// <summary>
        /// Sets a vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        public void SetVertex(uint vertex, float latitude, float longitude)
        {
            if (_nextVertexId <= vertex) { throw new ArgumentOutOfRangeException("vertex", "vertex is not part of this graph."); }

            var coordinate = _coordinates[vertex];
            coordinate.Latitude = latitude;
            coordinate.Longitude = longitude;
            _coordinates[vertex] = coordinate;
        }

        /// <summary>
        /// Returns the information in the current vertex.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public bool GetVertex(uint id, out float latitude, out float longitude)
        {
            if (_nextVertexId > id)
            {
                latitude = _coordinates[id].Latitude;
                longitude = _coordinates[id].Longitude;
                return true;
            }
            latitude = float.MaxValue;
            longitude = float.MaxValue;
            return false;
        }

        /// <summary>
        /// Adds an edge with the associated data.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="data"></param>
        public void AddEdge(uint vertex1, uint vertex2, TEdgeData data)
        {
            this.AddEdge(vertex1, vertex2, data, null);
        }

        /// <summary>
        /// Adds an edge with the associated data.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="data"></param>
        /// <param name="coordinates"></param>
        public void AddEdge(uint vertex1, uint vertex2, TEdgeData data, GeoCoordinateSimple[] coordinates)
        {
            this.AddEdge(vertex1, vertex2, data, coordinates, null);
        }

        /// <summary>
        /// Adds an edge with the associated data.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="data"></param>
        /// <param name="coordinates"></param>
        /// <param name="comparer">Comparator to compare edges and replace obsolete ones.</param>
        public void AddEdge(uint vertex1, uint vertex2, TEdgeData data, GeoCoordinateSimple[] coordinates, IDynamicGraphEdgeComparer<TEdgeData> comparer)
        {
            // if (!data.Forward) { throw new ArgumentOutOfRangeException("data", "Edge data has to be forward."); }

            if (vertex1 == vertex2) { throw new ArgumentException("Given vertices must be different."); }
            if (_nextVertexId <= vertex1) { throw new ArgumentOutOfRangeException("vertex1", "vertex1 is not part of this graph."); }
            if (_nextVertexId <= vertex2) { throw new ArgumentOutOfRangeException("vertex2", "vertex2 is not part of this graph."); }

            var edgeId = _vertices[vertex1];
            if (_vertices[vertex1] != NO_EDGE)
            { // check for an existing edge first.
                // check if the arc exists already.
                edgeId = _vertices[vertex1];
                uint nextEdgeSlot = 0;
                while (edgeId != NO_EDGE)
                { // keep looping.
                    uint otherVertexId = 0;
                    uint previousEdgeId = edgeId;
                    bool forward = true;
                    if (_edges[edgeId + NODEA] == vertex1)
                    {
                        otherVertexId = _edges[edgeId + NODEB];
                        nextEdgeSlot = edgeId + NEXTNODEA;
                        edgeId = _edges[edgeId + NEXTNODEA];
                    }
                    else
                    {
                        otherVertexId = _edges[edgeId + NODEA];
                        nextEdgeSlot = edgeId + NEXTNODEB;
                        edgeId = _edges[edgeId + NEXTNODEB];
                        forward = false;
                    }
                    if (otherVertexId == vertex2)
                    { // this is the edge we need.
                        if (!forward)
                        {
                            data = (TEdgeData)data.Reverse();
                        }
                        if (comparer != null)
                        { // there is a comparer.
                            var existingData = _edgeData[previousEdgeId / 4];
                            if (comparer.Overlaps(data, existingData))
                            { // an arc was found that represents the same directional information.
                                _edgeData[previousEdgeId / 4] = data;
                                _edgeShapes[previousEdgeId / 4] = coordinates;
                            }
                            return;
                        }
                        _edgeData[previousEdgeId / 4] = data;
                        _edgeShapes[previousEdgeId / 4] = coordinates;
                        return;
                    }
                }

                // create a new edge.
                edgeId = _nextEdgeId;
                if (_nextEdgeId + NEXTNODEB >= _edges.Length)
                { // there is a need to increase edges array.
                    this.IncreaseEdgeSize();
                }
                _edges[_nextEdgeId + NODEA] = vertex1;
                _edges[_nextEdgeId + NODEB] = vertex2;
                _edges[_nextEdgeId + NEXTNODEA] = NO_EDGE;
                _edges[_nextEdgeId + NEXTNODEB] = NO_EDGE;
                _nextEdgeId = _nextEdgeId + EDGE_SIZE;

                // append the new edge to the from list.
                _edges[nextEdgeSlot] = edgeId;

                // set data.
                _edgeData[edgeId / 4] = data;
                _edgeShapes[edgeId / 4] = coordinates;
            }
            else
            { // create a new edge and set.
                edgeId = _nextEdgeId;
                _vertices[vertex1] = _nextEdgeId;

                if (_nextEdgeId + NEXTNODEB >= _edges.Length)
                { // there is a need to increase edges array.
                    this.IncreaseEdgeSize();
                }
                _edges[_nextEdgeId + NODEA] = vertex1;
                _edges[_nextEdgeId + NODEB] = vertex2;
                _edges[_nextEdgeId + NEXTNODEA] = NO_EDGE;
                _edges[_nextEdgeId + NEXTNODEB] = NO_EDGE;
                _nextEdgeId = _nextEdgeId + EDGE_SIZE;

                // set data.
                _edgeData[edgeId / 4] = data;
                _edgeShapes[edgeId / 4] = coordinates;
            }

            var toEdgeId = _vertices[vertex2];
            if (toEdgeId != NO_EDGE)
            { // there are existing edges.
                uint nextEdgeSlot = 0;
                while (toEdgeId != NO_EDGE)
                { // keep looping.
                    uint otherVertexId = 0;
                    if (_edges[toEdgeId + NODEA] == vertex2)
                    {
                        otherVertexId = _edges[toEdgeId + NODEB];
                        nextEdgeSlot = toEdgeId + NEXTNODEA;
                        toEdgeId = _edges[toEdgeId + NEXTNODEA];
                    }
                    else
                    {
                        otherVertexId = _edges[toEdgeId + NODEA];
                        nextEdgeSlot = toEdgeId + NEXTNODEB;
                        toEdgeId = _edges[toEdgeId + NEXTNODEB];
                    }
                }
                _edges[nextEdgeSlot] = edgeId;
            }
            else
            { // there are no existing edges point the vertex straight to it's first edge.
                _vertices[vertex2] = edgeId;
            }

            return;
        }

        /// <summary>
        /// Deletes all edges leading from/to the given vertex. 
        /// </summary>
        /// <param name="vertex"></param>
        public void RemoveEdges(uint vertex)
        {
            var edges = this.GetEdges(vertex);

            foreach(var edge in edges)
            {
                this.RemoveEdge(vertex, edge.Key);
            }
        }

        /// <summary>
        /// Deletes the edge between the two given vertices.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        public void RemoveEdge(uint vertex1, uint vertex2)
        {
            if (_nextVertexId <= vertex1) { throw new ArgumentOutOfRangeException("vertex1", "vertex1 is not part of this graph."); }
            if (_nextVertexId <= vertex2) { throw new ArgumentOutOfRangeException("vertex2", "vertex2 is not part of this graph."); }

            if(_vertices[vertex1] == NO_EDGE ||
                _vertices[vertex2] == NO_EDGE)
            { // no edge to remove here!
                return;
            }

            // remove for vertex1.
            var nextEdgeId = _vertices[vertex1];
            uint nextEdgeSlot = 0;
            uint previousEdgeSlot = 0;
            uint currentEdgeId = 0;
            while (nextEdgeId != NO_EDGE)
            { // keep looping.
                uint otherVertexId = 0;
                currentEdgeId = nextEdgeId;
                previousEdgeSlot = nextEdgeSlot;
                if (_edges[nextEdgeId + NODEA] == vertex1)
                {
                    otherVertexId = _edges[nextEdgeId + NODEB];
                    nextEdgeSlot = nextEdgeId + NEXTNODEA;
                    nextEdgeId = _edges[nextEdgeId + NEXTNODEA];
                }
                else
                {
                    otherVertexId = _edges[nextEdgeId + NODEA];
                    nextEdgeSlot = nextEdgeId + NEXTNODEB;
                    nextEdgeId = _edges[nextEdgeId + NEXTNODEB];
                }
                if (otherVertexId == vertex2)
                { // this is the edge we need.
                    if (_vertices[vertex1] == currentEdgeId)
                    { // the edge being remove if the 'first' edge.
                        // point to the next edge.
                        _vertices[vertex1] = nextEdgeId;
                    }
                    else
                    { // the edge being removed is not the 'first' edge.
                        // set the previous edge slot to the current edge id being the next one.
                        _edges[previousEdgeSlot] = nextEdgeId;
                    }
                    break;
                }
            }

            // remove for vertex2.
            nextEdgeId = _vertices[vertex2];
            nextEdgeSlot = 0;
            previousEdgeSlot = 0;
            currentEdgeId = 0;
            while (nextEdgeId != NO_EDGE)
            { // keep looping.
                uint otherVertexId = 0;
                currentEdgeId = nextEdgeId;
                previousEdgeSlot = nextEdgeSlot;
                if (_edges[nextEdgeId + NODEA] == vertex2)
                {
                    otherVertexId = _edges[nextEdgeId + NODEB];
                    nextEdgeSlot = nextEdgeId + NEXTNODEA;
                    nextEdgeId = _edges[nextEdgeId + NEXTNODEA];
                }
                else
                {
                    otherVertexId = _edges[nextEdgeId + NODEA];
                    nextEdgeSlot = nextEdgeId + NEXTNODEB;
                    nextEdgeId = _edges[nextEdgeId + NEXTNODEB];
                }
                if (otherVertexId == vertex1)
                { // this is the edge we need.
                    if (_vertices[vertex2] == currentEdgeId)
                    { // the edge being remove if the 'first' edge.
                        // point to the next edge.
                        _vertices[vertex2] = nextEdgeId;
                    }
                    else
                    { // the edge being removed is not the 'first' edge.
                        // set the previous edge slot to the current edge id being the next one.
                        _edges[previousEdgeSlot] = nextEdgeId;
                    }

                    // reset everything about this edge.
                    _edges[currentEdgeId + NODEA] = NO_EDGE;
                    _edges[currentEdgeId + NODEB] = NO_EDGE;
                    _edges[currentEdgeId + NEXTNODEA] = NO_EDGE;
                    _edges[currentEdgeId + NEXTNODEB] = NO_EDGE;
                    _edgeData[currentEdgeId / EDGE_SIZE] = default(TEdgeData);
                    _edgeShapes[currentEdgeId / EDGE_SIZE] = null;
                    return;
                }
            }
            throw new Exception("Edge could not be reached from vertex2. Data in graph is invalid.");
        }

        /// <summary>
        /// Returns all arcs starting at the given vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public KeyValuePair<uint, TEdgeData>[] GetEdges(uint vertex)
        {
            if (_nextVertexId <= vertex) { throw new ArgumentOutOfRangeException("vertex", "vertex is not part of this graph."); }

            var edgeId = _vertices[vertex];
            if (edgeId == NO_EDGE)
            { // there are no edges.
                return new KeyValuePair<uint, TEdgeData>[0];
            }

            // loop over edges until a NO_EDGE is encountered.
            var edges = new List<KeyValuePair<uint, TEdgeData>>();
            while (edgeId != NO_EDGE)
            { // keep looping.
                if (_edges[edgeId + NODEA] == vertex)
                {
                    var otherVertexId = _edges[edgeId + NODEB];
                    edges.Add(
                        new KeyValuePair<uint, TEdgeData>(otherVertexId, _edgeData[edgeId / 4]));
                    edgeId = _edges[edgeId + NEXTNODEA];
                }
                else
                {
                    var otherVertexId = _edges[edgeId + NODEA];
                    edges.Add(
                        new KeyValuePair<uint, TEdgeData>(otherVertexId, (TEdgeData)_edgeData[edgeId / 4].Reverse()));
                    edgeId = _edges[edgeId + NEXTNODEB];
                }
            }

            return edges.ToArray();
        }

        /// <summary>
        /// Returns true if the given vertex has neighbour as a neighbour.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <returns></returns>
        public bool ContainsEdge(uint vertex1, uint vertex2)
        {
            if (_nextVertexId <= vertex1) { throw new ArgumentOutOfRangeException("vertex1", "vertex1 is not part of this graph."); }
            if (_nextVertexId <= vertex2) { throw new ArgumentOutOfRangeException("vertex2", "vertex2 is not part of this graph."); }

            if (_vertices[vertex1] == NO_EDGE)
            { // no edges here!
                return false;
            }
            var edgeId = _vertices[vertex1];
            uint nextEdgeSlot = 0;
            while (edgeId != NO_EDGE)
            { // keep looping.
                uint otherVertexId = 0;
                if (_edges[edgeId + NODEA] == vertex1)
                {
                    otherVertexId = _edges[edgeId + NODEB];
                    edgeId = _edges[edgeId + NEXTNODEA];
                    nextEdgeSlot = edgeId + NEXTNODEA;
                }
                else
                {
                    otherVertexId = _edges[edgeId + NODEA];
                    edgeId = _edges[edgeId + NEXTNODEB];
                    nextEdgeSlot = edgeId + NEXTNODEB;
                }
                if (otherVertexId == vertex2)
                { // this is the edge we need.
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the index associated with the given edge and return true if it exists.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="edgeDataIdx"></param>
        /// <param name="edgeDataForward"></param>
        /// <returns></returns>
        private bool GetEdgeIdx(uint vertex1, uint vertex2, out long edgeDataIdx, out bool edgeDataForward)
        {
            if (_nextVertexId <= vertex1) { throw new ArgumentOutOfRangeException("vertex1", "vertex1 is not part of this graph."); }
            if (_nextVertexId <= vertex2) { throw new ArgumentOutOfRangeException("vertex2", "vertex2 is not part of this graph."); }

            if (_vertices[vertex1] == NO_EDGE)
            { // no edges here!
                edgeDataIdx = -1;
                edgeDataForward = false;
                return false;
            }
            var edgeId = _vertices[vertex1];
            uint nextEdgeSlot = 0;
            while (edgeId != NO_EDGE)
            { // keep looping.
                uint otherVertexId = 0;
                var currentEdgeId = edgeId;
                edgeDataForward = true;
                if (_edges[edgeId + NODEA] == vertex1)
                {
                    otherVertexId = _edges[edgeId + NODEB];
                    edgeId = _edges[edgeId + NEXTNODEA];
                    nextEdgeSlot = edgeId + NEXTNODEA;
                }
                else
                {
                    otherVertexId = _edges[edgeId + NODEA];
                    edgeId = _edges[edgeId + NEXTNODEB];
                    nextEdgeSlot = edgeId + NEXTNODEB;
                    edgeDataForward = false;
                }
                if (otherVertexId == vertex2)
                { // this is the edge we need.
                    edgeDataIdx = currentEdgeId / EDGE_SIZE;
                    return true;
                }
            }
            edgeDataForward = false;
            edgeDataIdx = -1;
            return false;
        }

        /// <summary>
        /// Gets the data associated with the given edge and return true if it exists.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool GetEdge(uint vertex1, uint vertex2, out TEdgeData data)
        {
            long edgeDataIdx;
            bool edgeDataForward;
            if(this.GetEdgeIdx(vertex1, vertex2, out edgeDataIdx, out edgeDataForward))
            { // the edge exists.
                data = _edgeData[edgeDataIdx];
                if(!edgeDataForward)
                { // edge is backward.
                    data = (TEdgeData)data.Reverse();
                }
                return true;
            }
            data = default(TEdgeData);
            return false;
        }

        /// <summary>
        /// Gets the shape associated with the given edge and returns true if it exists.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="shape"></param>
        /// <returns></returns>
        public bool GetEdgeShape(uint vertex1, uint vertex2, out GeoCoordinateSimple[] shape)
        {
            long edgeDataIdx;
            bool edgeDataForward;
            if (this.GetEdgeIdx(vertex1, vertex2, out edgeDataIdx, out edgeDataForward))
            { // the edge exists.
                shape = _edgeShapes[edgeDataIdx];
                if (!edgeDataForward && shape != null)
                { // edge is backward.
                    var reverse = new GeoCoordinateSimple[shape.Length];
                    for (int idx = 0; idx < shape.Length; idx++)
                    {
                        reverse[idx] = shape[shape.Length - idx - 1];
                    }
                    shape = reverse;
                }
                return true;
            }
            shape = null;
            return false;
        }

        /// <summary>
        /// Trims the internal data structures of this graph.
        /// </summary>
        public void Trim()
        {
            // resize coordinates/vertices.
            _coordinates.Resize(_nextVertexId);
            // Array.Resize<GeoCoordinateSimple>(ref _coordinates, (int)_nextVertexId);
            _vertices.Resize(_nextVertexId);
            // Array.Resize<uint>(ref _vertices, (int)_nextVertexId);
           
            // resize edges.
            _edgeData.Resize(_nextEdgeId / EDGE_SIZE);
            _edgeShapes.Resize(_nextEdgeId / EDGE_SIZE);
            _edges.Resize(_nextEdgeId);
        }

        /// <summary>
        /// Resizes the internal data structures of the graph to handle the number of vertices/edges estimated.
        /// </summary>
        /// <param name="vertexEstimate"></param>
        /// <param name="edgeEstimate"></param>
        public void Resize(long vertexEstimate, long edgeEstimate)
        {
            // resize coordinates/vertices.
            this.IncreaseVertexSize((int)vertexEstimate);

            // resize edges.
            this.IncreaseEdgeSize((int)(edgeEstimate * EDGE_SIZE));
        }

        /// <summary>
        /// Returns the number of vertices in this graph.
        /// </summary>
        public uint VertexCount
        {
            get { return _nextVertexId - 1; }
        }

        /// <summary>
        /// Trims the size of this graph to it's smallest possible size.
        /// </summary>
        public void Compress()
        {
            // trim edges.
            uint maxAllocatedEdgeId = 0;
            for (uint edgeId = 0; edgeId < _nextEdgeId; edgeId = edgeId + EDGE_SIZE)
            {
                if (_edges[edgeId] != NO_EDGE)
                { // this edge is allocated.
                    if (edgeId != maxAllocatedEdgeId)
                    { // there is data here.
                        this.MoveEdge(edgeId, maxAllocatedEdgeId);
                    }
                    maxAllocatedEdgeId = maxAllocatedEdgeId + EDGE_SIZE;
                }
            }
            _nextEdgeId = maxAllocatedEdgeId;

            // trim vertices.
            uint minUnAllocatedVertexId = 0;
            for(uint vertexId = 0; vertexId < _nextVertexId; vertexId++)
            {
                if(_vertices[vertexId] != NO_EDGE)
                {
                    minUnAllocatedVertexId = vertexId;
                }
            }
            _nextVertexId = minUnAllocatedVertexId + 1;
        }

        /// <summary>
        /// Moves an edge from one location to another.
        /// </summary>
        /// <param name="oldEdgeId"></param>
        /// <param name="newEdgeId"></param>
        private void MoveEdge(uint oldEdgeId, uint newEdgeId)
        {
            // first copy the data.
            _edges[newEdgeId + NODEA] = _edges[oldEdgeId + NODEA];
            _edges[newEdgeId + NODEB] = _edges[oldEdgeId + NODEB];
            _edges[newEdgeId + NEXTNODEA] = _edges[oldEdgeId + NEXTNODEA];
            _edges[newEdgeId + NEXTNODEB] = _edges[oldEdgeId + NEXTNODEB];
            _edgeData[newEdgeId / EDGE_SIZE] = _edgeData[oldEdgeId / EDGE_SIZE];

            // loop over all edges of vertex1 and replace the oldEdgeId with the new one.
            uint vertex1 = _edges[oldEdgeId + NODEA];
            var edgeId = _vertices[vertex1];
            if (edgeId == oldEdgeId)
            { // edge is the first one, easy!
                _vertices[vertex1] = newEdgeId;
            }
            else
            { // edge is somewhere in the edges list.
                while (edgeId != NO_EDGE)
                { // keep looping.
                    var edgeIdLocation = edgeId + NEXTNODEB;
                    if (_edges[edgeId + NODEA] == vertex1)
                    { // edge loction is different.
                        edgeIdLocation = edgeId + NEXTNODEA;
                    }
                    edgeId = _edges[edgeIdLocation];
                    if (edgeId == oldEdgeId)
                    {
                        _edges[edgeIdLocation] = newEdgeId;
                        break;
                    }
                }
            }            
            
            // loop over all edges of vertex2 and replace the oldEdgeId with the new one.
            uint vertex2 = _edges[oldEdgeId + NODEB];
            edgeId = _vertices[vertex2];
            if (edgeId == oldEdgeId)
            { // edge is the first one, easy!
                _vertices[vertex2] = newEdgeId;
            }
            else
            { // edge is somewhere in the edges list.
                while (edgeId != NO_EDGE)
                { // keep looping.
                    var edgeIdLocation = edgeId + NEXTNODEB;
                    if (_edges[edgeId + NODEA] == vertex2)
                    { // edge loction is different.
                        edgeIdLocation = edgeId + NEXTNODEA;
                    }
                    edgeId = _edges[edgeIdLocation];
                    if (edgeId == oldEdgeId)
                    {
                        _edges[edgeIdLocation] = newEdgeId;
                        break;
                    }
                }
            }

            // remove the old data.
            _edges[oldEdgeId + NODEA] = NO_EDGE;
            _edges[oldEdgeId + NODEB] = NO_EDGE;
            _edges[oldEdgeId + NEXTNODEA] = NO_EDGE;
            _edges[oldEdgeId + NEXTNODEB] = NO_EDGE;
            _edgeData[oldEdgeId / EDGE_SIZE] = default(TEdgeData);
        }
    }
}