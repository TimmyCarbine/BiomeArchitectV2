using System;
using System.Collections.Generic;
using System.Linq;
using BiomeArchitectV2.Biomes.Defs;
using BiomeArchitectV2.Biomes.Generation;
using BiomeArchitectV2.Biomes.Growth;
using BiomeArchitectV2.Core;

namespace BiomeArchitectV2.Biomes.Maps
{
    public sealed class BiomeChunkMap( WorldConfig config, RegionBands bands, BiomeChunkGrowthResult growth)
    {
        private const int EMPTY_OWNER = -1;

        public WorldConfig Config { get; } = config;
        public RegionBands Bands { get; } = bands;
        public int ChunksX { get; } = growth.ChunksX;
        public int ChunksY { get; } = growth.ChunksY;
        public int[,] Owners { get; } = growth.Owners;
        public IReadOnlyList<BiomeDef> Biomes { get; } = growth.Biomes.ToArray();
    



        public int GetBiomeIndexAtChunk(int cx, int cy)
        {
            if ((uint)cx >= (uint)ChunksX || (uint)cy >= (uint)ChunksY)
                return EMPTY_OWNER;

            return Owners[cx, cy];
        }



        public BiomeDef GetBiomeAtChunk(int cx, int cy)
        {
            int idx = GetBiomeIndexAtChunk(cx, cy);
            if ((uint)idx >= (uint)Biomes.Count)
                return null;

            return Biomes[idx];
        }



        public RegionId GetRegionAtChunkRow(int cy)
        {
            return Bands.GetRegionForChunkRow(cy);
        }
    }
}