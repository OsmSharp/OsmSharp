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

using System;
namespace OsmSharp.IO.MemoryMappedFiles
{
    /// <summary>
    /// Abstract representation of a memory mapped file.
    /// </summary>
    public interface IMemoryMappedFile : IDisposable
    {
        /// <summary>
        /// Creates a MemoryMappedViewAccessor that maps to a view of the memory-mapped file, and that has the specified offset and size.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        IMemoryMappedViewAccessor CreateViewAccessor(long offset, long size);

        /// <summary>
        /// Returns the size of the structure represented by T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        long GetSizeOf<T>() where T : struct;
    }
}