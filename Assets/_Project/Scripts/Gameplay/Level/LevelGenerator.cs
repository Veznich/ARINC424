using System;
using Arkanoid.Configs;
using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Детерминированная генерация раскладки по seed + архетипу.
    /// Не знает про Unity-сцену / пул — только данные.
    /// </summary>
    public sealed class LevelGenerator
    {
        public LevelLayout Generate(int levelNumber, int seed, LevelArchetype archetype, LevelConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

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

            var cells = new BlockSpawnData[w * h];
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var idx = y * w + x;
                    if (!mask[idx])
                    {
                        cells[idx] = new BlockSpawnData(x, y, BlockType.Empty, 0);
                        continue;
                    }

                    var type = RollType(rng, config);
                    var hits = HitsFor(type, rng, config);
                    cells[idx] = new BlockSpawnData(x, y, type, hits);
                }
            }

            return new LevelLayout
            {
                LevelNumber = levelNumber,
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

            // Башни по краям
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

        private static BlockType RollType(System.Random rng, LevelConfig config)
        {
            var green = config.weightGreen > 0f ? config.weightGreen : config.weightNormal;
            var yellow = config.weightYellow;
            var red = config.weightRed > 0f ? config.weightRed : config.weightHard;
            var sum = green + yellow + red;
            if (sum <= 0f)
            {
                return BlockType.Green;
            }

            var roll = (float)rng.NextDouble() * sum;
            if (roll < green)
            {
                return BlockType.Green;
            }

            roll -= green;
            if (roll < yellow)
            {
                return BlockType.Yellow;
            }

            return BlockType.Red;
        }

        private static int HitsFor(BlockType type, System.Random rng, LevelConfig config)
        {
            switch (type)
            {
                case BlockType.Yellow:
                    return 2;
                case BlockType.Red:
                    return 3;
                case BlockType.Empty:
                    return 0;
                default:
                    return 1; // Green
            }
        }
    }
}
