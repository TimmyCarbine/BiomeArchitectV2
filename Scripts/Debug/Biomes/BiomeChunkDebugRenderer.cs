using Godot;
using System;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Biomes.Generation;

namespace BiomeArchitectV2.Debug.Biomes
{
    public sealed partial class BiomeChunkDebugRenderer : Node2D
    {
        private WorldConfig _config = null!;
        private int _seed;
        private BiomeArchitectV2.Biomes.Generation.RegionBands _bands = null!;

        [Export] public bool ShowGridLines { get; set; } = true;
        [Export] public bool ShowFill { get; set; } = true;
        [Export] public float GridLineWidth { get; set; } = 2f;



        public void Init(WorldConfig config, int seed, RegionBands bands)
        {
            _config = config;
            _seed = seed;
            _bands = bands;
            QueueRedraw();
        }



        public override void _Draw()
        {
            if (_config == null) return;

            int chunkSizePx = _config.BiomeChunkWorldSizePx;

            for (int cy = 0; cy < _config.BiomeChunksY; cy++)
            {
                for (int cx = 0; cx < _config.BiomeChunksX; cx++)
                {
                    Rect2 rect = new(
                        x: cx * chunkSizePx,
                        y: cy * chunkSizePx,
                        width: chunkSizePx,
                        height: chunkSizePx
                    );

                    if (ShowFill)
                    {
                        var region = _bands.GetRegionForChunkRow(cy);
                        Color fill = GetRegionGreyscale(region);
                        DrawRect(rect, fill, filled: true);
                    }
                }
            }
        }



        private static Color GetRegionGreyscale(RegionId region)
        {
            return region switch
            {
                RegionId.Sky => new Color(0.80f, 0.80f, 0.80f, 1f),
                RegionId.Surface => new Color(0.55f, 0.55f, 0.55f, 1f),
                _ => new Color(0.20f, 0.20f, 0.20f, 1f),
            };
        }
    }
}