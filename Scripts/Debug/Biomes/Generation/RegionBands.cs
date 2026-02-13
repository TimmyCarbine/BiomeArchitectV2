using System;
using BiomeArchitectV2.Core;
using Godot;

namespace BiomeArchitectV2.Biomes.Generation
{
    public sealed class RegionBands
    {
        public int SkyHeightChunks { get; }
        public int SurfaceHeightChunks { get; }
        public int UndergroundHeightChunks { get; }

        public RegionBands(int skyHeightChunks, int surfaceHeightChunks, int undergroundHeightChunks)
        {
            SkyHeightChunks = skyHeightChunks;
            SurfaceHeightChunks = surfaceHeightChunks;
            UndergroundHeightChunks = undergroundHeightChunks;
        }



        public static RegionBands Generate(WorldConfig config, int seed)
        {
            var rng = new Random(unchecked(seed * 73856093 + 19349663));

            int total = config.BiomeChunksY;
            int surface = rng.Next(2, 5);
            int skyMin = Math.Max(1, (int)Mathf.Round(total * 0.25f));
            int skyMax = Math.Max(skyMin, (int)Mathf.Round(total * 0.40f));
            int maxSkyAllowed = Math.Max(1, total - surface - 1);
            int sky = Math.Min(rng.Next(skyMin, skyMax + 1), maxSkyAllowed);
            int underground = total - sky - surface;

            if (underground < 1)
            {
                underground = 1;
                sky = Math.Max(1, total - surface - underground);
            }
            return new RegionBands(sky, surface, underground);
        }



        public RegionId GetRegionForChunkRow(int cy)
        {
            if (cy < SkyHeightChunks) return RegionId.Sky;
            if (cy < SkyHeightChunks + SurfaceHeightChunks) return RegionId.Surface;
            return RegionId.Underground;
        }
    }
}