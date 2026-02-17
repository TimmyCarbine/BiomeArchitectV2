using System.Collections.Generic;
using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Biomes.Maps;

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
        [Export] public int GroundSourceId { get; set; } = 0;
        [Export] public Vector2I GroundAtlasCoords { get; set; } = new(1, 0);
        [Export] public int GroundAlternative { get; set; } = 0;

        private WorldConfig _config = null!;
        private BiomeChunkMap _biomeMap = null!;
        private int _seed;
        private Vector2I _lastCenterChunk = new(int.MinValue, int.MaxValue);
        private readonly HashSet<Vector2I> _loadedChunks = new();
        private Vector2I _tileSizePx;



        public void Init(WorldConfig config, int seed, BiomeChunkMap biomeMap)
        {
            _config = config;
            _seed = seed;
            _biomeMap = biomeMap;

            _tileSizePx = GetTileSizePxOrFallback(_terrainLayer);

            ClearAllLoadedChunks();
            _lastCenterChunk = new(int.MinValue, int.MaxValue);
        }



        public override void _Process(double delta)
        {
            if (_config == null) return;
            if (_biomeMap == null) return;
            if (_terrainLayer == null) return;

            Vector2I center = GetCenterTerrainChunk();

            if (!AlwaysRefresh && center == _lastCenterChunk)
                return;

            _lastCenterChunk = center;
            StreamWindow(center);
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

            endTileY = Mathf.Min(endTileY, _config.TerrainHeightTiles - 1);

            if (!_config.WrapX)
            {
                if (startTileX < 0 || startTileX >= _config.TerrainWidthTiles)
                    return;

                endTileX = Mathf.Min(endTileX, _config.TerrainWidthTiles - 1);
            }

            for (int tx = startTileX; tx < endTileX; tx++)
            {
                int dataX = _config.WrapX ? Mod(tx, _config.TerrainWidthTiles) : tx;
                int surfaceY = HeightAtx(dataX, _config.TerrainHeightTiles, _seed);

                for (int ty = startTileY; ty < endTileY; ty++)
                {
                    if (ty < surfaceY)
                    {
                        _terrainLayer.EraseCell(new Vector2I(tx, ty));
                        continue;
                    }

                    _terrainLayer.SetCell(
                        new Vector2I(tx, ty),
                        GroundSourceId,
                        GroundAtlasCoords,
                        GroundAlternative
                    );
                }
            }
        }



        private void ClearTerrainChunk(Vector2I terrainChunkCoord)
        {
            GetStartEndTiles(terrainChunkCoord, out int startTileX, out int endTileX, out int startTileY, out int endTileY);

            endTileY = Mathf.Min(endTileY, _config.TerrainHeightTiles - 1);

            if (!_config.WrapX)
            {
                if (startTileX < 0 || startTileX >= _config.TerrainWidthTiles)
                    return;

                endTileX = Mathf.Min(endTileX, _config.TerrainWidthTiles - 1);
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



        private static int HeightAtx(int x, int worldHeightTiles, int seed)
        {
            float baseY = worldHeightTiles * 0.35f;
            float a1 = worldHeightTiles * 0.03f;
            float a2 = worldHeightTiles * 0.06f;
            float a3 = worldHeightTiles * 0.02f;

            float f1 = 0.005f;
            float f2 = 0.0018f;
            float f3 = 0.012f;

            float s = seed * 0.0001f;

            float y =
                baseY +
                Mathf.Sin((x * f1) + s) * a1 +
                Mathf.Sin((x * f2) + (s * 2f)) * a2 +
                Mathf.Sin((x * f3) + (s * 5f)) * a3;

            int iy = Mathf.RoundToInt(y);
            iy = Mathf.Clamp(iy, 0, worldHeightTiles - 8);

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
    }
}