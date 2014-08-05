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

using Ionic.Zlib;
using OsmSharp.IO;
using OsmSharp.Math.Geo;
using OsmSharp.Math.Geo.Simple;
using OsmSharp.Osm.Tiles;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Routing.Graph.Serialization;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;

namespace OsmSharp.Routing.Osm.Graphs.Serialization
{
    /// <summary>
    /// A v2 routing serializer.
    /// </summary>
    /// <remarks>Versioning is implemented in the file format to guarantee backward compatibility.</remarks>
    public class RoutingDataSourceLiveEdgeSerializer : RoutingDataSourceSerializer<LiveEdge>
    {
        /// <summary>
        /// Holds the size of the tile meta.
        /// </summary>
        private const int TileMetaSize = 2 * 4 + 2 * 8;

        /// <summary>
        /// Holds the zoom.
        /// </summary>
        private const int Zoom = 13;

        /// <summary>
        /// Holds the compression flag.
        /// </summary>
        private readonly bool _compress;

        /// <summary>
        /// Holds the runtime type model.
        /// </summary>
        private readonly RuntimeTypeModel _runtimeTypeModel;

        /// <summary>
        /// Creates a new v2 serializer.
        /// </summary>
        /// <param name="compress">Flag telling this serializer to compress it's data.</param>
        public RoutingDataSourceLiveEdgeSerializer(bool compress)
        {
            _compress = compress;

            RuntimeTypeModel typeModel = TypeModel.Create();
            typeModel.Add(typeof(SerializableGraphTileMetas), true); // the tile metadata.
            typeModel.Add(typeof(SerializableGraphTile), true); // one tile of data.
            typeModel.Add(typeof(SerializableTags), true); // a list of tags.
            typeModel.Add(typeof(SerializableGraphArcs), true); // a list of arcs.
            typeModel.Add(typeof(GeoCoordinateSimple), true); // a list of arcs.

            _runtimeTypeModel = typeModel;
        }

        /// <summary>
        /// Returns the version string.
        /// </summary>
        public override string VersionString
        {
            get { return "LiveEdge.v1"; }
        }

        /// <summary>
        /// Does the v1 serialization.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="graph"></param>
        /// <returns></returns>
        protected override void DoSerialize(LimitedStream stream,
            DynamicGraphRouterDataSource<LiveEdge> graph)
        {
            // create an index per tile.
            var dataPerTile = new Dictionary<Tile, UnserializedTileData>();
            for (uint vertex = 1; vertex < graph.VertexCount + 1; vertex++)
            { // loop over all vertices and serialize all into the correct tile.
                float latitude, longitude;
                if (graph.GetVertex(vertex, out latitude, out longitude))
                { // the vertex was found.
                    // build the correct tile.
                    var tile = Tile.CreateAroundLocation(new GeoCoordinate(latitude, longitude), Zoom);
                    UnserializedTileData serializableGraphTile;
                    if (!dataPerTile.TryGetValue(tile, out serializableGraphTile))
                    { // create the new tile.
                        serializableGraphTile = new UnserializedTileData();
                        serializableGraphTile.Ids = new List<uint>();
                        serializableGraphTile.Latitude = new List<ushort>();
                        serializableGraphTile.Longitude = new List<ushort>();
                        serializableGraphTile.StringTable = new Dictionary<string, int>();
                        serializableGraphTile.Arcs = new List<SerializableGraphArcs>();

                        dataPerTile.Add(tile, serializableGraphTile);
                    }

                    // create short latitude/longitude.
                    serializableGraphTile.Ids.Add(vertex);
                    serializableGraphTile.Latitude.Add((ushort)(((tile.TopLeft.Latitude - latitude)
                        / tile.Box.DeltaLat) * ushort.MaxValue));
                    serializableGraphTile.Longitude.Add((ushort)(((longitude - tile.TopLeft.Longitude)
                        / tile.Box.DeltaLon) * ushort.MaxValue));

                    // get the arcs.
                    var arcs = graph.GetEdges(vertex);

                    // serialize the arcs.
                    if (arcs != null && arcs.Length > 0)
                    {
                        var serializableGraphArcs = new SerializableGraphArcs();
                        serializableGraphArcs.DestinationId = new uint[arcs.Length];
                        serializableGraphArcs.Forward = new bool[arcs.Length];
                        serializableGraphArcs.TileX = new int[arcs.Length];
                        serializableGraphArcs.TileY = new int[arcs.Length];
                        serializableGraphArcs.Tags = new SerializableTags[arcs.Length];
                        serializableGraphArcs.Intermediates = new SerializableCoordinates[arcs.Length];
                        serializableGraphArcs.Distances = new float[arcs.Length];

                        for (int idx = 0; idx < arcs.Length; idx++)
                        {
                            var arc = arcs[idx];
                            // get destination tile.
                            if (graph.GetVertex(arc.Key, out latitude, out longitude))
                            { // the destionation was found.
                                GeoCoordinateSimple[] arcValueCoordinates;
                                if(!graph.GetEdgeShape(vertex, arc.Key, out arcValueCoordinates))
                                {
                                    arcValueCoordinates = null;
                                }
                                var destinationTile = Tile.CreateAroundLocation(new GeoCoordinate(latitude, longitude), Zoom);
                                serializableGraphArcs.DestinationId[idx] = arc.Key;
                                serializableGraphArcs.TileX[idx] = destinationTile.X;
                                serializableGraphArcs.TileY[idx] = destinationTile.Y;
                                serializableGraphArcs.Forward[idx] = arc.Value.Forward;
                                serializableGraphArcs.Intermediates[idx] = new SerializableCoordinates() {
                                    Coordinates = SerializableCoordinate.FromSimpleArray(arcValueCoordinates)
                                };
                                serializableGraphArcs.Distances[idx] = arc.Value.Distance;

                                // get the tags.
                                var tagsCollection = graph.TagsIndex.Get(arc.Value.Tags);
                                if (tagsCollection != null)
                                {
                                    serializableGraphArcs.Tags[idx] = new SerializableTags();
                                    serializableGraphArcs.Tags[idx].Keys = new int[tagsCollection.Count];
                                    serializableGraphArcs.Tags[idx].Values = new int[tagsCollection.Count];
                                    int tagsIndex = 0;
                                    foreach (var tag in tagsCollection)
                                    {
                                        int key;
                                        if (!serializableGraphTile.StringTable.TryGetValue(
                                            tag.Key, out key))
                                        { // string not yet in string table.
                                            key = serializableGraphTile.StringTable.Count;
                                            serializableGraphTile.StringTable.Add(tag.Key,
                                                key);
                                        }
                                        int value;
                                        if (!serializableGraphTile.StringTable.TryGetValue(
                                            tag.Value, out value))
                                        { // string not yet in string table.
                                            value = serializableGraphTile.StringTable.Count;
                                            serializableGraphTile.StringTable.Add(tag.Value,
                                                value);
                                        }
                                        serializableGraphArcs.Tags[idx].Keys[tagsIndex] = key;
                                        serializableGraphArcs.Tags[idx].Values[tagsIndex] = value;
                                        tagsIndex++;
                                    }
                                }
                            }
                        }

                        serializableGraphTile.Arcs.Add(serializableGraphArcs);
                    }
                }
            }

            // LAYOUT OF V2: {HEADER}{compressionflag(1byte)}{#tiles(4byte)}{tilesMetaEnd(8byte)}{tiles-meta-data-xxxxxxx}{tiles-data}
            // {HEADER} : already written before this method.
            // {#tiles(4byte)} : the number of tiles in this file (calculate the offset of the {tiles-data} 
            //                   section using (TileMetaSize * dataPerTile.Count + 4 + 8)
            // {tilesMetaEnd(8byte)} : the end of the meta tiles.
            // {tiles-meta-data-xxxxxxx} : the serialized tile metadata.
            // {tiles-data} : the actual tile data.

            // calculate the space needed for the tile offset.
            const long tileMetaOffset = 1 + 4 + 8;
            long tileOffset = TileMetaSize * dataPerTile.Count +
                tileMetaOffset; // all tile metadata + a tile count + tags offset.

            // build the tile metadata while writing the tile data.
            stream.Seek(tileOffset, SeekOrigin.Begin); 
            var metas = new SerializableGraphTileMetas();
            metas.Length = new int[dataPerTile.Count];
            metas.Offset = new long[dataPerTile.Count];
            metas.TileX = new int[dataPerTile.Count];
            metas.TileY = new int[dataPerTile.Count];
            int metasIndex = 0;
            foreach (var unserializedTileData in dataPerTile)
            {                
                // create the tile meta.
                metas.TileX[metasIndex] = unserializedTileData.Key.X;
                metas.TileY[metasIndex] = unserializedTileData.Key.Y;
                metas.Offset[metasIndex] = stream.Position;

                // create the tile.
                var serializableGraphTile = new SerializableGraphTile();
                serializableGraphTile.Arcs = unserializedTileData.Value.Arcs.ToArray();
                serializableGraphTile.Ids = unserializedTileData.Value.Ids.ToArray();
                serializableGraphTile.Latitude = unserializedTileData.Value.Latitude.ToArray();
                serializableGraphTile.Longitude = unserializedTileData.Value.Longitude.ToArray();
                serializableGraphTile.StringTable = new string[unserializedTileData.Value.StringTable.Count];
                foreach (var stringEntry in unserializedTileData.Value.StringTable)
                {
                    serializableGraphTile.StringTable[stringEntry.Value] =
                        stringEntry.Key;
                }

                // serialize the tile.
                if (!_compress)
                { // compresses the file.
                    _runtimeTypeModel.Serialize(stream, serializableGraphTile);
                }
                else 
                { // first compress the data, then write.
                    var uncompressed = new MemoryStream();
                    _runtimeTypeModel.Serialize(uncompressed, serializableGraphTile);
                    var uncompressedBuffer = uncompressed.ToArray();

                    byte[] compressed = GZipStream.CompressBuffer(uncompressedBuffer);
                    stream.Write(compressed, 0, compressed.Length);
                }

                // calculate the length of the data that was just serialized.
                metas.Length[metasIndex] = (int)(stream.Position - metas.Offset[metasIndex]);

                metasIndex++;
            }

            // serialize all tile meta data.
            stream.Seek(tileMetaOffset, SeekOrigin.Begin);
            _runtimeTypeModel.Serialize(stream, metas);
            long tileMetaEnd = stream.Position; // save the meta and.

            // save all the offsets.
            stream.Seek(0, SeekOrigin.Begin);
            byte[] compressionFlag = new[] { (byte)(_compress ? 1 : 0) };
            stream.Write(compressionFlag, 0, 1);
            byte[] tileCountBytes = BitConverter.GetBytes(metas.TileX.Length);
            stream.Write(tileCountBytes, 0, tileCountBytes.Length); // 4 bytes
            byte[] tileMetaEndBytes = BitConverter.GetBytes(tileMetaEnd);
            stream.Write(tileMetaEndBytes, 0, tileMetaEndBytes.Length); // 8 bytes

            stream.Flush();
        }

        /// <summary>
        /// Does the v1 deserialization.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="lazy"></param>
        /// <param name="vehicles"></param>
        /// <returns></returns>
        protected override IBasicRouterDataSource<LiveEdge> DoDeserialize(
            LimitedStream stream, bool lazy, IEnumerable<string> vehicles)
        {
            // serialize all tile meta data.
            stream.Seek(0, SeekOrigin.Begin);

            var compressionFlag = new byte[1];
            stream.Read(compressionFlag, 0, 1);
            bool decompress = (compressionFlag[0] == (byte) 1);

            var tileCountBytes = new byte[4];
            stream.Read(tileCountBytes, 0, tileCountBytes.Length);

            var tileMetaEndBytes = new byte[8];
            stream.Read(tileMetaEndBytes, 0, tileMetaEndBytes.Length);
            var tileMetaEnd = BitConverter.ToInt64(tileMetaEndBytes, 0);

            // deserialize meta data.
            var metas = (SerializableGraphTileMetas)_runtimeTypeModel.Deserialize(
                new CappedStream(stream, stream.Position, tileMetaEnd - stream.Position), null,
                    typeof(SerializableGraphTileMetas));

            // create the datasource.
            var routerDataSource = new RouterLiveEdgeDataSource(stream, decompress, metas, Zoom,
                    this, vehicles, 1000);
            if (!lazy)
            {
                // pre-load everything.
                for (int tileIdx = 0; tileIdx < metas.TileX.Length; tileIdx++)
                {
                    routerDataSource.LoadMissingTile(
                        new Tile(metas.TileX[tileIdx], metas.TileY[tileIdx], Zoom));
                }
            }

            // return router datasource.
            return routerDataSource;
        }

        /// <summary>
        /// Deserialize the given tile data.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="decompress"></param>
        /// <returns></returns>
        internal SerializableGraphTile DeserializeTile(Stream stream, long offset, int length, bool decompress)
        {
            if (decompress)
            { // decompress the data.
                var buffer = new byte[length];
                stream.Seek(offset, SeekOrigin.Begin);
                stream.Read(buffer, 0, length);

                var memoryStream = new MemoryStream(buffer);
                var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
                return (SerializableGraphTile) _runtimeTypeModel.Deserialize(gZipStream
                                                                             , null,
                                                                             typeof (SerializableGraphTile));
            }
            return (SerializableGraphTile)_runtimeTypeModel.Deserialize(
                new CappedStream(stream, offset, length), null,
                    typeof(SerializableGraphTile));
        }

        private class UnserializedTileData
        {
            /// <summary>
            /// Gets/sets the ids.
            /// </summary>
            public List<uint> Ids { get; set; }

            /// <summary>
            /// Gets/sets the offset of the latitude relative to the upper-right corner.
            /// </summary>
            public List<ushort> Latitude { get; set; }

            /// <summary>
            /// Gets/sets the offset of the longitude relative to the upper-right corner.
            /// </summary>
            public List<ushort> Longitude { get; set; }

            /// <summary>
            /// Gets/sets the stringtable.
            /// </summary>
            public Dictionary<string, int> StringTable { get; set; }

            /// <summary>
            /// Gets/sets the vertices.
            /// </summary>
            public List<SerializableGraphArcs> Arcs { get; set; }
        }

        #region Serializable Classes

        /// <summary>
        /// Serializable object containing all metadata tiles.
        /// </summary>
        [ProtoContract]
        internal class SerializableGraphTileMetas
        {
            /// <summary>
            /// The tile x-coordinates.
            /// </summary>
            [ProtoMember(1)]
            public int[] TileX { get; set; }

            /// <summary>
            /// The tile y-coordinates.
            /// </summary>
            [ProtoMember(2)]
            public int[] TileY { get; set; }

            /// <summary>
            /// The tile offsets.
            /// </summary>
            [ProtoMember(3)]
            public long[] Offset { get; set; }

            /// <summary>
            /// The tile lengths.
            /// </summary>
            [ProtoMember(4)]
            public int[] Length { get; set; }
        }

        /// <summary>
        /// Serializable object containing all data in a dynamic graph in one tile.
        /// </summary>
        [ProtoContract]
        internal class SerializableGraphTile
        {
            /// <summary>
            /// Gets/sets the ids.
            /// </summary>
            [ProtoMember(1)]
            public uint[] Ids { get; set; }

            /// <summary>
            /// Gets/sets the offset of the latitude relative to the upper-right corner.
            /// </summary>
            [ProtoMember(2)]
            public ushort[] Latitude { get; set; }

            /// <summary>
            /// Gets/sets the offset of the longitude relative to the upper-right corner.
            /// </summary>
            [ProtoMember(3)]
            public ushort[] Longitude { get; set; }

            /// <summary>
            /// Gets/sets the stringtable.
            /// </summary>
            [ProtoMember(4)]
            public string[] StringTable { get; set; }

            /// <summary>
            /// Gets/sets the vertices.
            /// </summary>
            [ProtoMember(5)]
            public SerializableGraphArcs[] Arcs { get; set; }
        }

        /// <summary>
        /// Serializable object containt all data about one arc.
        /// </summary>
        [ProtoContract]
        internal class SerializableGraphArcs
        {
            /// <summary>
            /// Gets/sets the destination id.
            /// </summary>
            [ProtoMember(1)]
            public uint[] DestinationId { get; set; }

            /// <summary>
            /// Gets/sets the tile x-coordinate.
            /// </summary>
            [ProtoMember(2)]
            public int[] TileX { get; set; }

            /// <summary>
            /// Gets/sets the tile y-coordinate.
            /// </summary>
            [ProtoMember(3)]
            public int[] TileY { get; set; }

            /// <summary>
            /// Gets/sets the forward flag.
            /// </summary>
            [ProtoMember(4)]
            public bool[] Forward { get; set; }

            /// <summary>
            /// Gets/sets the forward flag.
            /// </summary>
            [ProtoMember(5)]
            public SerializableTags[] Tags { get; set; }

            /// <summary>
            /// Gets/sets the intermediates.
            /// </summary>
            [ProtoMember(6)]
            public SerializableCoordinates[] Intermediates { get; set; }

            /// <summary>
            /// Gets/sets the distances.
            /// </summary>
            [ProtoMember(7)]
            public float[] Distances { get; set; }
        }

        /// <summary>
        /// Serializable coordinates list.
        /// </summary>
        [ProtoContract]
        internal class SerializableCoordinates
        {
            /// <summary>
            /// Gets/sets the intermediates.
            /// </summary>
            [ProtoMember(1)]
            public SerializableCoordinate[] Coordinates { get; set; }
        }

        /// <summary>
        /// Serializable coordinate.
        /// </summary>
        [ProtoContract]
        internal class SerializableCoordinate
        {
            /// <summary>
            /// Gets/sets the latitude.
            /// </summary>
            [ProtoMember(1)]
            public float Latitude { get; set; }

            /// <summary>
            /// Gets/sets the longitude.
            /// </summary>
            [ProtoMember(2)]
            public float Longitude { get; set; }

            /// <summary>
            /// Returns a simple array.
            /// </summary>
            /// <param name="coordinates"></param>
            /// <returns></returns>
            public static GeoCoordinateSimple[] ToSimpleArray(SerializableCoordinate[] coordinates)
            {
                if(coordinates == null)
                {
                    return null;
                }
                var simples = new GeoCoordinateSimple[coordinates.Length];
                for(int idx = 0; idx < coordinates.Length; idx++)
                {
                    simples[idx].Latitude = coordinates[idx].Latitude;
                    simples[idx].Longitude = coordinates[idx].Longitude;
                }
                return simples;
            }

            /// <summary>
            /// Returns a simple array.
            /// </summary>
            /// <param name="simples"></param>
            /// <returns></returns>
            public static SerializableCoordinate[] FromSimpleArray(GeoCoordinateSimple[] simples)
            {
                if(simples == null)
                {
                    return null;
                }
                var coordinates = new SerializableCoordinate[simples.Length];
                for (int idx = 0; idx < simples.Length; idx++)
                {
                    coordinates[idx] = new SerializableCoordinate();
                    coordinates[idx].Latitude = simples[idx].Latitude;
                    coordinates[idx].Longitude = simples[idx].Longitude;
                }
                return coordinates;
            }
        }

        /// <summary>
        /// Serializeable version of a series of tags.
        /// </summary>
        [ProtoContract]
        internal class SerializableTags
        {
            /// <summary>
            /// Holds all the keys.
            /// </summary>
            [ProtoMember(1)]
            public int[] Keys { get; set; }

            /// <summary>
            /// Holds all the values.
            /// </summary>
            [ProtoMember(2)]
            public int[] Values { get; set; }
        }

        #endregion
    }
}