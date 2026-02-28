using System;
using System.Data;

namespace BiomeArchitectV2.Biomes.Maps
{
    public sealed class BiomeTileMap
    {
        public const byte UNASSIGNED = 255;

        public int Width { get; }
        public int Height { get; }

        private readonly byte[] _biomeMap;



        public BiomeTileMap(int width, int height)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0, nameof(width));
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0, nameof(height));

            Width = width;
            Height = height;

            _biomeMap = new byte[width * height];
            Array.Fill(_biomeMap, UNASSIGNED);
        }
        


        public byte Get(int x, int y) => _biomeMap[y * Width + x];
        public byte Set(int x, int y, byte biomeIndex) => _biomeMap[y * Width + x] = biomeIndex;

        public byte[] Raw => _biomeMap;
    }
}