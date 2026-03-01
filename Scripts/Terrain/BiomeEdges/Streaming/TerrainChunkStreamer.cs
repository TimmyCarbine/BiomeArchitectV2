using System.Collections.Generic;
using Godot;
using BiomeArchitectV2.Core;
using BiomeArchitectV2.Biomes.Maps;
using BiomeArchitectV2.Biomes.Generation;
using BiomeArchitectV2.Biomes.Defs;

namespace BiomeArchitectV2.Terrain.Streaming
{
    public sealed partial class TerrainChunkStreamer : Node
    {
        [Export] private TileMapLayer _terrainLayer = null!;
        [Export] private Node2D _followTarget = null!;

        [Export] public int TerrainChunkSizeTiles { get; set; } = 64;
        [Export] public int StreamRadiusChunksX { get; set; } = 3;
        [Export] public int StreamRadiusChunksY { get; set; } = 2;
        [Export] public bool AlwaysRefresh { get; set; } = false;

        [Export] public int RegionSourceId { get; set; } = 1;
        [Export] public Vector2I RegionAtlasCoords { get; set; } = new(0, 0);

        // Optional biome-specific overrides (simple keyword-based for now)
        [Export] public int StoneSourceId { get; set; } = 1;
        [Export] public Vector2I StoneAtlasCoords { get; set; } = new(1, 0);
        [Export] public int DirtSourceId { get; set; } = 1;
        [Export] public Vector2I DirtAtlasCoords { get; set; } = new(2, 0);
        [Export] public int SandSourceId { get; set; } = 1;
        [Export] public Vector2I SandAtlasCoords { get; set; } = new(3, 0);
        [Export] public int ClaySourceId { get; set; } = 1;
        [Export] public Vector2I ClayAtlasCoords { get; set; } = new(4, 0);
        [Export] public int CrystalSourceId { get; set; } = 1;
        [Export] public Vector2I CrystalAtlasCoords { get; set; } = new(5, 0);
        [Export] public int BasaltSourceId { get; set; } = 1;
        [Export] public Vector2I BasaltAtlasCoords { get; set; } = new(6, 0);
        [Export] public int CloudSourceId { get; set; } = 1;
        [Export] public Vector2I CloudAtlasCoords { get; set; } = new(7, 0);

        private ShaderMaterial _biomeTintMaterial = null!;
        private ImageTexture _paletteTex = null!;
        private ImageTexture _indexTex = null!;
        private WorldConfig _config = null!;
        private BiomeChunkMap _biomeMap = null!;
        private BiomeTileMap _biomeTiles = null!;
        private int _seed;
        private Vector2I _lastCenterChunk = new(int.MinValue, int.MaxValue);
        private readonly HashSet<Vector2I> _loadedChunks = new();
        private Vector2I _tileSizePx;
        private Vector2I _lastWindowOriginTile = new(int.MinValue, int.MinValue);
        private Vector2I _lastWindowSizeTile = Vector2I.Zero;



        public void Init(WorldConfig config, int seed, BiomeChunkMap biomeMap, BiomeTileMap biomeTiles)
        {
            _config = config;
            _seed = seed;
            _biomeMap = biomeMap;
            _biomeTiles = biomeTiles;

            _tileSizePx = GetTileSizePxOrFallback(_terrainLayer);
            EnsureBiomeTintMaterial();
            BuildOrUpdatePaletteTexture();
            _lastWindowOriginTile = new(int.MinValue, int.MinValue);
            _lastWindowSizeTile = Vector2I.Zero;

            ClearAllLoadedChunks();
            _lastCenterChunk = new(int.MinValue, int.MaxValue);
        }



        public override void _Process(double delta)
        {
            if (_config == null) return;
            if (_biomeTiles == null) return;
            if (_terrainLayer == null) return;

            Vector2I center = GetCenterTerrainChunk();

            if (!AlwaysRefresh && center == _lastCenterChunk)
                return;

            _lastCenterChunk = center;
            StreamWindow(center);
            UpdateViewOriginUniform();
            UpdateShaderViewUniforms();
        }



        public void SetFollowTarget(Node2D target)
        {
            _followTarget = target;
            _lastCenterChunk = new Vector2I(int.MinValue, int.MaxValue);
        }



        public Vector2I GetTerrainTileSizePxForBuild()
        {
            return GetTileSizePxOrFallback(_terrainLayer);
        }



        private void StreamWindow(Vector2I centerChunk)
        {
            int minX = centerChunk.X - StreamRadiusChunksX;
            int maxX = centerChunk.X + StreamRadiusChunksX;
            int minY = centerChunk.Y - StreamRadiusChunksY;
            int maxY = centerChunk.Y + StreamRadiusChunksY;

            int terrainChunksY = CeilDiv(_config.TerrainHeightTiles, TerrainChunkSizeTiles);
            minY = Mathf.Clamp(minY, 0, terrainChunksY - 1);
            maxY = Mathf.Clamp(maxY, 0, terrainChunksY - 1);

            int terrainChunksX = CeilDiv(_config.TerrainWidthTiles, TerrainChunkSizeTiles);
            if (!_config.WrapX)
            {
                minX = Mathf.Clamp(minX, 0, terrainChunksX - 1);
                maxX = Mathf.Clamp(maxX, 0, terrainChunksX - 1);
            }

            var desired = new HashSet<Vector2I>();

            for (int cy = minY; cy <= maxY; cy++)
            {
                for (int cx = minX; cx <= maxX; cx++)
                {
                    var key = new Vector2I(cx, cy);
                    desired.Add(key);

                    if (!_loadedChunks.Contains(key))
                    {
                        PaintTerrainChunk(key);
                        _loadedChunks.Add(key);
                    }
                }
            }

            var toRemove = new List<Vector2I>();
            foreach (var c in _loadedChunks)
            {
                if (!desired.Contains(c))
                    toRemove.Add(c);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                ClearTerrainChunk(toRemove[i]);
                _loadedChunks.Remove(toRemove[i]);
            }

            UpdateBiomeIndexWindow(minX, maxX, minY, maxY);
        }



        private void UpdateViewOriginUniform()
{
            // If you have a Camera2D in the scene, this is the cleanest way:
            Camera2D cam = GetViewport().GetCamera2D();

            if (cam != null)
            {
                Vector2 viewSize = GetViewport().GetVisibleRect().Size;
                Vector2 topLeftWorld = cam.GlobalPosition - (viewSize * 0.5f);

                _biomeTintMaterial.SetShaderParameter("u_view_origin_px", topLeftWorld);
                return;
            }

            _biomeTintMaterial.SetShaderParameter("u_view_origin_px", Vector2.Zero);
        }



        private void PaintTerrainChunk(Vector2I terrainChunkCoord)
        {
            GetStartEndTiles(terrainChunkCoord, out int startTileX, out int endTileX, out int startTileY, out int endTileY);

            endTileY = Mathf.Min(endTileY, _config.TerrainHeightTiles);

            if (!_config.WrapX)
            {
                if (startTileX < 0 || startTileX >= _config.TerrainWidthTiles)
                    return;

                endTileX = Mathf.Min(endTileX, _config.TerrainWidthTiles);
            }

            for (int tx = startTileX; tx < endTileX; tx++)
            {
                int dataX = _config.WrapX ? Mod(tx, _config.TerrainWidthTiles) : tx;
                int surfaceY = HeightAtx(dataX, _config.TerrainWidthTiles, _config.TerrainHeightTiles, _seed);

                for (int ty = startTileY; ty < endTileY; ty++)
                {
                    if (ty < surfaceY)
                    {
                        _terrainLayer.EraseCell(new Vector2I(tx, ty));
                        continue;
                    }

                    GetGroundTileFor(tx, ty, out int src, out Vector2I atlas, out int alt);

                    _terrainLayer.SetCell(
                        new Vector2I(tx, ty),
                        src,
                        atlas,
                        alt
                    );
                }
            }
        }



        private void ClearTerrainChunk(Vector2I terrainChunkCoord)
        {
            GetStartEndTiles(terrainChunkCoord, out int startTileX, out int endTileX, out int startTileY, out int endTileY);

            endTileY = Mathf.Min(endTileY, _config.TerrainHeightTiles);

            if (!_config.WrapX)
            {
                if (startTileX < 0 || startTileX >= _config.TerrainWidthTiles)
                    return;

                endTileX = Mathf.Min(endTileX, _config.TerrainWidthTiles);
            }

            for (int tx = startTileX; tx < endTileX; tx++)
            {
                for (int ty = startTileY; ty < endTileY; ty++)
                {
                    _terrainLayer.EraseCell(new Vector2I(tx, ty));
                }
            }
        }



        private void UpdateBiomeIndexWindow(int minChunkX, int maxChunkX, int minChunkY, int maxChunkY)
        {
            int originTileX = minChunkX * TerrainChunkSizeTiles;
            int originTileY = minChunkY * TerrainChunkSizeTiles;

            int windowChunksX = (maxChunkX - minChunkX) + 1;
            int windowChunksY = (maxChunkY - minChunkY) + 1;

            int windowTilesX = windowChunksX * TerrainChunkSizeTiles;
            int windowTilesY = windowChunksY * TerrainChunkSizeTiles;

            var origin = new Vector2I(originTileX, originTileY);
            var size = new Vector2I(windowTilesX, windowTilesY);

            if (origin == _lastWindowOriginTile && size == _lastWindowSizeTile)
                return;

            _lastWindowOriginTile = origin;
            _lastWindowSizeTile = size;

            byte[] idxBytes = new byte[windowTilesX * windowTilesY];

            int w = _config.TerrainWidthTiles;
            int h = _config.TerrainHeightTiles;

            for (int y = 0; y < windowTilesY; y++)
            {
                int worldY = originTileY + y;

                int clampedY = Mathf.Clamp(worldY, 0, h - 1);

                int row = y * windowTilesX;

                for (int x = 0; x < windowTilesX; x++)
                {
                    int worldX = originTileX + x;

                    int wx = _config.WrapX ? Mod(worldX, w) : Mathf.Clamp(worldX, 0, w - 1);

                    byte b = _biomeTiles.Get(wx, clampedY);

                    idxBytes[row + x] = (b == BiomeTileMap.UNASSIGNED) ? (byte)255 : b;
                }
            }

            var indexImage = Image.CreateFromData(
                windowTilesX,
                windowTilesY,
                false,
                Image.Format.R8,
                idxBytes
            );

            if (_indexTex == null || _indexTex.GetWidth() != windowTilesX || _indexTex.GetHeight() != windowTilesY)
                _indexTex = ImageTexture.CreateFromImage(indexImage);
            else
                _indexTex.Update(indexImage);

            _biomeTintMaterial.SetShaderParameter("u_biome_index_tex", _indexTex);
            _biomeTintMaterial.SetShaderParameter("u_window_origin_tile", (Vector2)origin);
            _biomeTintMaterial.SetShaderParameter("u_window_size_tile", (Vector2)size);
            _biomeTintMaterial.SetShaderParameter("u_tile_size_px", (Vector2)_tileSizePx);
        }



        private void GetStartEndTiles(Vector2I terrainChunkCoord, out int startTileX, out int endTileX, out int startTileY, out int endTileY)
        {
            startTileX = terrainChunkCoord.X * TerrainChunkSizeTiles;
            startTileY = terrainChunkCoord.Y * TerrainChunkSizeTiles;
            endTileX = startTileX + TerrainChunkSizeTiles;
            endTileY = startTileY + TerrainChunkSizeTiles;
        }



        private void ClearAllLoadedChunks()
        {
            foreach (var c in _loadedChunks)
                ClearTerrainChunk(c);

            _loadedChunks.Clear();
        }



        private Vector2I GetCenterTerrainChunk()
        {
            Vector2 worldPos = _followTarget != null
                ? _followTarget.GlobalPosition
                : new Vector2(_config.TerrainWidthTiles * _tileSizePx.X * 0.5f, _config.TerrainHeightTiles * _tileSizePx.Y * 0.5f);

            int tileX = Mathf.FloorToInt(worldPos.X / _tileSizePx.X);
            int tileY = Mathf.FloorToInt(worldPos.Y / _tileSizePx.Y);

            int cx = Mathf.FloorToInt((float)tileX / TerrainChunkSizeTiles);
            int cy = Mathf.FloorToInt((float)tileY / TerrainChunkSizeTiles);

            int terrainChunksY = CeilDiv(_config.TerrainHeightTiles, TerrainChunkSizeTiles);
            cy = Mathf.Clamp(cy, 0, terrainChunksY - 1);

            if (!_config.WrapX)
            {
                int terrainChunksX = CeilDiv(_config.TerrainWidthTiles, TerrainChunkSizeTiles);
                cx = Mathf.Clamp(cx, 0, terrainChunksX - 1);
            }

            return new Vector2I(cx, cy);
        }



        private static int HeightAtx(int x, int worldWidthTiles, int worldHeightTiles, int seed)
        {
            x = Mod(x, worldWidthTiles);

            float baseY = worldHeightTiles * 0.35f;
            float a1 = worldHeightTiles * 0.03f;
            float a2 = worldHeightTiles * 0.06f;
            float a3 = worldHeightTiles * 0.02f;

            float w = Mathf.Tau / worldWidthTiles;

            float p1 = seed * 0.00010f;
            float p2 = seed * 0.00023f;
            float p3 = seed * 0.00047f;

            float y =
                baseY +
                Mathf.Sin(w * 1f * x + p1) * a1 +
                Mathf.Sin(w * 3f * x + p2) * a2 +
                Mathf.Sin(w * 7f * x + p3) * a3;

            int iy = Mathf.RoundToInt(y);
            iy = Mathf.Clamp(iy, 0, worldHeightTiles - 1);

            return iy;
        }



        private static Vector2I GetTileSizePxOrFallback(TileMapLayer layer)
        {
            if (layer.TileSet != null)
            {
                Vector2I size = layer.TileSet.TileSize;
                if (size.X > 0 && size.Y > 0)
                    return size;
            }

            return new Vector2I(32, 32);
        }



        private static int CeilDiv(int a, int b) => (a + b -1) / b;



        private static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }



        private BiomeDef GetBiomeDefAtTerrainTile(int terrainTileX, int terrainTileY)
        {
            int wx = _config.WrapX ? Mod(terrainTileX, _config.TerrainWidthTiles) : terrainTileX;

            if ((uint)wx >= (uint)_config.TerrainWidthTiles)
                return null;

            if ((uint)terrainTileY >= (uint)_config.TerrainHeightTiles)
                return null;

            byte b = _biomeTiles.Get(wx, terrainTileY);
            if (b == BiomeTileMap.UNASSIGNED)
                return null;

            int idx = b;
            if ((uint)idx >= (uint)_biomeMap.Biomes.Count)
                return null;

            return _biomeMap.Biomes[idx];
        }



        private string GetBiomeIdAtTerrainTile(int terrainTileX, int terrainTileY)
        {
            var biome = GetBiomeDefAtTerrainTile(terrainTileX, terrainTileY);

            return biome?.Id ?? string.Empty;
        }



        private byte GetBiomeIndexAtTerrainTile(int terrainTileX, int terrainTileY)
        {
            int wx = _config.WrapX ? Mod(terrainTileX, _config.TerrainWidthTiles) : terrainTileX;
            wx = Mathf.Clamp(wx, 0, _config.TerrainWidthTiles - 1);
            int y = Mathf.Clamp(terrainTileY, 0, _config.TerrainHeightTiles - 1);

            return _biomeTiles.Get(wx, y);
        }



        private RegionId GetRegionAtTerrainTile(int terrainTileX, int terrainTileY)
        {
            var biome = GetBiomeDefAtTerrainTile(terrainTileX, terrainTileY);
            return biome != null ? biome.Region : RegionId.Underground;
        }



        private void GetGroundTileFor(int terrainTileX, int terrainTileY, out int sourceId, out Vector2I atlas, out int alt)
        {
            var biome = GetBiomeDefAtTerrainTile(terrainTileX, terrainTileY);

            if (biome == null)
            {
                sourceId = RegionSourceId;
                atlas = RegionAtlasCoords;
                alt = 0;
                return;
            }

            string id = biome.Id.ToLowerInvariant();

            if (id.Contains("geode") || id.Contains("stone") || id.Contains("badlands") || id.Contains("caverns"))
            {
                sourceId = StoneSourceId;
                atlas = StoneAtlasCoords;
                alt = 0;
                return;
            }
            if (id.Contains("abyssal") || id.Contains("ancient") || id.Contains("forest") || id.Contains("meadow"))
            {
                sourceId = DirtSourceId;
                atlas = DirtAtlasCoords;
                alt = 0;
                return;
            }
            if (id.Contains("desert") || id.Contains("salt") || id.Contains("oasis") || id.Contains("ashen"))
            {
                sourceId = SandSourceId;
                atlas = SandAtlasCoords;
                alt = 0;
                return;
            }
            if (id.Contains("river") || id.Contains("lake") || id.Contains("ocean") || id.Contains("swamp"))
            {
                sourceId = ClaySourceId;
                atlas = ClayAtlasCoords;
                alt = 0;
                return;
            }
            if (id.Contains("crystal") || id.Contains("aether") || id.Contains("quartz") || id.Contains("frozen"))
            {
                sourceId = CrystalSourceId;
                atlas = CrystalAtlasCoords;
                alt = 0;
                return;
            }
            if (id.Contains("lava") || id.Contains("magma") || id.Contains("basalt") || id.Contains("obsidian"))
            {
                sourceId = BasaltSourceId;
                atlas = BasaltAtlasCoords;
                alt = 0;
                return;
            }
            if (id.Contains("glowworm") || id.Contains("mushroom") || id.Contains("fossil") || id.Contains("bioluminescent"))
            {
                sourceId = CloudSourceId;
                atlas = CloudAtlasCoords;
                alt = 0;
                return;
            }

            sourceId = RegionSourceId;
            atlas = RegionAtlasCoords;
            alt = 0;
        }



        private void EnsureBiomeTintMaterial()
        {
            if (_terrainLayer.Material is ShaderMaterial existing)
            {
                _biomeTintMaterial = existing;
                return;
            }

            Shader shader = GD.Load<Shader>("res://Assets/Shaders/Terrain/BiomeTintTileMap.gdshader");

            _biomeTintMaterial = new ShaderMaterial
            {
                Shader = shader
            };

            _terrainLayer.Material = _biomeTintMaterial;
        }

        private void BuildOrUpdatePaletteTexture()
        {
            int biomeCount = _biomeMap.Biomes.Count;
            biomeCount = Mathf.Clamp(biomeCount, 1, 255);

            byte[] rgba = new byte[biomeCount * 4];

            for (int i = 0; i < biomeCount; i++)
            {
                BiomeDef biome = _biomeMap.Biomes[i];
                Color tint = ApplyRegionLighting(biome.Colour, biome.Region);

                int o = i * 4;
                rgba[o + 0] = (byte)Mathf.Clamp(Mathf.RoundToInt(tint.R * 255f), 0, 255);
                rgba[o + 1] = (byte)Mathf.Clamp(Mathf.RoundToInt(tint.G * 255f), 0, 255);
                rgba[o + 2] = (byte)Mathf.Clamp(Mathf.RoundToInt(tint.B * 255f), 0, 255);
                rgba[o + 3] = 255;
            }

            var paletteImage = Image.CreateFromData(
                biomeCount,
                1,
                false,
                Image.Format.Rgba8,
                rgba
            );

            if (_paletteTex == null)
                _paletteTex = ImageTexture.CreateFromImage(paletteImage);
            else
                _paletteTex.Update(paletteImage);

            _biomeTintMaterial.SetShaderParameter("u_palette_tex", _paletteTex);
            _biomeTintMaterial.SetShaderParameter("u_palette_size", (float)biomeCount);

            _biomeTintMaterial.SetShaderParameter("u_tile_size_px", (Vector2)_tileSizePx);
        }

        private static Color ApplyRegionLighting(Color biomeColor, RegionId region)
        {
            const float SKY_TO_WHITE = 0.65f;
            const float SURFACE_TO_WHITE = 0.05f;
            const float UNDERGROUND_TO_BLACK = 0.55f;

            return region switch
            {
                RegionId.Sky => biomeColor.Lerp(Colors.White, SKY_TO_WHITE),
                RegionId.Surface => biomeColor.Lerp(Colors.White, SURFACE_TO_WHITE),
                _ => biomeColor.Lerp(Colors.Black, UNDERGROUND_TO_BLACK),
            };
        }



        private void UpdateShaderViewUniforms()
        {
            if (_biomeTintMaterial == null)
                return;

            var vp = GetViewport();
            var cam = vp.GetCamera2D();
            Vector2 vpPx = vp.GetVisibleRect().Size;

            // Default fallback if no camera is active.
            Vector2 viewOriginWorldPx = Vector2.Zero;

            if (cam != null)
            {
                // Zoom IN => see LESS world => divide by zoom.
                Vector2 viewSizeWorldPx = new Vector2(
                    vpPx.X * cam.Zoom.X,
                    vpPx.Y * cam.Zoom.Y
                );

                // This is the key: gets the *actual* world-space centre of the screen.
                Vector2 screenCenterWorldPx = cam.GetScreenCenterPosition();

                viewOriginWorldPx = screenCenterWorldPx - (viewSizeWorldPx * 0.5f);
            }

            _biomeTintMaterial.SetShaderParameter("u_view_origin_px", viewOriginWorldPx);

            // Your TerrainLayer is at (0,0) so this is fine either way.
            _biomeTintMaterial.SetShaderParameter("u_tilemap_origin_px", _terrainLayer.GlobalPosition);
        }
    }
}