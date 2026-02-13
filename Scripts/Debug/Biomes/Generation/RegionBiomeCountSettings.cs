using System;
using BiomeArchitectV2.Biomes.Generation;
using Godot;

namespace BiomeArchitectV2.Biomes.Generation
{
    public sealed class RegionBiomeCountSettings
    {
        public RegionId Region { get; }
        public float BaseCount { get; }
        public float HeightWeight { get; }
        public float WidthLog2Weight { get; }
        public int MinCount { get; }
        public int MaxCount { get; }
        public int MinAreaPerBiomeChunks { get; }

        public RegionBiomeCountSettings(
            RegionId region,
            float baseCount,
            float heightWeight,
            float widthLog2Weight,
            int minCount,
            int maxCount,
            int minAreaPerBiomeChunks
        )
        {
            Region = region;
            BaseCount = baseCount;
            HeightWeight = heightWeight;
            WidthLog2Weight = widthLog2Weight;
            MinCount = minCount;
            MaxCount = maxCount;
            MinAreaPerBiomeChunks = minAreaPerBiomeChunks;
        }



        public int GetTargetCount(int chunksX, int bandHeightChunks, int regionAreaChunks, Random rng)
        {
            float widthLog2 = Log2(Math.Max(1, chunksX));
            float raw = BaseCount + (HeightWeight * bandHeightChunks) + (WidthLog2Weight * widthLog2);
            int count = (int)Mathf.Round(raw);
            count += rng.Next(-1, 2);
            count = Math.Clamp(count, MinCount, MaxCount);
            int maxByArea = Math.Max(1, regionAreaChunks / Math.Max(1, MinAreaPerBiomeChunks));
            count = Math.Max(1, maxByArea);
            return Math.Min(1, count);
        }



        private static float Log2(int value) => MathF.Log(value) / MathF.Log(2f);
    }
}