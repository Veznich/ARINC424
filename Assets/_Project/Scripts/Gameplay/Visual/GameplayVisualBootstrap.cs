using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Псевдо-3D + звёздный фон. Кадр поля — через PlayfieldLayout (платформа внизу, HUD сверху).
    /// </summary>
    public static class GameplayVisualBootstrap
    {
        public const float PaddleDepth = 0.7f;
        public const float BallEmission = 0.22f;
        public const float PaddleEmission = 0.16f;
        public const float BlockEmission = 0.12f;

        public static void Apply(
            Transform arenaRoot,
            Camera cam,
            PaddleController paddle,
            BallController ball,
            PlayfieldBounds bounds = null)
        {
            if (arenaRoot == null)
            {
                return;
            }

            PlayfieldLayout.ApplyPaddlePosition(paddle);
            PlayfieldLayout.ConfigureCamera(cam, bounds);
            ConfigureLight();
            EnsureStarfield(arenaRoot);
            StylePaddle(paddle);
            StyleBall(ball);

            var oldBg = arenaRoot.Find("PlayfieldBackground");
            if (oldBg != null)
            {
                oldBg.gameObject.SetActive(false);
            }
        }

        private static void ConfigureLight()
        {
            var light = Object.FindAnyObjectByType<Light>();
            if (light == null || light.type != LightType.Directional)
            {
                var go = new GameObject("Directional Light");
                light = go.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(48f, -42f, 0f);
            light.intensity = 1.35f;
            light.color = new Color(0.95f, 0.97f, 1f);
            light.shadows = LightShadows.Soft;
        }

        private static void EnsureStarfield(Transform arenaRoot)
        {
            var existing = arenaRoot.GetComponentInChildren<StarfieldBackground>(true);
            if (existing == null)
            {
                var go = new GameObject("Starfield");
                go.transform.SetParent(arenaRoot, false);
                existing = go.AddComponent<StarfieldBackground>();
            }

            existing.EnsureBuilt();
        }

        private static void StylePaddle(PaddleController paddle)
        {
            if (paddle == null)
            {
                return;
            }

            var t = paddle.transform;
            var s = t.localScale;
            s.z = Mathf.Max(PaddleDepth, s.y * 1.6f);
            t.localScale = s;
            ApplyRenderer(paddle.gameObject, new Color(0.2f, 0.95f, 1f), PaddleEmission, sphere: false);
        }

        private static void StyleBall(BallController ball)
        {
            if (ball == null)
            {
                return;
            }

            ApplyRenderer(ball.gameObject, new Color(1f, 0.25f, 0.9f), BallEmission, sphere: true);
        }

        private static void ApplyRenderer(GameObject go, Color color, float emission, bool sphere)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            var mat = sphere
                ? Utils.RuntimeMaterialUtil.CreatePseudo3dSphere(color, emission)
                : Utils.RuntimeMaterialUtil.CreatePseudo3d(color, emission);
            if (mat != null)
            {
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }
    }
}
