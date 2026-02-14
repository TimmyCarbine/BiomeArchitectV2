using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Debug.Biomes;
using BiomeArchitectV2.Biomes.Catalog;
using BiomeArchitectV2.Biomes.Generation;
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

            LogTerrainResult();
            LogSelectionResult(selectionResult);
            LogSeedResult(seedResult);

            GD.Print("-----------------------------------------------------------");
        }



        private void LogTerrainResult()
        {
            GD.Print($"[BiomeArchitectV2] Regenerated with Seed {WorldSeed, 12} | Terrain = {TerrainWidthTiles} Tiles * {TerrainHeightTiles} Tiles | " +
                $"Biome Chunks = {_config.BiomeChunksX} * {_config.BiomeChunksY}");
        }



        private void LogSelectionResult(BiomeSelectionResult result)
        {
            foreach (var region in result.Regions)
            {
                GD.Print($"[BiomeArchitectV2] {region.Region, -11} | Bands = {region.BandHeight, 2} | Area = {region.Area, 4} | Target = {region.TargetCount, 2} | " +
                        $"Selected = {region.SelectedBiomes.Count, 2} => {string.Join(", ", region.SelectedBiomes)}");
            }
        }



        private void LogSeedResult(BiomeSeedResult result)
        {
            foreach (var s in result.Seeds)
            {
                GD.Print($"[BiomeArchitectV2] Seed | {s.Region,-11} | {s.Biome.Id, -20} @ ({s.ChunkCoord.X},{s.ChunkCoord.Y})");
            }
        }
    }
}