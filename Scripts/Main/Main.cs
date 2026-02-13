using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Debug.Biomes;
using BiomeArchitectV2.Debug.UI;
using BiomeArchitectV2.Biomes.Generation;
using BiomeArchitectV2.Biomes.Catalog;
using System.Collections.Generic;
using System.ComponentModel;

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
            var settings = RegionBiomeCountSettingsProvider.CreateDefaults();
            var selectionRng = new System.Random(unchecked(seed * 92821 + 1337));

            LogRegionSelections(catalog, settings, bands, selectionRng);

            GD.Print($"[BiomeArchitectV2] Regenerated with Seed = {WorldSeed} | Terrain = {TerrainWidthTiles}px * {TerrainHeightTiles}px | " +
                $"Biome Chunks = {_config.BiomeChunksX} * {_config.BiomeChunksY}");
        }



        private void LogRegionSelections(
            BiomeCatalog catalog,
            IReadOnlyDictionary<RegionId, RegionBiomeCountSettings> settings,
            RegionBands bands,
            System.Random rng
        )
        {
            LogRegion(RegionId.Sky, bands.SkyHeightChunks);
            LogRegion(RegionId.Surface, bands.SurfaceHeightChunks);
            LogRegion(RegionId.Underground, bands.UndergroundHeightChunks);

            void LogRegion(RegionId region, int bandHeight)
            {
                int regionArea = _config.BiomeChunksX * bandHeight;

                int target = settings[region].GetTargetCount(
                    chunksX: _config.BiomeChunksX,
                    bandHeightChunks: bandHeight,
                    regionAreaChunks: regionArea,
                    rng: rng
                );

                var picked = BiomeSelection.SelectForRegion(
                    catalog: catalog,
                    region: region,
                    targetCount: target,
                    rng: rng
                );

                GD.Print($"[BiomeArchitectV2] {region, -11} | Band = {bandHeight, 2} | Area = {regionArea, 4} | " + 
                    $"Target = {target, 2} | Selected = {picked.Count, 2} => " +
                    string.Join(", ", picked.ConvertAll(b => b.Id)));
            }
        }
    }
}