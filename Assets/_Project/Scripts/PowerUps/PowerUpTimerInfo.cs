namespace Arkanoid.Gameplay
{
    /// <summary>Один активный таймер бонуса для HUD.</summary>
    public struct PowerUpTimerInfo
    {
        public PowerUpType Type;
        public float Remaining;
        public float Duration;
        public bool IsInstant;

        public float Normalized => Duration > 0.001f ? Remaining / Duration : 0f;
    }
}
