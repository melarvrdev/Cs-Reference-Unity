// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.TerrainUtils
{
    internal enum TerrainMapStatusCode
    {
        OK = 0,
        Overlapping = 1 << 0,
        SizeMismatch = 1 << 2,
        EdgeAlignmentMismatch = 1 << 3,
    }

    public readonly struct TerrainTileCoord
    {
        public readonly int tileX;
        public readonly int tileZ;

        public TerrainTileCoord(int tileX, int tileZ)
        {
            this.tileX = tileX;
            this.tileZ = tileZ;
        }
    }

    public class TerrainMap
    {
        public Terrain GetTerrain(int tileX, int tileZ)
        {
            Terrain result = null;
            m_terrainTiles.TryGetValue(new TerrainTileCoord(tileX, tileZ), out result);
            return result;
        }

        private struct QueueElement
        {
            public readonly int tileX;
            public readonly int tileZ;
            public readonly Terrain terrain;
            public QueueElement(int tileX, int tileZ, Terrain terrain)
            {
                this.tileX = tileX;
                this.tileZ = tileZ;
                this.terrain = terrain;
            }
        }

        static public TerrainMap CreateFromConnectedNeighbors(Terrain originTerrain, System.Predicate<Terrain> filter = null, bool fullValidation = true)
        {
            if (originTerrain == null)
                return null;

            if (originTerrain.terrainData == null)
                return null;

            TerrainMap terrainMap = new TerrainMap();

            Queue<QueueElement> todoQueue = new Queue<QueueElement>();
            todoQueue.Enqueue(new QueueElement(0, 0, originTerrain));

            int maxTerrains = Terrain.activeTerrains.Length;
            while (todoQueue.Count > 0)
            {
                QueueElement cur = todoQueue.Dequeue();
                if ((filter == null) || filter(cur.terrain))
                {
                    if (terrainMap.TryToAddTerrain(cur.tileX, cur.tileZ, cur.terrain))
                    {
                        // sanity check to stop bad neighbors causing infinite iteration
                        if (terrainMap.m_terrainTiles.Count > maxTerrains)
                            break;

                        if (cur.terrain.leftNeighbor != null)
                            todoQueue.Enqueue(new QueueElement(cur.tileX - 1, cur.tileZ, cur.terrain.leftNeighbor));
                        if (cur.terrain.bottomNeighbor != null)
                            todoQueue.Enqueue(new QueueElement(cur.tileX, cur.tileZ - 1, cur.terrain.bottomNeighbor));
                        if (cur.terrain.rightNeighbor != null)
                            todoQueue.Enqueue(new QueueElement(cur.tileX + 1, cur.tileZ, cur.terrain.rightNeighbor));
                        if (cur.terrain.topNeighbor != null)
                            todoQueue.Enqueue(new QueueElement(cur.tileX, cur.tileZ + 1, cur.terrain.topNeighbor));
                    }
                }
            }

            // run validation to check alignment status
            if (fullValidation)
                terrainMap.Validate();

            return terrainMap;
        }

        // create a terrain map of ALL terrains, by using only their placement to fit them to a grid
        // the position and size of originTerrain defines the grid alignment and origin.  if NULL, we use the first active terrain
        static public TerrainMap CreateFromPlacement(Terrain originTerrain, System.Predicate<Terrain> filter = null, bool fullValidation = true)
        {
            if ((Terrain.activeTerrains == null) || (Terrain.activeTerrains.Length == 0) || (originTerrain == null))
                return null;

            if (originTerrain.terrainData == null)
                return null;

            int groupID = originTerrain.groupingID;
            float gridOriginX = originTerrain.transform.position.x;
            float gridOriginZ = originTerrain.transform.position.z;
            float gridSizeX = originTerrain.terrainData.size.x;
            float gridSizeZ = originTerrain.terrainData.size.z;

            if (filter == null)
                filter = (x => (x.groupingID == groupID));

            return CreateFromPlacement(new Vector2(gridOriginX, gridOriginZ), new Vector2(gridSizeX, gridSizeZ), filter, fullValidation);
        }

        // create a terrain map of ALL terrains, by using only their placement to fit them to a grid
        // the position and size of originTerrain defines the grid alignment and origin.  if NULL, we use the first active terrain
        static public TerrainMap CreateFromPlacement(Vector2 gridOrigin, Vector2 gridSize, System.Predicate<Terrain> filter = null, bool fullValidation = true)
        {
            if ((Terrain.activeTerrains == null) || (Terrain.activeTerrains.Length == 0))
                return null;

            TerrainMap terrainMap = new TerrainMap();

            float gridScaleX = 1.0f / gridSize.x;
            float gridScaleZ = 1.0f / gridSize.y;

            // iterate all active terrains
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                // some integration tests just create a terrain component without terrain data
                if (terrain.terrainData == null)
                    continue;

                if ((filter == null) || filter(terrain))
                {
                    // convert position to a grid index, with proper rounding
                    Vector3 pos = terrain.transform.position;
                    int tileX = Mathf.RoundToInt((pos.x - gridOrigin.x) * gridScaleX);
                    int tileZ = Mathf.RoundToInt((pos.z - gridOrigin.y) * gridScaleZ);
                    // attempt to add the terrain at that grid position
                    terrainMap.TryToAddTerrain(tileX, tileZ, terrain);
                }
            }

            // run validation to check alignment status
            if (fullValidation)
                terrainMap.Validate();

            return (terrainMap.m_terrainTiles.Count > 0) ? terrainMap : null;
        }

        Vector3 m_patchSize;            // size of the first terrain, used for consistency checks

        private TerrainMapStatusCode m_errorCode;

        private Dictionary<TerrainTileCoord, Terrain> m_terrainTiles;
        public Dictionary<TerrainTileCoord, Terrain> terrainTiles => m_terrainTiles;

        public TerrainMap()
        {
            m_errorCode = TerrainMapStatusCode.OK;
            m_terrainTiles = new Dictionary<TerrainTileCoord, Terrain>();
        }

        void AddTerrainInternal(int x, int z, Terrain terrain)
        {
            if (m_terrainTiles.Count == 0)
                m_patchSize = terrain.terrainData.size;
            else
            {
                // check consistency with existing terrains
                if (terrain.terrainData.size != m_patchSize)
                {
                    // ERROR - terrain is not the same size as other terrains
                    m_errorCode |= TerrainMapStatusCode.SizeMismatch;
                }
            }
            m_terrainTiles.Add(new TerrainTileCoord(x, z), terrain);
        }

        // attempt to place the specified terrain tile at the specified (x,z) position, with consistency checks
        bool TryToAddTerrain(int tileX, int tileZ, Terrain terrain)
        {
            bool added = false;
            if (terrain != null)
            {
                Terrain existing = GetTerrain(tileX, tileZ);
                if (existing != null)
                {
                    // already a terrain in the location -- check it is the same tile
                    if (existing != terrain)
                    {
                        // ERROR - multiple different terrains at the same coordinate!
                        m_errorCode |= TerrainMapStatusCode.Overlapping;
                    }
                }
                else
                {
                    // add terrain to the terrain map
                    AddTerrainInternal(tileX, tileZ, terrain);
                    added = true;
                }
            }
            return added;
        }

        void ValidateTerrain(int tileX, int tileZ)
        {
            Terrain terrain = GetTerrain(tileX, tileZ);
            if (terrain != null)
            {
                // grab neighbors (according to grid)
                Terrain left = GetTerrain(tileX - 1, tileZ);
                Terrain right = GetTerrain(tileX + 1, tileZ);
                Terrain top = GetTerrain(tileX, tileZ + 1);
                Terrain bottom = GetTerrain(tileX, tileZ - 1);

                // check edge alignment
                {
                    if (left)
                    {
                        if (!Mathf.Approximately(terrain.transform.position.x, left.transform.position.x + left.terrainData.size.x) ||
                            !Mathf.Approximately(terrain.transform.position.z, left.transform.position.z))
                        {
                            // unaligned edge, tile doesn't match expected location
                            m_errorCode |= TerrainMapStatusCode.EdgeAlignmentMismatch;
                        }
                    }
                    if (right)
                    {
                        if (!Mathf.Approximately(terrain.transform.position.x + terrain.terrainData.size.x, right.transform.position.x) ||
                            !Mathf.Approximately(terrain.transform.position.z, right.transform.position.z))
                        {
                            // unaligned edge, tile doesn't match expected location
                            m_errorCode |= TerrainMapStatusCode.EdgeAlignmentMismatch;
                        }
                    }
                    if (top)
                    {
                        if (!Mathf.Approximately(terrain.transform.position.x, top.transform.position.x) ||
                            !Mathf.Approximately(terrain.transform.position.z + terrain.terrainData.size.z, top.transform.position.z))
                        {
                            // unaligned edge, tile doesn't match expected location
                            m_errorCode |= TerrainMapStatusCode.EdgeAlignmentMismatch;
                        }
                    }
                    if (bottom)
                    {
                        if (!Mathf.Approximately(terrain.transform.position.x, bottom.transform.position.x) ||
                            !Mathf.Approximately(terrain.transform.position.z, bottom.transform.position.z + bottom.terrainData.size.z))
                        {
                            // unaligned edge, tile doesn't match expected location
                            m_errorCode |= TerrainMapStatusCode.EdgeAlignmentMismatch;
                        }
                    }
                }
            }
        }

        // perform all validation checks on the terrain map
        TerrainMapStatusCode Validate()
        {
            // iterate all tiles and validate them
            foreach (TerrainTileCoord coord in m_terrainTiles.Keys)
            {
                ValidateTerrain(coord.tileX, coord.tileZ);
            }
            return m_errorCode;
        }
    }

    [MovedFrom("UnityEngine.Experimental.TerrainAPI")]
    public static class TerrainUtility
    {
        internal static bool ValidTerrainsExist() { return Terrain.activeTerrains != null && Terrain.activeTerrains.Length > 0; }

        internal static void ClearConnectivity()
        {
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                //it should clear only allowAutoConnect flag is true.
                //Otherwise, the setting value will be cleared on first render frame if allowAutoConnect is set to false.
                //case 1241302
                if (terrain.allowAutoConnect)
                    terrain.SetNeighbors(null, null, null, null);
            }
        }

        internal static Dictionary<int, TerrainMap> CollectTerrains(bool onlyAutoConnectedTerrains = true)
        {
            if (!ValidTerrainsExist())
                return null;

            // Collect by groups
            Dictionary<int, TerrainMap> groups = new Dictionary<int, TerrainMap>();
            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (onlyAutoConnectedTerrains && !t.allowAutoConnect)
                    continue;

                if (!groups.ContainsKey(t.groupingID))
                {
                    TerrainMap map = TerrainMap.CreateFromPlacement(t, (x => (x.groupingID == t.groupingID) && !(onlyAutoConnectedTerrains && !x.allowAutoConnect)));
                    if (map != null)
                        groups.Add(t.groupingID, map);
                }
            }
            return (groups.Count != 0) ? groups : null;
        }

        [RequiredByNativeCode]
        public static void AutoConnect()
        {
            if (!ValidTerrainsExist())
                return;

            ClearConnectivity();

            Dictionary<int, TerrainMap> terrainGroups = CollectTerrains();
            if (terrainGroups == null)
                return;

            foreach (var group in terrainGroups)
            {
                TerrainMap terrains = group.Value;

                foreach (var tile in terrains.terrainTiles)
                {
                    TerrainTileCoord coords = tile.Key;

                    Terrain center = terrains.GetTerrain(coords.tileX, coords.tileZ);

                    Terrain left = terrains.GetTerrain(coords.tileX - 1, coords.tileZ);
                    Terrain right = terrains.GetTerrain(coords.tileX + 1, coords.tileZ);
                    Terrain top = terrains.GetTerrain(coords.tileX, coords.tileZ + 1);
                    Terrain bottom = terrains.GetTerrain(coords.tileX, coords.tileZ - 1);

                    center.SetNeighbors(left, top, right, bottom);
                }
            }
        }
    }
}
