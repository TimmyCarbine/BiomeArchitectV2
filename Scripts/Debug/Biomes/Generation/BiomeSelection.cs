using System;
using System.Collections.Generic;
using BiomeArchitectV2.Biomes.Catalog;
using BiomeArchitectV2.Biomes.Defs;

namespace BiomeArchitectV2.Biomes.Generation
{
    public static class BiomeSelection
    {
        public static List<BiomeDef> SelectForRegion(
            BiomeCatalog catalog,
            RegionId region,
            int targetCount,
            Random rng
        )
        {
            IReadOnlyList<BiomeDef> pool = catalog.GetByRegion(region);
            int count = Math.Min(targetCount, pool.Count);
            var selected = new List<BiomeDef>(count);
            var remaining = new List<BiomeDef>(pool);

            for (int i = 0; i < count; i++)
            {
                int index = PickWeightedIndex(remaining, rng);
                selected.Add(remaining[index]);
                remaining.RemoveAt(index);
            }

            selected.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            return selected;
        }



        private static int PickWeightedIndex(List<BiomeDef> list, Random rng)
        {
            int total = 0;
            for (int i = 0; i < list.Count; i++) total += list[i].SelectionWeight;

            int roll = rng.Next(0, total);
            int running = 0;
            for (int i = 0; i < list.Count; i++)
            {
                running += list[i].SelectionWeight;
                if (roll < running) return i;
            }
            return list.Count - 1;
        }
    }
}