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

using OsmSharp.Math.Geo.Simple;
using OsmSharp.Routing.Graph;

namespace OsmSharp.Routing.Osm.Graphs
{
    /// <summary>
    /// A simple edge containing the orignal OSM-tags and a flag indicating the direction of this edge relative to the 
    /// OSM-direction.
    /// </summary>
    public struct LiveEdge : IDynamicGraphEdgeData
    {
        /// <summary>
        /// Contains a value that represents tagsId and forward flag [forwardFlag (true when zero)][tagsIdx].
        /// </summary>
        private uint _value;

        /// <summary>
        /// Gets/sets the value.
        /// </summary>
        internal uint Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        /// <summary>
        /// Flag indicating if this is a forward or backward edge relative to the tag descriptions.
        /// </summary>
        public bool Forward 
        {
            get
            { // true when first bit is 0.
                return _value % 2 == 0;
            }
            set
            {
                if (_value % 2 == 0)
                { // true already.
                    if (!value) { _value = _value + 1; }
                }
                else
                { // false already.
                    if (value) { _value = _value - 1; }
                }
            }
        }

        /// <summary>
        /// The properties of this edge.
        /// </summary>
        public uint Tags
        {
            get
            {
                return _value / 2;
            }
            set
            {
                if (_value % 2 == 0)
                { // true already.
                    _value = value * 2;
                }
                else
                { // false already.
                    _value = (value * 2) + 1;
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of intermediate coordinates.
        /// </summary>
        public GeoCoordinateSimple[] Coordinates { get; set; }

        /// <summary>
        /// Gets/or sets the total distance of this edge.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// Returns true if this edge represents a neighbour-relation.
        /// </summary>
        public bool RepresentsNeighbourRelations
        {
            get { return true; }
        }

        /// <summary>
        /// Creates the exact reverse of this edge.
        /// </summary>
        /// <returns></returns>
        public IDynamicGraphEdgeData Reverse()
        {
            if (this.Coordinates != null)
            {
                var reverseCoordiantes = new GeoCoordinateSimple[this.Coordinates.Length];
                this.Coordinates.CopyToReverse(reverseCoordiantes, 0);
                return new LiveEdge()
                {
                    Coordinates = reverseCoordiantes,
                    Distance = this.Distance,
                    Forward = !this.Forward,
                    Tags = this.Tags
                };
            }
            return new LiveEdge()
            {
                Coordinates = null,
                Distance = this.Distance,
                Forward = !this.Forward,
                Tags = this.Tags
            };
        }

        /// <summary>
        /// Returns true if the other edge represents the same information than this edge.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IDynamicGraphEdgeData other)
        {
            if(other is LiveEdge)
            { // ok, type is the same.
                var otherLive = (LiveEdge)other;
                if(otherLive._value != this._value)
                { // basic info different.
                    return false;
                }

                // only the coordinates can be different now.
                if (this.Coordinates != null)
                { // both have to contain the same coordinates.
                    if (otherLive.Coordinates == null ||
                        this.Coordinates.Length != otherLive.Coordinates.Length)
                    { // impossible, different number of coordinates.
                        return false;
                    }

                    for (int idx = 0; idx < otherLive.Coordinates.Length; idx++)
                    {
                        if (this.Coordinates[idx].Longitude != otherLive.Coordinates[idx].Longitude ||
                            this.Coordinates[idx].Latitude != otherLive.Coordinates[idx].Latitude)
                        { // oeps, coordinates are different!
                            return false;
                        }
                    }
                    return true;
                }
                else
                { // both are null.
                    return otherLive.Coordinates == null;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the other edge represents the same geographical information than this edge.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool EqualsGeometrically(IDynamicGraphEdgeData other)
        {
            if (other is LiveEdge)
            { // ok, type is the same.
                var otherLive = (LiveEdge)other;

                // only the coordinates can be different now.
                if (this.Coordinates != null)
                { // both have to contain the same coordinates.
                    if (this.Coordinates.Length != otherLive.Coordinates.Length)
                    { // impossible, different number of coordinates.
                        return false;
                    }

                    for (int idx = 0; idx < otherLive.Coordinates.Length; idx++)
                    {
                        if (this.Coordinates[idx].Longitude != otherLive.Coordinates[idx].Longitude ||
                            this.Coordinates[idx].Latitude != otherLive.Coordinates[idx].Latitude)
                        { // oeps, coordinates are different!
                            return false;
                        }
                    }
                    return true;
                }
                else
                { // both are null.
                    return otherLive.Coordinates == null;
                }
            }
            return false;
        }
    }
}
