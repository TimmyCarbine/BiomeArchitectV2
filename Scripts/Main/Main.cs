using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Debug.Biomes;
using BiomeArchitectV2.Biomes.Catalog;
using BiomeArchitectV2.Biomes.Generation;
using BiomeArchitectV2.Biomes.Growth;
using BiomeArchitectV2.Biomes.Seeding;
using BiomeArchitectV2.UI;

namespace BiomeArchitectV2
{
    public sealed partial class Main : Node2D
    {
        [Export] public int TerrainWidthTiles { get; set; } = 2048;
        [Export] public int TerrainHeightTiles { get; set; } = 1024;
        [Export] public int WorldSeed { get; set; } = 12345;

        [Export] private BiomeChunkDebugRenderer _renderer = null!;
        [Export] private SeedControllerUI _seedUi = null!;

        private WorldConfig _config = null!;



        public override void _Ready()
        {
            _config = new WorldConfig(TerrainWidthTiles, TerrainHeightTiles);

            _seedUi.Init(this, WorldSeed);

            RegenerateWithSeed(WorldSeed);            
        }



        public void RegenerateWithSeed(int seed)
        {
            WorldSeed = seed;

            RegionBands bands = RegionBands.Generate(_config, WorldSeed);

            _renderer.Init(_config, WorldSeed, bands);

            var catalog = BiomeCatalog.CreateDefault();

            BiomeSelectionResult selectionResult = BiomeSelectionPipeline.Run(catalog, bands, _config.BiomeChunksX, WorldSeed);
            BiomeSeedResult seedResult = BiomeSeeder.Run(selectionResult, bands, _config.BiomeChunksX, WorldSeed);
            BiomeChunkGrowthResult growthResult = BiomeChunkGrower.Run(_config, bands, seedResult);

            LogTerrainResult();
            LogSelectionResult(selectionResult);
            LogSeedResult(seedResult);
            LogGrowthResult(growthResult);

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

                    switch (region)
                    {
                        case RegionId.Sky:
                            skyTotal++;
                            break;
                        case RegionId.Surface:
                            surfaceTotal++;
                            break;
                        case RegionId.Underground:
                            undergroundTotal++;
                            break;
                        default:
                            break;
                    }
                }
            }

            for (int i = 0; i < result.Biomes.Count; i++)
            {
                GD.Print($"[BiomeArchitectV2] Growth | {result.Biomes[i].Id, -28} => {counts[i]} chunks");
            }
            GD.Print("[BiomeArchitectV2] ---------- REGION TOTALS ----------");
            GD.Print($"[BiomeArchitectV2] Sky         => {skyTotal} chunks");
            GD.Print($"[BiomeArchitectV2] Surface     => {surfaceTotal} chunks");
            GD.Print($"[BiomeArchitectV2] Underground => {undergroundTotal} chunks");

            int totalClaimed = skyTotal + surfaceTotal + undergroundTotal;
            int expectedTotal = result.ChunksX * result.ChunksY;

            GD.Print($"[BiomeArchitectV2] TOTAL CLAIMED = {totalClaimed}/{expectedTotal}");
        }
    }
}