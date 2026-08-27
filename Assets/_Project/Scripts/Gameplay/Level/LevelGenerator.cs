using System;
using System.Collections.Generic;
using Arkanoid.Configs;
using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Генерация: архетип → кандидаты → ровно N блоков.
    /// L1 ≈ 5 green; каждый уровень больше блоков;
    /// каждые 10 уровней открывается новый цвет/HP.
    /// </summary>
    public sealed class LevelGenerator
    {
        public LevelLayout Generate(int levelNumber, int seed, LevelArchetype archetype, LevelConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var level = Mathf.Max(1, levelNumber);
            var w = Mathf.Max(3, config.gridWidth);
            var h = Mathf.Max(3, config.gridHeight);
            var rng = new System.Random(seed);
            var mask = new bool[w * h];

            switch (archetype)
            {
                case LevelArchetype.Tunnel:
                    FillTunnel(mask, w, h);
                    break;
                case LevelArchetype.Fortress:
                    FillFortress(mask, w, h);
                    break;
                default:
                    FillDiamond(mask, w, h);
                    break;
            }

            var candidates = new List<int>(w * h);
            for (var i = 0; i < mask.Length; i++)
            {
                if (mask[i])
                {
                    candidates.Add(i);
                }
            }

            // Если маска слишком редкая — добиваем верхней зоной
            if (candidates.Count < 5)
            {
                FillTopBand(mask, w, h, rows: Mathf.Min(4, h));
                candidates.Clear();
                for (var i = 0; i < mask.Length; i++)
                {
                    if (mask[i])
                    {
                        candidates.Add(i);
                    }
                }
            }

            Shuffle(candidates, rng);

            var target = TargetBlockCount(level, config, candidates.Count);
            var maxTier = MaxTierForLevel(level, config);
            var bandProgress = TierBandProgress(level, config); // 0..1 внутри десятки

            var cells = new BlockSpawnData[w * h];
            for (var i = 0; i < cells.Length; i++)
            {
                var x = i % w;
                var y = i / w;
                cells[i] = new BlockSpawnData(x, y, BlockType.Empty, 0);
            }

            for (var n = 0; n < target; n++)
            {
                var idx = candidates[n];
                var x = idx % w;
                var y = idx / w;
                var hits = RollHits(rng, maxTier, bandProgress);
                var type = BlockTypeFromHits(hits);
                cells[idx] = new BlockSpawnData(x, y, type, hits);
            }

            return new LevelLayout
            {
                LevelNumber = level,
                Seed = seed,
                Archetype = archetype,
                Width = w,
                Height = h,
                CellSize = config.cellSize,
                Origin = config.gridOrigin,
                Cells = cells
            };
        }

        public static LevelArchetype PickArchetype(int seed)
        {
            return (LevelArchetype)(Mathf.Abs(seed) % 3);
        }

        public static int MaxTierForLevel(int level, LevelConfig config)
        {
            var every = config != null ? Mathf.Max(1, config.tierUnlockEveryLevels) : 10;
            var cap = config != null ? Mathf.Clamp(config.maxBlockTier, 1, 8) : 8;
            // L1–9 → 1, L10–19 → 2, …
            var tier = 1 + (Mathf.Max(1, level) - 1) / every;
            return Mathf.Clamp(tier, 1, cap);
        }

        public static int TargetBlockCount(int level, LevelConfig config, int candidateCap)
        {
            var start = config != null ? Mathf.Max(1, config.startBlockCount) : 5;
            var per = config != null ? Mathf.Max(0, config.blocksPerLevel) : 3;
            var max = config != null ? Mathf.Max(start, config.maxBlockCount) : 72;
            var count = start + (Mathf.Max(1, level) - 1) * per;
            count = Mathf.Clamp(count, 1, max);
            if (candidateCap > 0)
            {
                count = Mathf.Min(count, candidateCap);
            }

            return count;
        }

        /// <summary>0 в начале десятки, ~1 перед следующим unlock.</summary>
        private static float TierBandProgress(int level, LevelConfig config)
        {
            var every = config != null ? Mathf.Max(1, config.tierUnlockEveryLevels) : 10;
            return (Mathf.Max(1, level) - 1) % every / (float)every;
        }

        private static int RollHits(System.Random rng, int maxTier, float bandProgress)
        {
            if (maxTier <= 1)
            {
                return 1;
            }

            // Нижние лвлы чаще; ближе к unlock следующего — чаще топ-лвл
            var weights = new float[maxTier];
            var sum = 0f;
            for (var h = 1; h <= maxTier; h++)
            {
                var w = maxTier - h + 1f; // 1 HP весомее
                if (h == maxTier)
                {
                    w += 1.5f + bandProgress * 4f;
                }

                weights[h - 1] = w;
                sum += w;
            }

            var roll = (float)rng.NextDouble() * sum;
            for (var h = 1; h <= maxTier; h++)
            {
                roll -= weights[h - 1];
                if (roll <= 0f)
                {
                    return h;
                }
            }

            return maxTier;
        }

        public static BlockType BlockTypeFromHits(int hits)
        {
            if (hits <= 0)
            {
                return BlockType.Empty;
            }

            if (hits >= 8)
            {
                return BlockType.Diamond;
            }

            return (BlockType)hits;
        }

        private static void Shuffle(List<int> list, System.Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private static void FillTopBand(bool[] mask, int w, int h, int rows)
        {
            var y0 = Mathf.Max(0, h - rows);
            for (var y = y0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    mask[y * w + x] = true;
                }
            }
        }

        private static void FillTunnel(bool[] mask, int w, int h)
        {
            var leftWall = Mathf.Max(2, w / 4);
            var rightStart = w - leftWall;
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var side = x < leftWall || x >= rightStart;
                    var ceiling = y >= h - 2 && (x < leftWall + 1 || x >= rightStart - 1);
                    mask[y * w + x] = side || ceiling;
                }
            }
        }

        private static void FillFortress(bool[] mask, int w, int h)
        {
            var wallBottom = Mathf.Max(2, h / 3);
            var wallTop = h - 1;
            for (var y = wallBottom; y <= wallTop; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    mask[y * w + x] = true;
                }
            }

            for (var y = Mathf.Max(0, wallBottom - 2); y <= wallTop; y++)
            {
                mask[y * w + 0] = true;
                mask[y * w + (w - 1)] = true;
                if (w > 3)
                {
                    mask[y * w + 1] = true;
                    mask[y * w + (w - 2)] = true;
                }
            }

            var gateY = (wallBottom + wallTop) / 2;
            if (w >= 5)
            {
                mask[gateY * w + w / 2] = false;
            }
        }

        private static void FillDiamond(bool[] mask, int w, int h)
        {
            var cx = (w - 1) * 0.5f;
            var cy = (h - 1) * 0.5f;
            var maxR = Mathf.Min(w, h) * 0.45f;
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var manhattan = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
                    mask[y * w + x] = manhattan <= maxR;
                }
            }
        }
    }
}
