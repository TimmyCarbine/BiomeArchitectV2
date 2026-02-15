using System;
using System.Collections.Generic;
using Godot;
using BiomeArchitectV2.Biomes.Defs;
using BiomeArchitectV2.Biomes.Generation;
using BiomeArchitectV2.Biomes.Seeding;
using BiomeArchitectV2.Core;
using System.Text;

namespace BiomeArchitectV2.Biomes.Growth
{
    public static class BiomeChunkGrower
    {
        private const int EMPTY_OWNER = -1;

        private static readonly Vector2I[] NeighbourDirs =
        {
            new Vector2I(-1, 0),
            new Vector2I(+1, 0),
            new Vector2I(0, -1),
            new Vector2I(0, +1),
        };



        public static BiomeChunkGrowthResult Run(
            WorldConfig config,
            RegionBands bands,
            BiomeSeedResult seeds
        )
        {
            int chunksX = config.BiomeChunksX;
            int chunksY = config.BiomeChunksY;

            int[,] owners = new int[chunksX, chunksY];
            for (int x = 0; x < chunksX; x++)
            {
                for (int y = 0; y < chunksY; y++)
                {
                    owners[x, y] = EMPTY_OWNER;
                }
            }

            var biomeList = new List<BiomeDef>(seeds.Seeds.Count);
            var frontiers = new List<Queue<int>>(seeds.Seeds.Count);
            var frontierSets = new List<HashSet<int>>(seeds.Seeds.Count);

            int claimed = 0;
            for (int i = 0; i < seeds.Seeds.Count; i++)
            {
                BiomeSeedData seed = seeds.Seeds[i];
                biomeList.Add(seed.Biome);
                frontiers.Add(new Queue<int>(capacity: 64));
                frontierSets.Add(new HashSet<int>());

                int sx = seed.ChunkCoord.X;
                int sy = seed.ChunkCoord.Y;

                if (!IsInBounds(sx, sy, chunksX, chunksY))
                    continue;
                
                if (owners[sx, sy] != EMPTY_OWNER)
                    continue;

                owners[sx, sy] = i;
                claimed++;

                EnqueueNeighbours(config, bands, seed.Region, sx, sy, i, owners, chunksX, chunksY, frontiers[i], frontierSets[i]);
            }

            int total = chunksX * chunksY;
            int safetyIterations = total * 16;
            int iter = 0;

            while (claimed < total)
            {
                iter++;
                if (iter > safetyIterations)
                {
                    GD.PushError($"[BiomeArchitectV2] Growth safety break. Claimed {claimed}/{total}. Check region bounds / seeding.");
                    break;
                }

                bool anyClaimedThisRound = false;

                for (int biomeIndex = 0; biomeIndex < biomeList.Count; biomeIndex++)
                {
                    if (!TryDequeueNextClaimable(frontiers[biomeIndex], frontierSets[biomeIndex], owners, chunksX, out int cell))
                        continue;

                    Decode(cell, chunksX, out int x, out int y);

                    owners[x, y] = biomeIndex;
                    claimed++;
                    anyClaimedThisRound = true;

                    RegionId region = seeds.Seeds[biomeIndex].Region;
                    EnqueueNeighbours(config, bands, region, x, y, biomeIndex, owners, chunksX, chunksY, frontiers[biomeIndex], frontierSets[biomeIndex]);

                    if (claimed >= total)
                        break;
                }

                if (!anyClaimedThisRound)
                {
                    GD.PushError($"[BiomeArchitectV2] Growth stalled. Claimed {claimed}/{total}. Likely unreachable cells due to region constraints.");
                    break;
                }
            }

            return new BiomeChunkGrowthResult(chunksX, chunksY, owners, biomeList);
        }



        private static bool TryDequeueNextClaimable(Queue<int> q, HashSet<int> frontierSet, int[,] owners, int chunksX, out int cell)
        {
            while (q.Count > 0)
            {
                int c = q.Dequeue();
                frontierSet.Remove(c);
                Decode(c, chunksX, out int x, out int y);
                if (owners[x, y] == EMPTY_OWNER)
                {
                    cell = c;
                    return true;
                }
            }

            cell = 0;
            return false;
        }



        private static void EnqueueNeighbours(
            WorldConfig config,
            RegionBands bands,
            RegionId region,
            int x,
            int y,
            int biomeIndex,
            int[,] owners,
            int chunksX,
            int chunksY,
            Queue<int> frontier,
            HashSet<int> frontierSet
        )
        {
            GetRegionYBounds(bands, region, out int minY, out int maxY);

            for (int i = 0; i < NeighbourDirs.Length; i++)
            {
                Vector2I d = NeighbourDirs[i];
                int nx = x + d.X;
                int ny = y + d.Y;

                if (config.WrapX)
                    nx = WrapX(nx, chunksX);

                if (!IsInBounds(nx, ny, chunksX, chunksY))
                    continue;
                
                if (ny < minY || ny > maxY)
                    continue;

                if (owners[nx, ny] != EMPTY_OWNER)
                    continue;

                int enc = Encode(nx, ny, chunksX);

                if (frontierSet.Add(enc))
                    frontier.Enqueue(enc);
            }
        }



        private static void GetRegionYBounds(RegionBands bands, RegionId region, out int minY, out int maxY)
        {
            if (region == RegionId.Sky)
            {
                minY = 0;
                maxY = bands.SkyHeightChunks - 1;
                return;
            }

            if (region == RegionId.Surface)
            {
                minY = bands.SkyHeightChunks;
                maxY = bands.SkyHeightChunks + bands.SurfaceHeightChunks -1;
                return;
            }

            minY = bands.SkyHeightChunks + bands.SurfaceHeightChunks;
            maxY = minY + bands.UndergroundHeightChunks - 1;
        }



        private static int WrapX(int x, int width)
        {
            int m = x % width;
            return m < 0 ? m + width : m;
        }



        private static bool IsInBounds(int x, int y, int w, int h)
        {
            return x >= 0 && x < w && y >= 0 && y < h;
        }



        private static int Encode(int x, int y, int chunksX)
        {
            return (y * chunksX) + x;
        }



        private static void Decode(int enc, int chunksX, out int x, out int y)
        {
            y = enc / chunksX;
            x = enc - (y * chunksX);
        }
    }
}