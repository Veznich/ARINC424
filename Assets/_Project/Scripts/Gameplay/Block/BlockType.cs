namespace Arkanoid.Gameplay
{
    /// <summary>
    /// HP = цвет. При ударе спускается на ступень ниже.
    /// 1 зелёный → 2 жёлтый → 3 красный → 4 синий → 5 чёрный → 6 медный → 7 железный → 8 бриллиант.
    /// Новый макс. лвл открывается каждые 10 уровней.
    /// </summary>
    public enum BlockType : byte
    {
        Empty = 0,
        Green = 1,
        Yellow = 2,
        Red = 3,
        Blue = 4,
        Black = 5,
        Copper = 6,
        Iron = 7,
        Diamond = 8
    }
}
