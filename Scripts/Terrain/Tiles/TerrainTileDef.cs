using Godot;

namespace BiomeArchitectV2.Terrain.Tiles
{
    public readonly struct TerrainTileDef
    {
        public readonly int SourceId;
        public readonly Vector2I AtlasCoords;
        public readonly int Alternative;



        public TerrainTileDef(int sourceId, Vector2I atlasCoords, int alternative)
        {
            SourceId = sourceId;
            AtlasCoords = atlasCoords;
            Alternative = alternative;
        }
    }
}