namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Стартовый лвл блока (цвет следует за оставшимися HP при ударах):
    /// Red=3, Yellow=2, Green=1.
    /// </summary>
    public enum BlockType : byte
    {
        Empty = 0,
        Green = 1,
        Yellow = 2,
        Red = 3
    }
}
