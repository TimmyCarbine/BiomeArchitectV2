using BiomeArchitectV2.Biomes.Generation;

namespace BiomeArchitectV2.Biomes.Defs
{
    public sealed class BiomeDef
    {
        public string Id { get; }
        public RegionId Region { get; }
        public int SelectionWeight { get; }

        public BiomeDef(string id, RegionId region, int selectionWeight)
        {
            Id = id;
            Region = region;
            SelectionWeight = selectionWeight;
        }
    }
}