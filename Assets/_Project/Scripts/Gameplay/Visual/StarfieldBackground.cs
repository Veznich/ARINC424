using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Тёмный звёздный фон. Звёзды приглушённые — не отвлекают от геймплея.
    /// </summary>
    public sealed class StarfieldBackground : MonoBehaviour
    {
        [SerializeField]
        private int starCount = 160;

        [SerializeField]
        [Range(0.05f, 0.5f)]
        private float starBrightness = 0.16f;

        [SerializeField]
        private float starSize = 0.04f;

        [SerializeField]
        private Color skyColor = new Color(0.015f, 0.02f, 0.045f, 1f);

        [SerializeField]
        private Vector2 fieldSize = new Vector2(16f, 22f);

        private bool _built;

        private void Awake()
        {
            EnsureBuilt();
        }

        /// <summary>Собрать небо + частицы (идемпотентно).</summary>
        public void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            BuildSky();
            BuildStars();
        }

        private void BuildSky()
        {
            var existing = transform.Find("SkyQuad");
            GameObject skyGo;
            if (existing != null)
            {
                skyGo = existing.gameObject;
            }
            else
            {
                skyGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                skyGo.name = "SkyQuad";
                skyGo.transform.SetParent(transform, false);
                DestroyCollider(skyGo);
            }

            skyGo.transform.localPosition = new Vector3(0f, 1.5f, 3.5f);
            skyGo.transform.localRotation = Quaternion.identity;
            skyGo.transform.localScale = new Vector3(fieldSize.x, fieldSize.y, 1f);

            var renderer = skyGo.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = Utils.RuntimeMaterialUtil.CreateColored(skyColor);
                if (mat != null)
                {
                    renderer.sharedMaterial = mat;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        private void BuildStars()
        {
            var existing = transform.Find("Stars");
            GameObject starsGo;
            if (existing != null)
            {
                starsGo = existing.gameObject;
                var oldPs = starsGo.GetComponent<ParticleSystem>();
                if (oldPs != null)
                {
                    Destroy(oldPs);
                }
            }
            else
            {
                starsGo = new GameObject("Stars");
                starsGo.transform.SetParent(transform, false);
            }

            starsGo.transform.localPosition = new Vector3(0f, 1.5f, 3.2f);

            var ps = starsGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 100000f;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = starCount;
            main.startSize = starSize;
            // Приглушённый белый/голубой — не «неоновые вспышки»
            var c = new Color(0.75f, 0.82f, 1f, starBrightness);
            main.startColor = c;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)starCount) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(fieldSize.x * 0.9f, fieldSize.y * 0.9f, 0.1f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.7f, 0.78f, 1f), 0f),
                    new GradientColorKey(new Color(0.85f, 0.88f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(starBrightness * 0.55f, 0f),
                    new GradientAlphaKey(starBrightness, 0.45f),
                    new GradientAlphaKey(starBrightness * 0.5f, 1f)
                });
            colorOverLifetime.color = grad;

            var renderer = starsGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;

            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                 ?? Shader.Find("Particles/Standard Unlit")
                                 ?? Shader.Find("Sprites/Default");
            if (particleShader != null)
            {
                var mat = new Material(particleShader);
                var dim = new Color(0.75f, 0.82f, 1f, starBrightness);
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", dim);
                }

                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", dim);
                }

                renderer.sharedMaterial = mat;
            }

            ps.Clear();
            ps.Play();
        }

        private static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
        }
    }
}
