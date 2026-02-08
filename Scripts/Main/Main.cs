using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Debug.Biomes;

namespace BiomeArchitectV2
{
    public sealed partial class Main : Node2D
    {
        [Export] public int TerrainWidthTiles { get; set; } = 2048;
        [Export] public int TerrainHeightTiles { get; set; } = 1024;
        [Export] public int WorldSeed { get; set; } = 12345;

        private WorldConfig _config = null!;



        public override void _Ready()
        {
            _config = new WorldConfig(TerrainWidthTiles, TerrainHeightTiles);
            var renderer = new BiomeChunkDebugRenderer
            {
                Name = "BiomeChunkDebug"
            };

            AddChild(renderer);
            renderer.Init(_config, WorldSeed);

            var cam = GetNodeOrNull<Camera2D>("Camera2D");
            if (cam != null)
            {
                cam.Position = new Vector2(0, 0);
                cam.MakeCurrent();
            }

            GD.Print($"[BiomeArchitectV2] Terrain {TerrainWidthTiles} x {TerrainHeightTiles} tiles => " +
                $"{_config.BiomeChunksX} x {_config.BiomeChunksY} biome chunks. ChunkPx = {_config.BiomeChunkWorldSizePx}");
        }
    }
}