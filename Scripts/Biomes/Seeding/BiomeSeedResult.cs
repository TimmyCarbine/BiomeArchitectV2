using System.Collections.Generic;

namespace BiomeArchitectV2.Biomes.Seeding
{
    public sealed class BiomeSeedResult(IReadOnlyList<BiomeSeedData> seeds)
    {
        public IReadOnlyList<BiomeSeedData> Seeds { get; } = seeds;
    }
}