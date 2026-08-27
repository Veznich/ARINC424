namespace Arkanoid.Gameplay
{
    /// <summary>Одна ячейка сгенерированного уровня.</summary>
    public struct BlockSpawnData
    {
        public int X;
        public int Y;
        public BlockType Type;
        public int Hits;

        public BlockSpawnData(int x, int y, BlockType type, int hits)
        {
            X = x;
            Y = y;
            Type = type;
            Hits = hits;
        }
    }

    /// <summary>Детерминированный результат LevelGenerator.</summary>
    public sealed class LevelLayout
    {
        public int LevelNumber;
        public int Seed;
        public LevelArchetype Archetype;
        public int Width;
        public int Height;
        public float CellSize;
        public UnityEngine.Vector3 Origin;
        public BlockSpawnData[] Cells;

        public int OccupiedCount
        {
            get
            {
                var n = 0;
                if (Cells == null)
                {
                    return 0;
                }

                for (var i = 0; i < Cells.Length; i++)
                {
                    if (Cells[i].Type != BlockType.Empty)
                    {
                        n++;
                    }
                }

                return n;
            }
        }
    }
}
