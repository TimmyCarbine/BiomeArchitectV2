using Godot;
using BiomeArchitectV2.Biomes.Maps;
using BiomeArchitectV2.Core;

namespace BiomeArchitectV2.Biomes.Generation
{
    public static class BiomeTileMapBuilder
    {
        public static BiomeTileMap BuildFromChunkMap(
            WorldConfig config,
            BiomeChunkMap biomeMap,
            Vector2I terrainTileSizePx
        )
        {
            int biomeChunkSizeTiles = config.BiomeChunkWorldSizePx / terrainTileSizePx.X;
            biomeChunkSizeTiles = Mathf.Max(1, biomeChunkSizeTiles);

            var outMap = new BiomeTileMap(config.TerrainWidthTiles, config.TerrainHeightTiles);

            int unassigned = 0;

            for (int y = 0; y < config.TerrainHeightTiles; y++)
            {
                int by = y / biomeChunkSizeTiles;
                by = Mathf.Clamp(by, 0, biomeMap.ChunksY - 1);
                
                for (int x = 0; x < config.TerrainWidthTiles; x++)
                {
                    int bx = x / biomeChunkSizeTiles;

                    if (config.WrapX)
                        bx = Mod(bx, biomeMap.ChunksX);
                    else
                        bx = Mathf.Clamp(bx, 0, biomeMap.ChunksX - 1);

                    int owner = biomeMap.GetBiomeIndexAtChunk(bx, by);
                    if (owner < 0)
                    {
                        outMap.Set(x, y, BiomeTileMap.UNASSIGNED);
                        unassigned++;
                        continue;
                    }

                    byte finalOwner = (byte)owner;

                    if (owner >= 0)
                    {
                        finalOwner = GetDiagonalisedOwner(
                            config,
                            biomeMap,
                            bx,
                            by,
                            x,
                            y,
                            biomeChunkSizeTiles,
                            (byte)owner
                        );
                    }

                    outMap.Set(x, y, finalOwner);
                }
            }

            GD.Print($"[BiomeTileMap] Built 1:1 from BiomeChunkMap. Size = {outMap.Width}x{outMap.Height} | biomeChunkSizeTiles = {biomeChunkSizeTiles} | biomes = {biomeMap.Biomes.Count} | unassigned = {unassigned}");

            int bordersBefore = CountBorderTiles(outMap, config);
            GD.Print($"[BiomeTileMap] Borders BEFORE smoothing = {bordersBefore}");

            //SmoothBordersMajority(outMap, config, iterations: 8);

            int bordersAfter = CountBorderTiles(outMap, config);
            GD.Print($"[BiomeTileMap] Borders AFTER smoothing = {bordersAfter}");

            return outMap;
        }



        private static int Mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }



        private static int CountBorderTiles(BiomeTileMap map, WorldConfig config)
        {
            int w = map.Width;
            int h = map.Height;
            int count = 0;

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    byte c = map.Get(x, y);
                    if (c == BiomeTileMap.UNASSIGNED)
                        continue;

                    int xl = config.WrapX ? Mod(x - 1, w) : x - 1;
                    int xr = config.WrapX ? Mod(x + 1, w) : x + 1;

                    if (xl < 0 || xr >= w)
                        continue;

                    if (map.Get(xl, y) != c ||
                        map.Get(xr, y) != c ||
                        map.Get(x, y - 1) != c ||
                        map.Get(x, y + 1) != c)
                    {
                        count++;
                    }
                }
            }

            return count;
        }



        private static void SmoothBordersMajority(BiomeTileMap map, WorldConfig config, int iterations)
        {
            int w = map.Width;
            int h = map.Height;

            byte[] src = map.Raw;
            byte[] dst = new byte[src.Length];

            System.Array.Copy(src, dst, src.Length);

            int iters = Mathf.Clamp(iterations, 1, 16);

            for (int it = 1; it <= iters; it++)
            {
                int changed = 0;
                
                for (int y = 1; y < h - 1; y++)
                {
                    int row = y * w;

                    for (int x = 0; x < w; x++)
                    {
                        int i = row + x;
                        byte c = src[i];

                        if (c == BiomeTileMap.UNASSIGNED)
                        {
                            dst[i] = c;
                            continue;
                        }

                        int xl = config.WrapX ? Mod(x - 1, w) : x - 1;
                        int xr = config.WrapX ? Mod(x + 1, w) : x + 1;

                        if (xl < 0 || xr >= w)
                        {
                            dst[i] = c;
                            continue;
                        }

                        byte l = src[row + xl];
                        byte r = src[row + xr];
                        byte u = src[(y - 1) * w + x];
                        byte d = src[(y + 1) * w + x];

                        bool isBorder = (l != c) || (r != c) || (u != c) || (d != c);
                        if (!isBorder)
                        {
                            dst[i] = c;
                            continue;
                        }

                        int oldDiff = 0;
                        if (l != c) oldDiff++;
                        if (r != c) oldDiff++;
                        if (u != c) oldDiff++;
                        if (d != c) oldDiff++;

                        byte bestCandidate = c;
                        int bestNewDiff = oldDiff;

                        void ConsiderCandidate(byte cand)
                        {
                            if (cand == c || cand == BiomeTileMap.UNASSIGNED)
                                return;

                            int newDiff = 0;
                            if (l != cand) newDiff++;
                            if (r != cand) newDiff++;
                            if (u != cand) newDiff++;
                            if (d != cand) newDiff++;

                            if (newDiff < bestNewDiff)
                            {
                                bestNewDiff = newDiff;
                                bestCandidate = cand;
                            }
                        }

                        ConsiderCandidate(l);
                        ConsiderCandidate(r);
                        ConsiderCandidate(u);
                        ConsiderCandidate(d);

                        if (bestCandidate != c && bestNewDiff < oldDiff)
                        {
                            dst[i] = bestCandidate;
                            changed++;
                            continue;
                        }

                        dst[i] = c;
                    }
                }

                var tmp = src;
                src = dst;
                dst = tmp;

                GD.Print($"[BiomeTileMap] SmoothBorders iteration = {it} | Changed = {changed}");

                if (changed == 0)
                    break;
            }

            if (!ReferenceEquals(src, map.Raw))
                System.Array.Copy(src, map.Raw, map.Raw.Length);
        }



        private static byte GetDiagonalisedOwner(
            WorldConfig config,
            BiomeChunkMap biomeMap,
            int bx,
            int by,
            int tileX,
            int tileY,
            int biomeChunkSizeTiles,
            byte ownerA
        )
        {
            int lx = tileX - (tileX / biomeChunkSizeTiles) * biomeChunkSizeTiles;
            int ly = tileY - (tileY / biomeChunkSizeTiles) * biomeChunkSizeTiles;

            int GetOwnerAt(int cx, int cy)
            {
                if (config.WrapX)
                    cx = Mod(cx, biomeMap.ChunksX);

                if ((uint)cx >= (uint)biomeMap.ChunksX || (uint)cy >= (uint)biomeMap.ChunksY)
                    return -1;

                return biomeMap.GetBiomeIndexAtChunk(cx, cy);
            }

            RegionId baseRegion = biomeMap.GetRegionAtChunkRow(by);

            bool SameRegion(int cy) => (uint)cy < (uint)biomeMap.ChunksY && biomeMap.GetRegionAtChunkRow(cy) == baseRegion;

            int east = SameRegion(by) ? GetOwnerAt(bx + 1, by) : -1;
            int west = SameRegion(by) ? GetOwnerAt(bx - 1, by) : -1;
            int south = SameRegion(by + 1) ? GetOwnerAt(bx, by + 1) : -1;
            int north = SameRegion(by - 1) ? GetOwnerAt(bx, by - 1) : -1;

            if (east >= 0 && south >= 0 && east == south && (byte)east != ownerA)
            {
                if (lx + ly >= biomeChunkSizeTiles - 1)
                    return (byte)east;
                return ownerA;
            }

            if (west >= 0 && south >= 0 && west == south && (byte)west != ownerA)
            {
                if ((biomeChunkSizeTiles - 1 - lx) + ly >= biomeChunkSizeTiles - 1)
                    return (byte)west;
                return ownerA;
            }

            if (east >= 0 && north >= 0 && east == north && (byte)east != ownerA)
            {
                if (lx + (biomeChunkSizeTiles - 1 - ly) >= biomeChunkSizeTiles - 1)
                    return (byte)east;
                return ownerA;
            }

            if (west >= 0 && north >= 0 && west == north && (byte)west != ownerA)
            {
                if ((biomeChunkSizeTiles - 1 - lx) + (biomeChunkSizeTiles - 1 - ly) >= biomeChunkSizeTiles - 1)
                    return (byte)west;
                return ownerA;
            }

            return ownerA;
        }
    }
}