using System.Collections.Generic;

namespace BiomeArchitectV2.Biomes.Generation
{
    public static class RegionBiomeCountSettingsProvider
    {
        public static IReadOnlyDictionary<RegionId, RegionBiomeCountSettings> CreateDefaults()
        {
            return new Dictionary<RegionId, RegionBiomeCountSettings>
            {
                {
                    RegionId.Sky, new RegionBiomeCountSettings(
                        region: RegionId.Sky,
                        baseCount: 0f,
                        heightWeight: 0.6f,
                        widthLog2Weight: 0.3f,
                        minCount: 2,
                        maxCount: 10,
                        minAreaPerBiomeChunks: 20
                    )
                },
                {
                    RegionId.Surface, new RegionBiomeCountSettings(
                        region: RegionId.Surface,
                        baseCount: 0f,
                        heightWeight: 0.55f,
                        widthLog2Weight: 0.25f,
                        minCount: 2,
                        maxCount: 8,
                        minAreaPerBiomeChunks: 24
                    )
                },
                {
                    RegionId.Underground, new RegionBiomeCountSettings(
                        region: RegionId.Underground,
                        baseCount: 0.5f,
                        heightWeight: 0.5f,
                        widthLog2Weight: 0.3f,
                        minCount: 3,
                        maxCount: 12,
                        minAreaPerBiomeChunks: 28
                    )
                },
            };
        }
    }
}