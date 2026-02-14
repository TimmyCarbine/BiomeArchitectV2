using System;
using System.Collections.Generic;
using BiomeArchitectV2.Biomes.Catalog;

namespace BiomeArchitectV2.Biomes.Generation
{
    public static class BiomeSelectionPipeline
    {
        public static BiomeSelectionResult Run(
            BiomeCatalog catalog,
            RegionBands bands,
            int chunksX,
            int seed
        )
        {
            var settings = RegionBiomeCountSettingsProvider.CreateDefaults();
            var rng = new Random(unchecked(seed * 92821 + 1337));

            var regions = new List<RegionSelectionData>(capacity: 3);

            AddRegion(RegionId.Sky, bands.SkyHeightChunks);
            AddRegion(RegionId.Surface, bands.SurfaceHeightChunks);
            AddRegion(RegionId.Underground, bands.UndergroundHeightChunks);

            return new BiomeSelectionResult(regions);

            void AddRegion(RegionId region, int bandHeight)
            {
                int area = chunksX * bandHeight;
                int target = settings[region].GetTargetCount(chunksX, bandHeight, area, rng);

                var picked = BiomeSelection.SelectForRegion(catalog, region, target, rng);

                regions.Add(new RegionSelectionData(region, bandHeight, area, target, picked.ToArray()));
            }
        }
    }
}