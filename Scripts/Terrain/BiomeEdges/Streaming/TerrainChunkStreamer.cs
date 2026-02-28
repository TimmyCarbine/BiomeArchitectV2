using System.Collections.Generic;
using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Biomes.Maps;
using BiomeArchitectV2.Biomes.Generation;
using BiomeArchitectV2.Biomes.Defs;

namespace BiomeArchitectV2.Terrain.Streaming
{
    public sealed partial class TerrainChunkStreamer : Node
    {
        [Export] private TileMapLayer _terrainLayer = null!;
        [Export] private Node2D _followTarget = null!;

        [Export] public int TerrainChunkSizeTiles { get; set; } = 64;
        [Export] public int StreamRadiusChunksX { get; set; } = 3;
        [Export] public int StreamRadiusChunksY { get; set; } = 2;
        [Export] public bool AlwaysRefresh { get; set; } = false;

        [Export] public int SkySourceId { get; set; } = 0;
        [Export] public Vector2I SkyAtlasCoords { get; set; } = new(0, 0);
        [Export] public int SurfaceSourceId { get; set; } = 0;
        [Export] public Vector2I SurfaceAtlasCoords { get; set; } = new(1, 0);
        [Export] public int UndergroundSourceId { get; set; } = 0;
        [Export] public Vector2I UndergroundAtlasCoords { get; set; } = new(2, 0);

        // Optional biome-specific overrides (simple keyword-based for now)
        [Export] public int LavaSourceId { get; set; } = 0;
        [Export] public Vector2I LavaAtlasCoords { get; set; } = new(3, 0);
        [Export] public int CrystalSourceId { get; set; } = 0;
        [Export] public Vector2I CrystalAtlasCoords { get; set; } = new(0, 0);

        private WorldConfig _config = null!;
        private BiomeChunkMap _biomeMap = null!;
        private BiomeTileMap _biomeTiles = null!;
        private int _seed;
        private Vector2I _lastCenterChunk = new(int.MinValue, int.MaxValue);
        private readonly HashSet<Vector2I> _loadedChunks = new();
        private Vector2I _tileSizePx;



        public void Init(WorldConfig config, int seed, BiomeChunkMap biomeMap, BiomeTileMap biomeTiles)
        {
            _config = config;
            _seed = seed;
            _biomeMap = biomeMap;
            _biomeTiles = biomeTiles;

            _tileSizePx = GetTileSizePxOrFallback(_terrainLayer);

            ClearAllLoadedChunks();
            _lastCenterChunk = new(int.MinValue, int.MaxValue);
        }



        public override void _Process(double delta)
        {
            if (_config == null) return;
            if (_biomeTiles == null) return;
            if (_terrainLayer == null) return;

            Vector2I center = GetCenterTerrainChunk();

            if (!AlwaysRefresh && center == _lastCenterChunk)
                return;

            _lastCenterChunk = center;
            StreamWindow(center);
        }



        public void SetFollowTarget(Node2D target)
        {
            _followTarget = target;
            _lastCenterChunk = new Vector2I(int.MinValue, int.MaxValue);
        }



        public Vector2I GetTerrainTileSizePxForBuild()
        {
            return GetTileSizePxOrFallback(_terrainLayer);
        }



        private void StreamWindow(Vector2I centerChunk)
        {
            int minX = centerChunk.X - StreamRadiusChunksX;
            int maxX = centerChunk.X + StreamRadiusChunksX;
            int minY = centerChunk.Y - StreamRadiusChunksY;
            int maxY = centerChunk.Y + StreamRadiusChunksY;

            int terrainChunksY = CeilDiv(_config.TerrainHeightTiles, TerrainChunkSizeTiles);
            minY = Mathf.Clamp(minY, 0, terrainChunksY - 1);
            maxY = Mathf.Clamp(maxY, 0, terrainChunksY - 1);

            int terrainChunksX = CeilDiv(_config.TerrainWidthTiles, TerrainChunkSizeTiles);
            if (!_config.WrapX)
            {
                minX = Mathf.Clamp(minX, 0, terrainChunksX - 1);
                maxX = Mathf.Clamp(maxX, 0, terrainChunksX - 1);
            }

            var desired = new HashSet<Vector2I>();

            for (int cy = minY; cy <= maxY; cy++)
            {
                for (int cx = minX; cx <= maxX; cx++)
                {
                    var key = new Vector2I(cx, cy);
                    desired.Add(key);

                    if (!_loadedChunks.Contains(key))
                    {
                        PaintTerrainChunk(key);
                        _loadedChunks.Add(key);
                    }
                }
            }

            var toRemove = new List<Vector2I>();
            foreach (var c in _loadedChunks)
            {
                if (!desired.Contains(c))
                    toRemove.Add(c);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                ClearTerrainChunk(toRemove[i]);
                _loadedChunks.Remove(toRemove[i]);
            }
        }



        private void PaintTerrainChunk(Vector2I terrainChunkCoord)
        {
            GetStartEndTiles(terrainChunkCoord, out int startTileX, out int endTileX, out int startTileY, out int endTileY);

            endTileY = Mathf.Min(endTileY, _config.TerrainHeightTiles);

            if (!_config.WrapX)
            {
                if (startTileX < 0 || startTileX >= _config.TerrainWidthTiles)
                    return;

                endTileX = Mathf.Min(endTileX, _config.TerrainWidthTiles);
            }

            for (int tx = startTileX; tx < endTileX; tx++)
            {
                int dataX = _config.WrapX ? Mod(tx, _config.TerrainWidthTiles) : tx;
                int surfaceY = HeightAtx(dataX, _config.TerrainWidthTiles, _config.TerrainHeightTiles, _seed);

                for (int ty = startTileY; ty < endTileY; ty++)
                {
                    if (ty < surfaceY)
                    {
                        _terrainLayer.EraseCell(new Vector2I(tx, ty));
                        continue;
                    }

                    GetGroundTileFor(tx, ty, out int src, out Vector2I atlas, out int alt);

                    _terrainLayer.SetCell(
                        new Vector2I(tx, ty),
                        src,
                        atlas,
                        alt
                    );
                }
            }
        }



        private void ClearTerrainChunk(Vector2I terrainChunkCoord)
        {
            GetStartEndTiles(terrainChunkCoord, out int startTileX, out int endTileX, out int startTileY, out int endTileY);

            endTileY = Mathf.Min(endTileY, _config.TerrainHeightTiles);

            if (!_config.WrapX)
            {
                if (startTileX < 0 || startTileX >= _config.TerrainWidthTiles)
                    return;

                endTileX = Mathf.Min(endTileX, _config.TerrainWidthTiles);
            }

            for (int tx = startTileX; tx < endTileX; tx++)
            {
                for (int ty = startTileY; ty < endTileY; ty++)
                {
                    _terrainLayer.EraseCell(new Vector2I(tx, ty));
                }
            }
        }



        private void GetStartEndTiles(Vector2I terrainChunkCoord, out int startTileX, out int endTileX, out int startTileY, out int endTileY)
        {
            startTileX = terrainChunkCoord.X * TerrainChunkSizeTiles;
            startTileY = terrainChunkCoord.Y * TerrainChunkSizeTiles;
            endTileX = startTileX + TerrainChunkSizeTiles;
            endTileY = startTileY + TerrainChunkSizeTiles;
        }



        private void ClearAllLoadedChunks()
        {
            foreach (var c in _loadedChunks)
                ClearTerrainChunk(c);

            _loadedChunks.Clear();
        }



        private Vector2I GetCenterTerrainChunk()
        {
            Vector2 worldPos = _followTarget != null
                ? _followTarget.GlobalPosition
                : new Vector2(_config.TerrainWidthTiles * _tileSizePx.X * 0.5f, _config.TerrainHeightTiles * _tileSizePx.Y * 0.5f);

            int tileX = Mathf.FloorToInt(worldPos.X / _tileSizePx.X);
            int tileY = Mathf.FloorToInt(worldPos.Y / _tileSizePx.Y);

            int cx = Mathf.FloorToInt((float)tileX / TerrainChunkSizeTiles);
            int cy = Mathf.FloorToInt((float)tileY / TerrainChunkSizeTiles);

            int terrainChunksY = CeilDiv(_config.TerrainHeightTiles, TerrainChunkSizeTiles);
            cy = Mathf.Clamp(cy, 0, terrainChunksY - 1);

            if (!_config.WrapX)
            {
                int terrainChunksX = CeilDiv(_config.TerrainWidthTiles, TerrainChunkSizeTiles);
                cx = Mathf.Clamp(cx, 0, terrainChunksX - 1);
            }

            return new Vector2I(cx, cy);
        }



        private static int HeightAtx(int x, int worldWidthTiles, int worldHeightTiles, int seed)
        {
            x = Mod(x, worldWidthTiles);

            float baseY = worldHeightTiles * 0.35f;
            float a1 = worldHeightTiles * 0.03f;
            float a2 = worldHeightTiles * 0.06f;
            float a3 = worldHeightTiles * 0.02f;

            float w = Mathf.Tau / worldWidthTiles;

            float p1 = seed * 0.00010f;
            float p2 = seed * 0.00023f;
            float p3 = seed * 0.00047f;

            float y =
                baseY +
                Mathf.Sin(w * 1f * x + p1) * a1 +
                Mathf.Sin(w * 3f * x + p2) * a2 +
                Mathf.Sin(w * 7f * x + p3) * a3;

            int iy = Mathf.RoundToInt(y);
            iy = Mathf.Clamp(iy, 0, worldHeightTiles - 1);

            return iy;
        }



        private static Vector2I GetTileSizePxOrFallback(TileMapLayer layer)
        {
            if (layer.TileSet != null)
            {
                Vector2I size = layer.TileSet.TileSize;
                if (size.X > 0 && size.Y > 0)
                    return size;
            }

            return new Vector2I(32, 32);
        }



        private static int CeilDiv(int a, int b) => (a + b -1) / b;



        private static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }



        private BiomeDef GetBiomeDefAtTerrainTile(int terrainTileX, int terrainTileY)
        {
            int wx = _config.WrapX ? Mod(terrainTileX, _config.TerrainWidthTiles) : terrainTileX;

            if ((uint)wx >= (uint)_config.TerrainWidthTiles)
                return null;

            if ((uint)terrainTileY >= (uint)_config.TerrainHeightTiles)
                return null;

            byte b = _biomeTiles.Get(wx, terrainTileY);
            if (b == BiomeTileMap.UNASSIGNED)
                return null;

            int idx = b;
            if ((uint)idx >= (uint)_biomeMap.Biomes.Count)
                return null;

            return _biomeMap.Biomes[idx];
        }



        private string GetBiomeIdAtTerrainTile(int terrainTileX, int terrainTileY)
        {
            var biome = GetBiomeDefAtTerrainTile(terrainTileX, terrainTileY);

            return biome?.Id ?? string.Empty;
        }



        private byte GetBiomeIndexAtTerrainTile(int terrainTileX, int terrainTileY)
        {
            int wx = _config.WrapX ? Mod(terrainTileX, _config.TerrainWidthTiles) : terrainTileX;
            wx = Mathf.Clamp(wx, 0, _config.TerrainWidthTiles - 1);
            int y = Mathf.Clamp(terrainTileY, 0, _config.TerrainHeightTiles - 1);

            return _biomeTiles.Get(wx, y);
        }



        private RegionId GetRegionAtTerrainTile(int terrainTileX, int terrainTileY)
        {
            var biome = GetBiomeDefAtTerrainTile(terrainTileX, terrainTileY);
            return biome != null ? biome.Region : RegionId.Underground;
        }



        private void GetGroundTileFor(int terrainTileX, int terrainTileY, out int sourceId, out Vector2I atlas, out int alt)
        {
            var biome = GetBiomeDefAtTerrainTile(terrainTileX, terrainTileY);

            // Fallback if unassigned
            if (biome == null)
            {
                sourceId = UndergroundSourceId;
                atlas = UndergroundAtlasCoords;
                alt = 0;
                return;
            }

            // Optional keyword overrides
            string id = biome.Id.ToLowerInvariant();
            if (id.Contains("lava") || id.Contains("magma") || id.Contains("basalt"))
            {
                sourceId = LavaSourceId;
                atlas = LavaAtlasCoords;
                alt = 0;
                return;
            }
            if (id.Contains("crystal") || id.Contains("geode") || id.Contains("quartz"))
            {
                sourceId = CrystalSourceId;
                atlas = CrystalAtlasCoords;
                alt = 0;
                return;
            }

            // Region-based fallback
            switch (biome.Region)
            {
                case RegionId.Sky:
                    sourceId = SkySourceId;
                    atlas = SkyAtlasCoords;
                    alt = 0;
                    return;

                case RegionId.Surface:
                    sourceId = SurfaceSourceId;
                    atlas = SurfaceAtlasCoords;
                    alt = 0;
                    return;

                default:
                    sourceId = UndergroundSourceId;
                    atlas = UndergroundAtlasCoords;
                    alt = 0;
                    return;
            }
        }
    }
}