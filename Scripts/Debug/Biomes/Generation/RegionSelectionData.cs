using System.Collections.Generic;
using BiomeArchitectV2.Biomes.Defs;

namespace BiomeArchitectV2.Biomes.Generation
{
    public readonly struct RegionSelectionData(
        RegionId region,
        int bandHeight,
        int area,
        int targetCount,
        IReadOnlyList<BiomeDef> selectedBiomes
    )
    {
        public RegionId Region { get; } = region;
        public int BandHeight { get; } = bandHeight;
        public int Area { get; } = area;
        public int TargetCount { get; } = targetCount;
        public IReadOnlyList<BiomeDef> SelectedBiomes { get; } = selectedBiomes;
    }
}