using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Раскладка поля: платформа у низа, потолок ниже HUD, без сильного pitch
    /// (иначе визуал кубов и XY-коллизии расходятся).
    /// </summary>
    public static class PlayfieldLayout
    {
        public const float DeathY = -8.6f;
        public const float PaddleY = -7.85f;
        public const float HudWorldMargin = 1.35f;
        public const float SidePad = 0.08f;
        public const float TopPad = 0.25f;

        public static void ApplyPaddlePosition(PaddleController paddle)
        {
            if (paddle == null)
            {
                return;
            }

            var p = paddle.transform.position;
            paddle.transform.position = new Vector3(p.x, PaddleY, 0f);
        }

        public static void FitBounds(PlayfieldBounds bounds, LevelLayout layout, float blockScale)
        {
            if (bounds == null || layout == null)
            {
                return;
            }

            var half = layout.CellSize * Mathf.Clamp(blockScale, 0.5f, 1f) * 0.5f;
            var minX = layout.Origin.x - half - SidePad;
            var maxX = layout.Origin.x + (layout.Width - 1) * layout.CellSize + half + SidePad;
            var maxY = layout.Origin.y + (layout.Height - 1) * layout.CellSize + half + TopPad;
            bounds.Set(minX, maxX, DeathY, maxY);
        }

        /// <summary>Камера: почти фронт (лёгкий pitch), низ у платформы, верх с запасом под статус-бар.</summary>
        public static void ConfigureCamera(Camera cam, PlayfieldBounds bounds)
        {
            if (cam == null)
            {
                return;
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.015f, 0.03f, 1f);
            cam.orthographic = true;

            var minY = bounds != null ? bounds.MinY : DeathY;
            var maxY = bounds != null ? bounds.MaxY : 8f;
            // Верх кадра выше потолка поля + HUD, низ чуть ниже death
            var viewBottom = minY - 0.35f;
            var viewTop = maxY + HudWorldMargin;
            var centerY = (viewBottom + viewTop) * 0.5f;
            var halfH = (viewTop - viewBottom) * 0.5f;
            cam.orthographicSize = Mathf.Max(8.5f, halfH);

            // Минимальный pitch — объём читается bevel'ом, коллизии = экран
            const float pitch = 6f;
            cam.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            // Камера смотрит примерно в центр поля
            var distance = 12f;
            var rad = pitch * Mathf.Deg2Rad;
            cam.transform.position = new Vector3(
                0f,
                centerY + Mathf.Sin(rad) * distance,
                -Mathf.Cos(rad) * distance);
        }
    }
}
