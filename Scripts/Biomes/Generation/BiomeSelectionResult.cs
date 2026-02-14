using System.Collections.Generic;

namespace BiomeArchitectV2.Biomes.Generation
{
    public sealed class BiomeSelectionResult
    {
        public IReadOnlyList<RegionSelectionData> Regions { get; }

        public BiomeSelectionResult(IReadOnlyList<RegionSelectionData> regions)
        {
            Regions = regions;
        }
    }
}