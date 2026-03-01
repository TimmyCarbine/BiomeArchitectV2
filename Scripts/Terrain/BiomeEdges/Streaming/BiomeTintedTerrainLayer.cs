using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Biomes.Maps;
using BiomeArchitectV2.Biomes.Defs;
using BiomeArchitectV2.Biomes.Generation;

namespace BiomeArchitectV2.Terrain.Streaming
{
    /// <summary>
    /// TileMapLayer that tints each placed tile at runtime based on the biome colour.
    /// Uses Godot 4's runtime TileData hooks:
    ///  - _use_tile_data_runtime_update()
    ///  - _tile_data_runtime_update()
    /// </summary>
    public sealed partial class BiomeTintedTerrainLayer : TileMapLayer
    {
        private WorldConfig _config = null!;
        private BiomeChunkMap _biomeMap = null!;
        private BiomeTileMap _biomeTiles = null!;

        /// <summary>
        /// Call this after you create/assign biome maps (same time you init the streamer).
        /// </summary>
        public void Init(WorldConfig config, BiomeChunkMap biomeMap, BiomeTileMap biomeTiles)
        {
            _config = config;
            _biomeMap = biomeMap;
            _biomeTiles = biomeTiles;
        }

        /// <summary>
        /// Godot calls this to ask "should I run _tile_data_runtime_update for this cell?"
        /// Return true only for cells you actually want tinted (performance).
        /// </summary>
        public override bool _UseTileDataRuntimeUpdate(Vector2I coords)
        {
            // If we haven't been initialised yet, do nothing.
            if (_config == null || _biomeMap == null || _biomeTiles == null)
                return false;

            // Only update if there is actually a tile placed here.
            // (Empty cells return source_id = -1)
            if (GetCellSourceId(coords) < 0)
                return false;

            // If the biome lookup is invalid/unassigned, don't bother updating.
            byte b = GetBiomeIndexAtTerrainTile(coords.X, coords.Y);
            return b != BiomeTileMap.UNASSIGNED;
        }

        /// <summary>
        /// Godot calls this right before rendering a tile.
        /// You can safely edit tile_data here (this is per-cell, runtime-only).
        /// </summary>
        public override void _TileDataRuntimeUpdate(Vector2I coords, TileData tileData)
        {
            // Safety checks.
            if (_config == null || _biomeMap == null || _biomeTiles == null)
                return;

            // Fetch biome definition for this terrain cell.
            BiomeDef biome = GetBiomeDefAtTerrainTile(coords.X, coords.Y);
            if (biome == null)
                return;

            // Apply your region lighting rule to the biome colour.
            Color tint = ApplyRegionLighting(biome.Colour, biome.Region);

            // This is the key: per-cell tint via TileData.Modulate.
            tileData.Modulate = tint;
        }

        // ----------------------------
        // Biome lookup helpers
        // ----------------------------

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

        private byte GetBiomeIndexAtTerrainTile(int terrainTileX, int terrainTileY)
        {
            int wx = _config.WrapX ? Mod(terrainTileX, _config.TerrainWidthTiles) : terrainTileX;
            wx = Mathf.Clamp(wx, 0, _config.TerrainWidthTiles - 1);

            int y = Mathf.Clamp(terrainTileY, 0, _config.TerrainHeightTiles - 1);

            return _biomeTiles.Get(wx, y);
        }

        private static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        // ----------------------------
        // Your lighting rule
        // ----------------------------

        private static Color ApplyRegionLighting(Color biomeColor, RegionId region)
        {
            const float SKY_TO_WHITE = 0.65f;
            const float SURFACE_TO_WHITE = 0.05f;
            const float UNDERGROUND_TO_BLACK = 0.55f;

            return region switch
            {
                RegionId.Sky => biomeColor.Lerp(Colors.White, SKY_TO_WHITE),
                RegionId.Surface => biomeColor.Lerp(Colors.White, SURFACE_TO_WHITE),
                _ => biomeColor.Lerp(Colors.Black, UNDERGROUND_TO_BLACK),
            };
        }
    }
}