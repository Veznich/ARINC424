using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>Чистая математика отскока мяча (Domain).</summary>
    public static class BallBounceCalculator
    {
        /// <summary>
        /// Угол отскока от платформы по позиции удара.
        /// hitFactor: -1 левый край … +1 правый край.
        /// </summary>
        public static Vector3 DirectionFromPaddleHit(
            float hitFactor,
            float paddleVelocityX,
            float maxBounceAngleDeg,
            float paddleImpactMultiplier,
            float speed)
        {
            hitFactor = Mathf.Clamp(hitFactor, -1f, 1f);
            var angleRad = hitFactor * maxBounceAngleDeg * Mathf.Deg2Rad;

            // База — вверх + отклонение по X (плоскость XY)
            var dir = new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad), 0f).normalized;

            // Импульс от движения платформы
            dir.x += paddleVelocityX * paddleImpactMultiplier * 0.05f;
            dir = dir.normalized;

            // Не даём улететь почти горизонтально
            if (dir.y < 0.25f)
            {
                dir.y = 0.25f;
                dir = dir.normalized;
            }

            return dir * speed;
        }

        /// <summary>Отражение от нормали стены с небольшим разбросом угла.</summary>
        public static Vector3 ReflectOffWall(
            Vector3 velocity,
            Vector3 normal,
            float wallBounceAngleDeg,
            float minSpeed)
        {
            var reflected = Vector3.Reflect(velocity, normal.normalized);
            if (wallBounceAngleDeg > 0.01f)
            {
                var jitterDeg = Random.Range(-wallBounceAngleDeg, wallBounceAngleDeg);
                reflected = Quaternion.Euler(0f, 0f, jitterDeg) * reflected;
            }

            var speed = Mathf.Max(reflected.magnitude, minSpeed);
            return reflected.normalized * speed;
        }

        /// <summary>Нормализованный hit-factor по ширине платформы.</summary>
        public static float ComputeHitFactor(float ballX, float paddleX, float paddleHalfWidth)
        {
            if (paddleHalfWidth < 0.0001f)
            {
                return 0f;
            }

            return Mathf.Clamp((ballX - paddleX) / paddleHalfWidth, -1f, 1f);
        }
    }
}
