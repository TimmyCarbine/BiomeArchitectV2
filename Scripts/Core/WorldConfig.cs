using System;

namespace BiomeArchitectV2.Core
{
    public sealed class WorldConfig
    {
        public const int TERRAIN_TILE_SIZE_PX = 32;
        public const int BIOME_CHUNK_SIZE_TILES = 64;
        public int TerrainWidthTiles { get; }
        public int TerrainHeightTiles { get; }
        public int BiomeChunksX => TerrainWidthTiles / BIOME_CHUNK_SIZE_TILES;
        public int BiomeChunksY => TerrainHeightTiles / BIOME_CHUNK_SIZE_TILES;
        public int BiomeChunkWorldSizePx => BIOME_CHUNK_SIZE_TILES * TERRAIN_TILE_SIZE_PX;
        public bool WrapX { get; }



        public WorldConfig(int terrainWidthTiles, int terrainHeightTiles)
        {
            if (terrainWidthTiles % BIOME_CHUNK_SIZE_TILES !=0)
                throw new ArgumentException($"TerrainWidthTiles must be divisible by {BIOME_CHUNK_SIZE_TILES}.", nameof(terrainWidthTiles));

            if (terrainHeightTiles % BIOME_CHUNK_SIZE_TILES != 0)
                throw new ArgumentException($"TerrainHeightTiles must be divisible by {BIOME_CHUNK_SIZE_TILES}.", nameof(terrainHeightTiles));

            TerrainWidthTiles = terrainWidthTiles;
            TerrainHeightTiles = terrainHeightTiles;
            WrapX = false;
        }
    }
}