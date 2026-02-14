using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Debug.Biomes;
using BiomeArchitectV2.Debug.UI;
using BiomeArchitectV2.Biomes.Generation;
using BiomeArchitectV2.Biomes.Catalog;

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

            BiomeSelectionResult selectionResult = BiomeSelectionPipeline.Run(
                catalog,
                bands,
                _config.BiomeChunksX,
                WorldSeed
            );

            GD.Print($"[BiomeArchitectV2] Regenerated with Seed {WorldSeed} | Terrain = {TerrainWidthTiles}px * {TerrainHeightTiles}px | " +
                $"Biome Chunks = {_config.BiomeChunksX} * {_config.BiomeChunksY}");
            LogSelectionResult(selectionResult);
            GD.Print("-----------------------------------------------------------");
        }



        private void LogSelectionResult(BiomeSelectionResult result)
        {
            foreach (var region in result.Regions)
            {
                GD.Print($"[BiomeArchitectV2] {region.Region, -11} | Bands = {region.BandHeight, 2} | Area = {region.Area, 4} | Target = {region.TargetCount, 2} | " +
                        $"Selected = {region.SelectedBiomes.Count, 2} => " +
                        string.Join(", ", region.SelectedBiomes));
            }
        }
    }
}