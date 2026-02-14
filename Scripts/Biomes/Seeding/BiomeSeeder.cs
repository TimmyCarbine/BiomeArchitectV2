using System;
using System.Collections.Generic;
using Godot;
using BiomeArchitectV2.Biomes.Defs;
using BiomeArchitectV2.Biomes.Generation;

namespace BiomeArchitectV2.Biomes.Seeding
{
    public static class BiomeSeeder
    {
        public static BiomeSeedResult Run(
            BiomeSelectionResult selection,
            RegionBands bands,
            int chunksX,
            int seed
        )
        {
            var rngSky = new Random(unchecked(seed * 41203 + 1));
            var rngSurface = new Random(unchecked(seed * 41203 + 2));
            var rngUnderground = new Random(unchecked(seed * 41203 + 3));
            var seeds = new List<BiomeSeedData>(capacity: 64);

            SeedRegion(RegionId.Sky, bands.SkyHeightChunks, regionTopY: 0, rngSky);
            SeedRegion(RegionId.Surface, bands.SurfaceHeightChunks, regionTopY: bands.SkyHeightChunks, rngSurface);
            SeedRegion(RegionId.Underground, bands.UndergroundHeightChunks, regionTopY: bands.SkyHeightChunks + bands.SurfaceHeightChunks, rngUnderground);

            return new BiomeSeedResult(seeds);

            void SeedRegion(RegionId region, int bandHeight, int regionTopY, Random rng)
            {
                RegionSelectionData regionData = FindRegion(selection, region);
                int n = regionData.SelectedBiomes.Count;
                if (n <= 0)
                    return;

                var ordered = OrderBiomesForSeeding(regionData.SelectedBiomes);

                for (int i = 0; i < ordered.Count; i++)
                {
                    BiomeDef biome = ordered[i];
                    (int minX, int maxX) = GetSlotRange(i, ordered.Count, chunksX);
                    int x = rng.Next(minX, maxX + 1);
                    int y = PickYWithBand(biome, regionTopY, bandHeight, rng);

                    seeds.Add(new BiomeSeedData(biome, region, new Vector2I(x, y)));
                }
            }
        }



        private static RegionSelectionData FindRegion(BiomeSelectionResult selection, RegionId region)
        {
            foreach (var r in selection.Regions)
            {
                if (r.Region == region)
                    return r;
            }

            throw new InvalidOperationException($"SelectionResult missing region {region}");
        }



        private static List<BiomeDef> OrderBiomesForSeeding(IReadOnlyList<BiomeDef> biomes)
        {
            var biased = new List<BiomeDef>();
            var unbiased = new List<BiomeDef>();

            foreach (var b in biomes)
            {
                if (b.PreferredVertical01.HasValue && b.VerticalBiasStrength01 > 0f)
                    biased.Add(b);
                else
                    unbiased.Add(b);
            }

            biased.Sort((a, b) => b.VerticalBiasStrength01.CompareTo(a.VerticalBiasStrength01));
            biased.Sort((a, b) =>
            {
                int cmp = b.VerticalBiasStrength01.CompareTo(a.VerticalBiasStrength01);
                return cmp != 0 ? cmp : string.CompareOrdinal(a.Id, b.Id);
            });

            unbiased.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            var ordered = new List<BiomeDef>(biomes.Count);
            ordered.AddRange(biased);
            ordered.AddRange(unbiased);

            return ordered;
        }



        private static (int minX, int maxX) GetSlotRange(int index, int count, int chunksX)
        {
            int minX = index * chunksX / count;
            int maxXExclusive = (index + 1) * chunksX / count;

            int maxX = Math.Max(minX, maxXExclusive - 1);
            maxX = Math.Min(maxX, chunksX - 1);

            return (minX, maxX);
        }



        private static int PickYWithBand(BiomeDef biome, int regionTopY, int bandHeight, Random rng)
        {
            int minY = regionTopY;
            int maxY = regionTopY + bandHeight - 1;

            if (bandHeight <= 1)
                return minY;

            if (biome.PreferredVertical01 is null || biome.VerticalBiasStrength01 <= 0f)
            {
                return rng.Next(minY, maxY + 1);
            }

            float t = Math.Clamp(biome.PreferredVertical01.Value, 0f, 1f);
            float ideal = minY + t * (bandHeight - 1);
            float strength = Math.Clamp(biome.VerticalBiasStrength01, 0f, 1f);
            float jitterRadius = Lerp(bandHeight * 0.35f, 1.0f, strength);
            int jitter = rng.Next(-(int)MathF.Ceiling(jitterRadius), (int)MathF.Ceiling(jitterRadius) + 1);
            int y = (int)MathF.Round(ideal) + jitter;
            
            return Math.Clamp(y, minY, maxY);
        }



        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}