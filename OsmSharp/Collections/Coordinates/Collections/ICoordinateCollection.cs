﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2014 Abelshausen Ben
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
using System;
using System.Collections.Generic;

namespace OsmSharp.Collections.Coordinates.Collections
{
    /// <summary>
    /// Represents a collection of coordinates.
    /// </summary>
    public interface ICoordinateCollection : IEnumerable<ICoordinate>, IEnumerator<ICoordinate>, ICoordinate
    {
        /// <summary>
        /// Returns the count.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Returns the reverse collection.
        /// </summary>
        /// <returns></returns>
        ICoordinateCollection Reverse();
    }

    /// <summary>
    /// Represents a coordinate.
    /// </summary>
    public interface ICoordinate
    {
        /// <summary>
        /// Holds the latitude.
        /// </summary>
        float Latitude { get; }

        /// <summary>
        /// Holds the longitude.
        /// </summary>
        float Longitude { get; }
    }

    /// <summary>
    /// Holds extension methods for the ICoordinateCollection.
    /// </summary>
    public static class CoordinateCollectionExtentions
    {
        /// <summary>
        /// Returns the simple array.
        /// </summary>
        /// <returns></returns>
        public static GeoCoordinateSimple[] ToSimpleArray(this ICoordinateCollection collection)
        {
            if(collection == null)
            {
                return null;
            }
            var array = new GeoCoordinateSimple[collection.Count];
            int idx = 0;
            collection.Reset();
            while (collection.MoveNext())
            {
                array[idx] = new GeoCoordinateSimple()
                {
                    Latitude = collection.Latitude,
                    Longitude = collection.Longitude
                };
                idx++;
            }
            return array;
        }
    }

    /// <summary>
    /// A wrapper for a ICoordinate array.
    /// </summary>
    public class CoordinateArrayCollection<CoordinateType> : ICoordinateCollection
        where CoordinateType : ICoordinate
    {
        /// <summary>
        /// Holds the coordinate array.
        /// </summary>
        private CoordinateType[] _coordinateArray;

        /// <summary>
        /// Holds the reverse flag.
        /// </summary>
        private bool _reverse = false;

        /// <summary>
        /// Creates a new ICoordinate array wrapper.
        /// </summary>
        /// <param name="coordinateArray"></param>
        public CoordinateArrayCollection(CoordinateType[] coordinateArray)
        {
            if (coordinateArray == null) { throw new ArgumentNullException("coordinateArray"); }

            _coordinateArray = coordinateArray;
            _reverse = false;
        }
        
        /// <summary>
        /// Creates a new ICoordinate array wrapper.
        /// </summary>
        /// <param name="coordinateArray"></param>
        /// <param name="reverse"></param>
        public CoordinateArrayCollection(CoordinateType[] coordinateArray, bool reverse)
        {
            if (coordinateArray == null) { throw new ArgumentNullException("coordinateArray"); }

            _coordinateArray = coordinateArray;
            _reverse = reverse;
        }

        /// <summary>
        /// Rests this collection an reuses it.
        /// </summary>
        /// <param name="coordinateArray"></param>
        public void ResetFor(CoordinateType[] coordinateArray)
        {
            _coordinateArray = coordinateArray;
            this.Reset();
        }

        /// <summary>
        /// Returns the count.
        /// </summary>
        public int Count
        {
            get { return _coordinateArray.Length; }
        }

        /// <summary>
        /// Returns the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<ICoordinate> GetEnumerator()
        {
            return this;
        }

        /// <summary>
        /// Returns the enumerator.
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        /// <summary>
        /// Holds the current idx.
        /// </summary>
        private int _currentIdx;

        /// <summary>
        /// Returns the current coordinate.
        /// </summary>
        public ICoordinate Current
        {
            get 
            {
                var current = _coordinateArray[_currentIdx];
                return new GeoCoordinateSimple() {
                    Latitude = current.Latitude,
                    Longitude = current.Longitude
                };
            }
        }

        /// <summary>
        /// Returns the current coordinate.
        /// </summary>
        object System.Collections.IEnumerator.Current
        {
            get
            {
                var current = _coordinateArray[_currentIdx];
                return new GeoCoordinateSimple()
                {
                    Latitude = current.Latitude,
                    Longitude = current.Longitude
                };
            }
        }

        /// <summary>
        /// Move to the next coordinate.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            _currentIdx++;
            return _currentIdx < this.Count;
        }

        /// <summary>
        /// Resets this enumerator.
        /// </summary>
        public void Reset()
        {
            _currentIdx = -1;
        }

        /// <summary>
        /// Disposes of all resources associated with this enumerator.
        /// </summary>
        public void Dispose()
        {

        }

        /// <summary>
        /// Returns the current latitude.
        /// </summary>
        public float Latitude
        {
            get { return _coordinateArray[_currentIdx].Latitude; }
        }

        /// <summary>
        /// Returns the current longitude.
        /// </summary>
        public float Longitude
        {
            get { return _coordinateArray[_currentIdx].Longitude; }
        }

        /// <summary>
        /// Returns the reverse collection.
        /// </summary>
        /// <returns></returns>
        public ICoordinateCollection Reverse()
        {
            return new CoordinateArrayCollection<CoordinateType>(_coordinateArray, !_reverse);
        }
    }
}