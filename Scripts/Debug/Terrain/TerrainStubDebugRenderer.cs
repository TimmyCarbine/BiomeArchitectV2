using Godot;
using BiomeArchitectV2.Biomes.Maps;
using BiomeArchitectV2.Core;

namespace BiomeArchitectV2.Debug.Terrain
{
    public sealed partial class TerrainStubDebugRenderer : Node2D
    {
        private const int MIN_DOTS_PER_CHUNK = 6;
        private const int MAX_DOTS_PER_CHUNK = 18;
        private const float DOT_RADIUS_MIN = 2.0f;
        private const float DOT_RADIUS_MAX = 4.5f;

        private WorldConfig _config = null!;
        private int _seed;
        private BiomeChunkMap _biomeMap = null!;

        [Export] public bool Enabled { get; set; } = true;
        [Export] public int DotCountMin { get; set; } = MIN_DOTS_PER_CHUNK;
        [Export] public int DotCountMax { get; set; } = MAX_DOTS_PER_CHUNK;
        [Export] public float DotRadiusMin { get; set; } = DOT_RADIUS_MIN;
        [Export] public float DotRadiusMax { get; set; } = DOT_RADIUS_MAX;
        [Export] public float Alpha { get; set; } = 0.90f;

        public void Init(WorldConfig config, int seed, BiomeChunkMap biomeMap)
        {
            _config = config;
            _seed = seed;
            _biomeMap = biomeMap;
            QueueRedraw();
        }



        public override void _Draw()
        {
            if (!Enabled) return;
            if (_config == null) return;
            if (_biomeMap == null) return;

            int chunkSizePx = _config.BiomeChunkWorldSizePx;

            for (int cy = 0; cy < _biomeMap.ChunksY; cy++)
            {
                for (int cx = 0; cx < _biomeMap.ChunksX; cx++)
                {
                    var biome = _biomeMap.GetBiomeAtChunk(cx, cy);
                    if (biome == null)
                        return;

                    int dots = RangeInt(Hash(_seed, cx, cy, biome.Id, salt: 11), DotCountMin, DotCountMax);

                    for (int i = 0; i < dots; i++)
                    {
                        int hPos = Hash(_seed, cx, cy, biome.Id, salt: 1000 + i);
                        float rx = Range01(hPos ^ 0xA2F1);
                        float ry = Range01(hPos ^ 0x19C7);

                        float px = (cx * chunkSizePx) + (rx * chunkSizePx);
                        float py = (cy * chunkSizePx) + (ry * chunkSizePx);

                        int hType = Hash(_seed, cx, cy, biome.Id, salt: 2000 + i);
                        var type = PickResourceType(biome.Id, hType);

                        Color c = GetResourceColor(type);
                        c.A = Alpha;

                        float r = Mathf.Lerp(DotRadiusMin, DotRadiusMax, Range01(hType ^ 0x7F4A));

                        DrawCircle(new Vector2(px, py), r, c);
                    }
                }
            }
        }



        private enum ResourceType
        {
            GenericOre,
            PlantFiber,
            Timber,
            Sand,
            Salt,
            Coral,
            Crystal,
            Sulphur,
            Toxic,
            Ice,
            Lava,
            Water,
            Glow,
            Organic,
            Clay,
            Minerals,
        }



        private static ResourceType PickResourceType(string biomeId, int h)
        {
            string id = biomeId.ToLowerInvariant();

            if (id.Contains("lava") || id.Contains("volcan") || id.Contains("cinder"))
                return Pick(h, ResourceType.Lava, ResourceType.GenericOre, ResourceType.Sulphur);

            if (id.Contains("sulphur"))
                return Pick(h, ResourceType.Sulphur, ResourceType.GenericOre, ResourceType.Toxic);

            if (id.Contains("toxic"))
                return Pick(h, ResourceType.Toxic, ResourceType.GenericOre, ResourceType.Glow);

            if (id.Contains("crystal") || id.Contains("geode") || id.Contains("quartz"))
                return Pick(h, ResourceType.Crystal, ResourceType.GenericOre, ResourceType.Glow);

            if (id.Contains("mushroom") || id.Contains("glowworm") || id.Contains("biolum"))
                return Pick(h, ResourceType.Glow, ResourceType.Organic, ResourceType.Water);

            if (id.Contains("swamp") || id.Contains("river") || id.Contains("ocean") || id.Contains("lake") || id.Contains("delta"))
                return Pick(h, ResourceType.Water, ResourceType.PlantFiber, ResourceType.GenericOre);

            if (id.Contains("reef") || id.Contains("coral"))
                return Pick(h, ResourceType.Coral, ResourceType.Salt, ResourceType.Water);

            if (id.Contains("oasis"))
                return Pick(h, ResourceType.Water, ResourceType.Sand, ResourceType.Organic);

            if (id.Contains("thermal") || id.Contains("spring"))
                return Pick(h, ResourceType.Water, ResourceType.Minerals, ResourceType.GenericOre);

            if (id.Contains("salt"))
                return Pick(h, ResourceType.Salt, ResourceType.Sand, ResourceType.GenericOre);

            if (id.Contains("desert") || id.Contains("dune") || id.Contains("badlands") || id.Contains("sand"))
                return Pick(h, ResourceType.Sand, ResourceType.GenericOre, ResourceType.Salt);

            if (id.Contains("tundra") || id.Contains("glacial") || id.Contains("ice") || id.Contains("frozen"))
                return Pick(h, ResourceType.Ice, ResourceType.GenericOre, ResourceType.Water);

            if (id.Contains("forest") || id.Contains("mangrove") || id.Contains("meadow") || id.Contains("prairie") || id.Contains("savanna"))
                return Pick(h, ResourceType.PlantFiber, ResourceType.Timber, ResourceType.GenericOre);

            if (id.Contains("storm") || id.Contains("thunder") || id.Contains("lightning"))
                return Pick(h, ResourceType.GenericOre, ResourceType.Glow, ResourceType.Water);

            // Fallback: a little variety.
            return Pick(h, ResourceType.GenericOre, ResourceType.PlantFiber, ResourceType.Water);
        }



        private static ResourceType Pick(int h, ResourceType a, ResourceType b, ResourceType c)
        {
            int v = (h & 0x7FFFFFFF) % 3;
            return v switch
            {
                0 => a,
                1 => b,
                _ => c,
            };
        }



        private static Color GetResourceColor(ResourceType t)
        {
            return t switch
            {
                ResourceType.GenericOre => new Color(0.85f, 0.85f, 0.90f, 1f),
                ResourceType.PlantFiber => new Color(0.30f, 0.85f, 0.35f, 1f),
                ResourceType.Timber => new Color(0.55f, 0.35f, 0.20f, 1f),
                ResourceType.Sand => new Color(0.95f, 0.85f, 0.50f, 1f),
                ResourceType.Salt => new Color(0.95f, 0.95f, 0.95f, 1f),
                ResourceType.Coral => new Color(1.00f, 0.45f, 0.55f, 1f),
                ResourceType.Crystal => new Color(0.55f, 0.90f, 1.00f, 1f),
                ResourceType.Sulphur => new Color(0.95f, 0.95f, 0.20f, 1f),
                ResourceType.Toxic => new Color(0.65f, 1.00f, 0.20f, 1f),
                ResourceType.Ice => new Color(0.65f, 0.80f, 1.00f, 1f),
                ResourceType.Lava => new Color(1.00f, 0.35f, 0.10f, 1f),
                ResourceType.Water => new Color(0.25f, 0.55f, 1.00f, 1f),
                ResourceType.Glow => new Color(0.75f, 0.55f, 1.00f, 1f),
                _ => new Color(1f, 1f, 1f, 1f),
            };
        }




        private static int Hash(int seed, int cx, int cy, string biomeId, int salt)
        {
            int h = seed;
            h = unchecked(h * 31 + cx);
            h = unchecked(h * 31 + cy);
            h = unchecked(h * 31 + salt);

            for (int i = 0; i < biomeId.Length; i++)
                h = unchecked(h * 31 + biomeId[i]);

            h ^= (h << 13);
            h ^= (h >> 17);
            h ^= (h << 5);

            return h;
        }



        private static float Range01(int h)
        {
            uint v = (uint)h;
            return (v & 0x00FFFFFFu) / 16777215f;
        }



        private static int RangeInt(int h, int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive)
                return minInclusive;

            int span = maxInclusive - minInclusive + 1;
            int v = (h & 0x7FFFFFFF) % span;
            return minInclusive + v;
        }
    }
}
