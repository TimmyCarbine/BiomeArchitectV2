using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Biomes.Defs;
using BiomeArchitectV2.Biomes.Generation;
using BiomeArchitectV2.Biomes.Growth;

namespace BiomeArchitectV2.Debug.Biomes
{
    public sealed partial class BiomeChunkDebugRenderer : Node2D
    {
        private WorldConfig _config = null!;
        private int _seed;
        private RegionBands _bands = null!;
        private BiomeChunkGrowthResult _growth = null!;

        [Export] public bool ShowGridLines { get; set; } = true;
        [Export] public bool ShowFill { get; set; } = true;
        [Export] public float GridLineWidth { get; set; } = 2f;



        public void Init(WorldConfig config, int seed, RegionBands bands)
        {
            _config = config;
            _seed = seed;
            _bands = bands;
            _growth = null;
            QueueRedraw();
        }

        public void Init(WorldConfig config, int seed, RegionBands bands, BiomeChunkGrowthResult growth)
        {
            _config = config;
            _seed = seed;
            _bands = bands;
            _growth = growth;
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

                        Color fill;

                        if (_growth != null)
                        {
                            int owner = _growth.Owners[cx, cy];
                            if (owner >= 0 && owner < _growth.Biomes.Count)
                            {
                                BiomeDef biome = _growth.Biomes[owner];
                                Color biomeColor = GetDeterministicBiomeColor(biome.Id, _seed);
                                
                                fill = ApplyRegionLighting(biomeColor, region);
                                fill.A = 1f;
                            }
                            else
                            {
                                fill = GetRegionGreyscale(region);
                            }
                        }
                        else
                        {
                            fill = GetRegionGreyscale(region);
                        }

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



        private static Color GetDeterministicBiomeColor(string biomeId, int seed)
        {
            int h = seed;
            for (int i = 0; i < biomeId.Length; i++)
                h = unchecked(h * 31 + biomeId[i]);

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



        private static Color ApplyRegionLighting(Color biomeColor, RegionId region)
        {
            const float SKY_TO_WHITE = 0.65f;
            const float SURFACE_TO_WHITE = 0.05f;
            const float UNDERGROUND_TO_BLACK = 0.55f;

            return region switch
            {
                RegionId.Sky => biomeColor.Lerp(Colors.White, SKY_TO_WHITE),
                RegionId.Surface => biomeColor.Lerp(Colors.White, SURFACE_TO_WHITE),
                _ => biomeColor.Lerp(Colors.Black, UNDERGROUND_TO_BLACK),
            };
        }
    }
}