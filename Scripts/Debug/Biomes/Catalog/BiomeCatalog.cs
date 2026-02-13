using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                new BiomeDef("Sky Meadow", RegionId.Sky, 40),
                new BiomeDef("Cloud Forest", RegionId.Sky, 30),
                new BiomeDef("Thunderstorm Fields", RegionId.Sky, 20),
                new BiomeDef("Rainbow Archipelago", RegionId.Sky, 15),
                new BiomeDef("Astral Plane", RegionId.Sky, 10),

                // --- Surface Biomes ---
                new BiomeDef("Prairie", RegionId.Surface, 45),
                new BiomeDef("Temperate Forest", RegionId.Surface, 40),
                new BiomeDef("Sandy Desert", RegionId.Surface, 25),
                new BiomeDef("Rolling Tundra", RegionId.Surface, 20),
                new BiomeDef("Coral Reef", RegionId.Surface, 10),
                new BiomeDef("Oasis", RegionId.Surface, 15),
                new BiomeDef("Coniferious Forest", RegionId.Surface, 30),
                new BiomeDef("Meadow", RegionId.Surface, 35),
                new BiomeDef("Glacial Plains", RegionId.Surface, 15),
                new BiomeDef("Plasma Fields", RegionId.Surface, 8),

                // --- Underground Biomes ---
                new BiomeDef("Crystal Caverns", RegionId.Underground, 25),
                new BiomeDef("Mushroom Caves", RegionId.Underground, 25),
                new BiomeDef("Frozen Tundra", RegionId.Underground, 20),
                new BiomeDef("Subterrainean Swamp", RegionId.Underground, 20),
                new BiomeDef("Toxic Caverns", RegionId.Underground, 12),
                new BiomeDef("Lava Tubes", RegionId.Underground, 10),
                new BiomeDef("Abyssal Forest", RegionId.Underground, 10),
                new BiomeDef("Aether Tunnels", RegionId.Underground, 10),
                new BiomeDef("Sulphur Pits", RegionId.Underground, 12),
                new BiomeDef("Underground Ocean", RegionId.Underground, 8),
                new BiomeDef("Subterrainean River", RegionId.Underground, 18),
                new BiomeDef("Dark Desert", RegionId.Underground, 12),
            };
            return new BiomeCatalog(defs);
        }
    }
}