using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Раскладка поля: платформа у низа, потолок ниже HUD, без сильного pitch
    /// (иначе визуал кубов и XY-коллизии расходятся).
    /// </summary>
    public static class PlayfieldLayout
    {
        public const float DeathY = -7.9f;
        public const float PaddleY = -5.55f;
        /// <summary>Отступ стен от края сетки блоков — ближе к краю экрана, не к бокам блоков.</summary>
        public const float SidePad = 1.15f;
        public const float TopPad = 0.15f;
        /// <summary>Насколько выше верхних блоков мяч может лететь до отскока (к статус-бару).</summary>
        public const float CeilingAboveBlocks = 2.1f;
        /// <summary>Доля экрана сверху под статус-бар — потолок мяча = низ этой зоны.</summary>
        public const float StatusBarScreenFraction = 0.075f;

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
            var blockTop = layout.Origin.y + (layout.Height - 1) * layout.CellSize + half + TopPad;
            // Отскок у статус-бара, а не сразу над блоками
            var maxY = blockTop + CeilingAboveBlocks;
            bounds.Set(minX, maxX, DeathY, maxY);
        }

        /// <summary>Камера: низ с запасом под иконки, верх — статус-бар над потолком мяча.</summary>
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
            // Место под 3D-иконки бонусов ниже платформы
            var viewBottom = minY - 0.55f;
            var spanToCeiling = Mathf.Max(0.1f, maxY - viewBottom);
            var hudBand = spanToCeiling * StatusBarScreenFraction / (1f - StatusBarScreenFraction);
            var viewTop = maxY + hudBand;
            var centerY = (viewBottom + viewTop) * 0.5f;
            var halfH = (viewTop - viewBottom) * 0.5f;
            cam.orthographicSize = Mathf.Max(8.5f, halfH);

            const float pitch = 3f;
            cam.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            var distance = 12f;
            var rad = pitch * Mathf.Deg2Rad;
            cam.transform.position = new Vector3(
                0f,
                centerY + Mathf.Sin(rad) * distance,
                -Mathf.Cos(rad) * distance);
        }
    }
}
