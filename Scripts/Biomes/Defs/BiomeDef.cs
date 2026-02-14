using BiomeArchitectV2.Biomes.Generation;
using Godot;

namespace BiomeArchitectV2.Biomes.Defs
{
    public sealed class BiomeDef(string id, RegionId region, int selectionWeight, float? preferredVertical01 = null, float verticalBiasStrength01 = 0f)
    {
        public string Id { get; } = id;
        public RegionId Region { get; } = region;
        public int SelectionWeight { get; } = selectionWeight;
        public float? PreferredVertical01 { get; } = preferredVertical01;
        public float VerticalBiasStrength01 { get; } = Mathf.Clamp(verticalBiasStrength01, 0f, 1f);

        public override string ToString() => Id;
    }
}