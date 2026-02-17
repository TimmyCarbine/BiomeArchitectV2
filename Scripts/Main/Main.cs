using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Debug.Biomes;
using BiomeArchitectV2.Debug.Terrain;
using BiomeArchitectV2.Biomes.Catalog;
using BiomeArchitectV2.Biomes.Generation;
using BiomeArchitectV2.Biomes.Growth;
using BiomeArchitectV2.Biomes.Maps;
using BiomeArchitectV2.Player;
using BiomeArchitectV2.Biomes.Seeding;
using BiomeArchitectV2.Terrain.Streaming;
using BiomeArchitectV2.UI;
using System.Linq;
using System.Collections.Generic;

namespace BiomeArchitectV2
{
    public sealed partial class Main : Node2D
    {
        [Export] public int TerrainWidthTiles { get; set; } = 2048;
        [Export] public int TerrainHeightTiles { get; set; } = 1024;
        [Export] public int WorldSeed { get; set; } = 12345;

        [Export] private BiomeChunkDebugRenderer _biomeRenderer = null!;
        [Export] private SeedControllerUI _seedUi = null!;
        [Export] private TerrainStubDebugRenderer _terrainRenderer = null!;
        [Export] private TerrainChunkStreamer _terrainStreamer = null!;
        [Export] private PlayerController _player = null!;

        private WorldConfig _config = null!;



        public override void _Ready()
        {
            _config = new WorldConfig(TerrainWidthTiles, TerrainHeightTiles);

            _seedUi.Init(this, WorldSeed);

            RegenerateWithSeed(WorldSeed);   

            _player.GlobalPosition = new Vector2(200, 0);         
        }



        public void RegenerateWithSeed(int seed)
        {
            WorldSeed = seed;

            RegionBands bands = RegionBands.Generate(_config, WorldSeed);

            var catalog = BiomeCatalog.CreateDefault();

            BiomeSelectionResult selectionResult = BiomeSelectionPipeline.Run(catalog, bands, _config.BiomeChunksX, WorldSeed);
            BiomeSeedResult seedResult = BiomeSeeder.Run(selectionResult, bands, _config.BiomeChunksX, WorldSeed);
            BiomeChunkGrowthResult growthResult = BiomeChunkGrower.Run(_config, bands, seedResult);
            BiomeChunkMap biomeMap = new BiomeChunkMap(_config, bands, growthResult);

            _terrainRenderer.Init(_config, WorldSeed, biomeMap);
            _biomeRenderer.Init(_config, WorldSeed, bands, growthResult);
            _terrainStreamer.Init(_config, WorldSeed, biomeMap);

            LogTerrainResult();
            LogSelectionResult(selectionResult);
            LogSeedResult(seedResult);
            LogGrowthResult(growthResult);
            LogTerrainStubProfiles(biomeMap);

            GD.Print("[BiomeArchitectV2] -----------------------------------------------------------");
        }



        private void LogTerrainResult()
        {
            GD.Print("[BiomeArchitectV2] ===================== MAP SIZE RESULTS =====================");
            GD.Print($"[BiomeArchitectV2] Regenerated with Seed {WorldSeed, 12} | Terrain = {TerrainWidthTiles} Tiles * {TerrainHeightTiles} Tiles | " +
                $"Biome Chunks = {_config.BiomeChunksX} * {_config.BiomeChunksY}");
        }



        private void LogSelectionResult(BiomeSelectionResult result)
        {
            GD.Print("[BiomeArchitectV2] ===================== BIOME SELECTION RESULTS =====================");
            foreach (var region in result.Regions)
            {
                GD.Print($"[BiomeArchitectV2] {region.Region, -11} | Bands = {region.BandHeight, 2} | Area = {region.Area, 4} | Target = {region.TargetCount, 2} | " +
                        $"Selected = {region.SelectedBiomes.Count, 2} => {string.Join(", ", region.SelectedBiomes)}");
            }
        }



        private void LogSeedResult(BiomeSeedResult result)
        {
            GD.Print("[BiomeArchitectV2] ===================== BIOME SEED RESULTS =====================");
            foreach (var s in result.Seeds)
            {
                GD.Print($"[BiomeArchitectV2] Seed | {s.Region,-11} | {s.Biome.Id, -20} @ ({s.ChunkCoord.X},{s.ChunkCoord.Y})");
            }
        }



        private void LogGrowthResult(BiomeChunkGrowthResult result)
        {
            GD.Print("[BiomeArchitectV2] ===================== BIOME GROWTH RESULTS =====================");
            var counts = new int[result.Biomes.Count];
            int skyTotal = 0;
            int surfaceTotal = 0;
            int undergroundTotal = 0;

            for (int x = 0; x < result.ChunksX; x++)
            {
                for (int y = 0; y < result.ChunksY; y++)
                {
                    int owner = result.Owners[x, y];
                    if (owner < 0 || owner >= counts.Length)
                        continue;

                    counts[owner]++;

                    RegionId region = result.Biomes[owner].Region;

                    if (region == RegionId.Sky)
                        skyTotal++;
                    else if (region == RegionId.Surface)
                        surfaceTotal++;
                    else if (region == RegionId.Underground)
                        undergroundTotal++;
                }
            }

            for (int i = 0; i < result.Biomes.Count; i++)
            {
                int regionCount = result.Biomes[i].Region switch
                {
                    RegionId.Sky => regionCount = skyTotal,
                    RegionId.Surface => regionCount = surfaceTotal,
                    _ => regionCount = undergroundTotal
                };
                GD.Print($"[BiomeArchitectV2] Growth | {result.Biomes[i].Id, -28} => {counts[i], 3} chunks" +
                    $" | {GetOwnershipPercentage(counts[i], regionCount), 3}% {result.Biomes[i].Region} Ownership");
            }

            int totalClaimed = skyTotal + surfaceTotal + undergroundTotal;
            int expectedTotal = result.ChunksX * result.ChunksY;

            GD.Print("[BiomeArchitectV2] ---------- REGION TOTALS ----------");
            GD.Print($"[BiomeArchitectV2] Sky           => {skyTotal, 4} chunks | {GetOwnershipPercentage(skyTotal, expectedTotal), 3}% Map Ownership");
            GD.Print($"[BiomeArchitectV2] Surface       => {surfaceTotal, 4} chunks | {GetOwnershipPercentage(surfaceTotal, expectedTotal), 3}% Map Ownership");
            GD.Print($"[BiomeArchitectV2] Underground   => {undergroundTotal, 4} chunks | {GetOwnershipPercentage(undergroundTotal, expectedTotal), 3}% Map Ownership");
            GD.Print($"[BiomeArchitectV2] TOTAL CLAIMED => {totalClaimed}/{expectedTotal}");
        }



        private int GetOwnershipPercentage(int chunks, int total)
        {
            return chunks * 100 / total;
        }



        private void LogTerrainStubProfiles(BiomeChunkMap map)
        {
            GD.Print("[BiomeArchitectV2] ===================== TERRAIN STUB PROFILES =====================");

            var counts = new Dictionary<string, int>();

            for (int cx = 0; cx < map.ChunksX; cx++)
            {
                for (int cy = 0; cy < map.ChunksY; cy++)
                {
                    var biome = map.GetBiomeAtChunk(cx, cy);
                    if (biome == null) continue;

                    if (!counts.ContainsKey(biome.Id))
                        counts[biome.Id] = 0;

                    counts[biome.Id]++;
                }
            }

            foreach (var kvp in counts.OrderByDescending(k => k.Value))
            {
                string profile = GetTerrainProfileForBiome(kvp.Key);

                GD.Print($"[BiomeArchitectV2] Terrain | {kvp.Key,-28} | Chunks = {kvp.Value,3} | Profile = {profile}");
            }
        }



        private string GetTerrainProfileForBiome(string biomeId)
        {
            string id = biomeId.ToLowerInvariant();

            if (id.Contains("lava") || id.Contains("volcan") || id.Contains("cinder"))
                return "High Lava / Basalt / Rare Ore";

            if (id.Contains("crystal") || id.Contains("geode") || id.Contains("quartz"))
                return "Crystal-Rich / Medium Ore";

            if (id.Contains("forest") || id.Contains("meadow") || id.Contains("prairie"))
                return "High Organic / Timber / Light Ore";

            if (id.Contains("desert") || id.Contains("sand") || id.Contains("badlands"))
                return "Sand / Salt / Sparse Ore";

            if (id.Contains("swamp") || id.Contains("river") || id.Contains("ocean"))
                return "Water-Heavy / Clay / Organic";

            if (id.Contains("tundra") || id.Contains("glacial") || id.Contains("ice"))
                return "Ice / Low Organic / Medium Ore";

            if (id.Contains("oasis"))
                return "Water-Heavy / Clay / Organic";

            if (id.Contains("mushroom") || id.Contains("glowworm") || id.Contains("biolum"))
                return "Glow / Organic / Water";

            if (id.Contains("thermal") || id.Contains("spring"))
                return "Water-Heavy / Minerals / Heat";

            return "Mixed Generic Resources";
        }
    }
}