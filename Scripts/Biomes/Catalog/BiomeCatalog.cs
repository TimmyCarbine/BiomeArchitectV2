using Godot;
using System;
using System.Collections.Generic;
using BiomeArchitectV2.Biomes.Defs;
using BiomeArchitectV2.Biomes.Generation;

namespace BiomeArchitectV2.Biomes.Catalog
{
    public sealed class BiomeCatalog
    {
        private readonly List<BiomeDef> _all = [];
        private readonly Dictionary<RegionId, List<BiomeDef>> _byRegion = [];
        private readonly HashSet<string> _ids = [];

        public BiomeCatalog(IEnumerable<BiomeDef> defs)
        {
            foreach (BiomeDef def in defs)
            {
                Add(def);
            }
        }



        public IReadOnlyList<BiomeDef> GetAll() => _all;



        public IReadOnlyList<BiomeDef> GetByRegion(RegionId region)
        {
            if (_byRegion.TryGetValue(region, out List<BiomeDef> list))
                return list;
            
            return Array.Empty<BiomeDef>();
        }



        private void Add(BiomeDef def)
        {
            if (def.SelectionWeight <= 0)
                throw new ArgumentException($"Biome '{def.Id}' has invalid SelectionWeight ({def.SelectionWeight}). Must be > 0.");

            if (!_ids.Add(def.Id))
                throw new ArgumentException($"Duplicate biome ID '{def.Id}' in catalog.");

            _all.Add(def);

            if (!_byRegion.TryGetValue(def.Region, out List<BiomeDef> list))
            {
                list = [];
                _byRegion.Add(def.Region, list);
            }
            list.Add(def);
        }



        public static BiomeCatalog CreateDefault()
        {
            var defs = new[]
            {
                // ====== SKY ====== preferredVertical01: 0 = top of band, 1 = bottom of  band
                // Common
                new BiomeDef("Sky Meadow",              RegionId.Sky, selectionWeight: 72, new Color("#2FAE5E"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Cloud Forest",            RegionId.Sky, selectionWeight: 66, new Color("#1E8B6A"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Wind-Carved Mists",       RegionId.Sky, selectionWeight: 60, new Color("#3A78B8"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),

                // Uncommon 
                new BiomeDef("Thunderstorm Fields",     RegionId.Sky, selectionWeight: 58, new Color("#3B3F8F"), preferredVertical01: 0.45f, verticalBiasStrength01: 0.45f), // biased
                new BiomeDef("Cumulus Highlands",       RegionId.Sky, selectionWeight: 52, new Color("#2F7BA6"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Sunshower Glades",        RegionId.Sky, selectionWeight: 48, new Color("#3DBE79"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Twilight Cloudbanks",     RegionId.Sky, selectionWeight: 42, new Color("#7A4FA8"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),

                // Rare
                new BiomeDef("Gale Blossom Fields",     RegionId.Sky, selectionWeight: 36, new Color("#D14D8B"), preferredVertical01: 0.60f, verticalBiasStrength01: 0.35f), // biased
                new BiomeDef("Hailstone Front",         RegionId.Sky, selectionWeight: 34, new Color("#3E6EB6"), preferredVertical01: 0.40f, verticalBiasStrength01: 0.50f), // biased
                new BiomeDef("Ice Crystal Drift",       RegionId.Sky, selectionWeight: 28, new Color("#2AA7C7"), preferredVertical01: 0.25f, verticalBiasStrength01: 0.55f), // biased
                new BiomeDef("Sunlit Stratus",          RegionId.Sky, selectionWeight: 26, new Color("#E2B032"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Sky Mangroves",           RegionId.Sky, selectionWeight: 22, new Color("#2E8C74"), preferredVertical01: 0.75f, verticalBiasStrength01: 0.45f), // biased

                // Very Rare
                new BiomeDef("Floating Kelp Canopy",    RegionId.Sky, selectionWeight: 18, new Color("#1F9A7D"), preferredVertical01: 0.85f, verticalBiasStrength01: 0.50f), // biased
                new BiomeDef("Cirrus Spires",           RegionId.Sky, selectionWeight: 14, new Color("#6B6FE0"), preferredVertical01: 0.15f, verticalBiasStrength01: 0.65f), // biased
                new BiomeDef("Lightning Reef",          RegionId.Sky, selectionWeight: 12, new Color("#2E66D1"), preferredVertical01: 0.50f, verticalBiasStrength01: 0.55f), // biased

                // Super Rare
                new BiomeDef("Moonlit Cirque",          RegionId.Sky, selectionWeight: 10, new Color("#2B2F6C"), preferredVertical01: 0.15f, verticalBiasStrength01: 0.60f), // biased
                new BiomeDef("Stormglass Halo",         RegionId.Sky, selectionWeight:  8, new Color("#3D7F86"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),    // biased
                new BiomeDef("Aurora Veil",             RegionId.Sky, selectionWeight:  6, new Color("#21C7B0"), preferredVertical01: 0.05f, verticalBiasStrength01: 0.80f), // biased

                // Extremely Rare
                new BiomeDef("Astral Plane",            RegionId.Sky, selectionWeight:  4, new Color("#1B1E2F"), preferredVertical01: 0.10f, verticalBiasStrength01: 0.85f), // biased
                new BiomeDef("Rainbow Archipelago",     RegionId.Sky, selectionWeight:  2, new Color("#FF5A5A"), preferredVertical01: 0.35f, verticalBiasStrength01: 0.35f), // biased
                

                // ====== SURFACE ====== preferredVertical01: 0 = top of band, 1 = bottom of  band
                // Common
                new BiomeDef("Prairie",                 RegionId.Surface, selectionWeight: 78, new Color("#6ECF58"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Temperate Forest",        RegionId.Surface, selectionWeight: 74, new Color("#2E8B3D"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Meadow",                  RegionId.Surface, selectionWeight: 70, new Color("#9CDC4B"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Coniferious Forest",      RegionId.Surface, selectionWeight: 64, new Color("#1F6B3A"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),

                // Uncommon
                new BiomeDef("Savanna",                 RegionId.Surface, selectionWeight: 56, new Color("#C9B54A"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Sandy Desert",            RegionId.Surface, selectionWeight: 52, new Color("#D9B06D"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Rolling Tundra",          RegionId.Surface, selectionWeight: 44, new Color("#86C8B8"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Karst Badlands",          RegionId.Surface, selectionWeight: 40, new Color("#B7774A"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),

                // Rare
                new BiomeDef("Oasis",                   RegionId.Surface, selectionWeight: 34, new Color("#2BAA8B"), preferredVertical01: 0.70f, verticalBiasStrength01: 0.40f), // biased
                new BiomeDef("Glacial Plains",          RegionId.Surface, selectionWeight: 26, new Color("#A8E7F2"), preferredVertical01: 0.20f, verticalBiasStrength01: 0.60f), // biased
                new BiomeDef("Salt Flats",              RegionId.Surface, selectionWeight: 22, new Color("#D6DADD"), preferredVertical01: 0.80f, verticalBiasStrength01: 0.40f), // biased

                // Very Rare
                new BiomeDef("Mangrove Delta",          RegionId.Surface, selectionWeight: 18, new Color("#2F7F62"), preferredVertical01: 0.85f, verticalBiasStrength01: 0.55f), // biased
                new BiomeDef("Volcanic Highlands",      RegionId.Surface, selectionWeight: 16, new Color("#8B3A2E"), preferredVertical01: 0.25f, verticalBiasStrength01: 0.60f), // biased

                // Super Rare
                new BiomeDef("Coral Reef",              RegionId.Surface, selectionWeight:  8, new Color("#2F9BD6"), preferredVertical01: 0.95f, verticalBiasStrength01: 0.75f), // biased

                // Extrememly Rare
                new BiomeDef("Plasma Fields",           RegionId.Surface, selectionWeight:  4, new Color("#B43CFF"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),

                // ====== UNDERGROUND ====== preferredVertical01: 0 = top of band, 1 = bottom of  band
                // Common
                new BiomeDef("Geode Grotto",            RegionId.Underground, selectionWeight: 75, new Color("#B34DFF"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Ironstone Warrens",       RegionId.Underground, selectionWeight: 70, new Color("#C06A3B"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Crystal Caves",         RegionId.Underground, selectionWeight: 68, new Color("#36C9FF"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Mushroom Caves",          RegionId.Underground, selectionWeight: 62, new Color("#E04A9A"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Subterrainean River",     RegionId.Underground, selectionWeight: 60, new Color("#2E78D6"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),

                // Uncommon
                new BiomeDef("Dripstone Cathedral",     RegionId.Underground, selectionWeight: 48, new Color("#C2A56B"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Basalt Column Caves",     RegionId.Underground, selectionWeight: 44, new Color("#55606B"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Quartz Vein Hollows",     RegionId.Underground, selectionWeight: 46, new Color("#E7E7F2"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Glowworm Grottos",        RegionId.Underground, selectionWeight: 42, new Color("#B8FF3C"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),

                // Rare
                new BiomeDef("Frozen Tundra",           RegionId.Underground, selectionWeight: 34, new Color("#76D7FF"), preferredVertical01: 0.20f, verticalBiasStrength01: 0.55f), // biased
                new BiomeDef("Lava Tubes",              RegionId.Underground, selectionWeight: 30, new Color("#FF4A2E"), preferredVertical01: 0.30f, verticalBiasStrength01: 0.60f), // biased
                new BiomeDef("Sulphur Pits",            RegionId.Underground, selectionWeight: 28, new Color("#E6D428"), preferredVertical01: 0.65f, verticalBiasStrength01: 0.55f), // biased
                new BiomeDef("Toxic Caverns",           RegionId.Underground, selectionWeight: 26, new Color("#5BFF3A"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Dark Desert",             RegionId.Underground, selectionWeight: 24, new Color("#9B7B46"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Fossil Beds",             RegionId.Underground, selectionWeight: 22, new Color("#B9A26A"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),
                new BiomeDef("Ancient Root Hollows",    RegionId.Underground, selectionWeight: 20, new Color("#6DA83A"), preferredVertical01: 0f,    verticalBiasStrength01: 0f),

                // Very rare
                new BiomeDef("Underground Ocean",       RegionId.Underground, selectionWeight: 18, new Color("#1EC6D6"), preferredVertical01: 0.95f, verticalBiasStrength01: 0.80f), // biased
                new BiomeDef("Thermal Spring Caverns",  RegionId.Underground, selectionWeight: 18, new Color("#FF7A3A"), preferredVertical01: 0.55f, verticalBiasStrength01: 0.55f), // biased
                new BiomeDef("Obsidian Hollows",        RegionId.Underground, selectionWeight: 16, new Color("#7C4DFF"), preferredVertical01: 0.75f, verticalBiasStrength01: 0.55f), // biased
                new BiomeDef("Subterrainean Swamp",     RegionId.Underground, selectionWeight: 14, new Color("#3D8B6A"), preferredVertical01: 0.70f, verticalBiasStrength01: 0.45f), // biased
                new BiomeDef("Blackwater Sinkholes",    RegionId.Underground, selectionWeight: 12, new Color("#2E3F5C"), preferredVertical01: 0.85f, verticalBiasStrength01: 0.60f), // biased

                // Super rare
                new BiomeDef("Bioluminescent Lake",     RegionId.Underground, selectionWeight: 10, new Color("#2CFFB7"), preferredVertical01: 0.80f, verticalBiasStrength01: 0.65f), // biased
                new BiomeDef("Abyssal Forest",          RegionId.Underground, selectionWeight:  6, new Color("#2AA14C"), preferredVertical01: 0.90f, verticalBiasStrength01: 0.80f), // biased

                // Extrememly Rare
                new BiomeDef("Aether Tunnels",          RegionId.Underground, selectionWeight:  5, new Color("#7AE3FF"), preferredVertical01: 0.50f, verticalBiasStrength01: 0.60f), // biased
                new BiomeDef("Ashen Cinder Caves",      RegionId.Underground, selectionWeight:  4, new Color("#FF6B5B"), preferredVertical01: 0.75f, verticalBiasStrength01: 0.70f), // biased
            };

            return new BiomeCatalog(defs);
        }
    }
}