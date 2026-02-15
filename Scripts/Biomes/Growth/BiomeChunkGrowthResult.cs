using System.Collections.Generic;
using BiomeArchitectV2.Biomes.Defs;

namespace BiomeArchitectV2.Biomes.Growth
{
    public sealed class BiomeChunkGrowthResult(int chunksX, int chunksY, int[,] owners, IReadOnlyList<BiomeDef> biomes)
    {
        public int ChunksX { get; } = chunksX;
        public int ChunksY { get; } = chunksY;
        public int[,] Owners { get; } = owners;
        public IReadOnlyList<BiomeDef> Biomes { get; } = biomes;
    }
}