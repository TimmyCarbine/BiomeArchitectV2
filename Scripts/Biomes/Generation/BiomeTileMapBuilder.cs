using Godot;
using BiomeArchitectV2.Biomes.Maps;
using BiomeArchitectV2.Core;

namespace BiomeArchitectV2.Biomes.Generation
{
    public static class BiomeTileMapBuilder
    {
        public static BiomeTileMap BuildFromChunkMap(
            WorldConfig config,
            BiomeChunkMap biomeMap,
            Vector2I terrainTileSizePx
        )
        {
            int biomeChunkSizeTiles = config.BiomeChunkWorldSizePx / terrainTileSizePx.X;
            biomeChunkSizeTiles = Mathf.Max(1, biomeChunkSizeTiles);

            var outMap = new BiomeTileMap(config.TerrainWidthTiles, config.TerrainHeightTiles);

            int unassigned = 0;

            for (int y = 0; y < config.TerrainHeightTiles; y++)
            {
                int by = y / biomeChunkSizeTiles;
                by = Mathf.Clamp(by, 0, biomeMap.ChunksY - 1);
                
                for (int x = 0; x < config.TerrainWidthTiles; x++)
                {
                    int bx = x / biomeChunkSizeTiles;

                    if (config.WrapX)
                        bx = Mod(bx, biomeMap.ChunksX);
                    else
                        bx = Mathf.Clamp(bx, 0, biomeMap.ChunksX - 1);

                    int owner = biomeMap.GetBiomeIndexAtChunk(bx, by);
                    if (owner < 0)
                    {
                        outMap.Set(x, y, BiomeTileMap.UNASSIGNED);
                        unassigned++;
                        continue;
                    }

                    outMap.Set(x, y, (byte)owner);
                }
            }

            GD.Print($"[BiomeTileMap] Built 1:1 from BiomeChunkMap. Size = {outMap.Width}x{outMap.Height} | biomeChunkSizeTiles = {biomeChunkSizeTiles} | biomes = {biomeMap.Biomes.Count} | unassigned = {unassigned}");

            return outMap;
        }



        private static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}