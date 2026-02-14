using Godot;
using BiomeArchitectV2.Biomes.Defs;
using BiomeArchitectV2.Biomes.Generation;

namespace BiomeArchitectV2.Biomes.Seeding
{
    public readonly struct BiomeSeedData(BiomeDef biome, RegionId region, Vector2I chunkCoord)
    {
        public BiomeDef Biome { get; } = biome;
        public RegionId Region { get; } = region;
        public Vector2I ChunkCoord { get; } = chunkCoord;

        public override string ToString()
        {
            return $"{Biome.Id} @ ({ChunkCoord.X},{ChunkCoord.Y})";
        }
    }
}