using Godot;
using System;
using BiomeArchitectV2.Core;

namespace BiomeArchitectV2.Debug.Biomes
{
    public sealed partial class BiomeChunkDebugRenderer : Node2D
    {
        private WorldConfig _config = null!;
        private int _seed;

        [Export] public bool ShowGridLines { get; set; } = true;
        [Export] public bool ShowFill { get; set; } = true;
        [Export] public float GridLineWidth { get; set; } = 2f;



        public void Init(WorldConfig config, int seed)
        {
            _config = config;
            _seed = seed;
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
                        Color fill = GetDeterministicChunkColor(cx, cy, _seed);
                        DrawRect(rect, fill, filled: true);
                    }
                }
            }
        }



        private static Color GetDeterministicChunkColor(int cx, int cy, int seed)
        {
            int h = seed;
            h = unchecked(h * 31 + cx);
            h = unchecked(h * 31 + cy);
            h ^= (h << 13);
            h ^= (h >> 17);
            h ^= (h << 5);

            float r = ((h >> 0) & 0xFF) / 255f;
            float g = ((h >> 8) & 0xFF) / 255f;
            float b = ((h >> 16) & 0xFF) / 255f;

            r = 0.25f + 0.75f * r;
            g = 0.25f + 0.75f * g;
            b = 0.25f + 0.75f * b;

            return new Color(r, g, b, 1f);
        }
    }
}