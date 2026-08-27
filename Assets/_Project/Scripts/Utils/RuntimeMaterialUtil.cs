using UnityEngine;
using UnityEngine.Rendering;

namespace Arkanoid.Utils
{
    /// <summary>
    /// Runtime-материалы: псевдо-3D (Lit + bevel map + emission).
    /// Кубы при виде спереди иначе выглядят плоскими — albedo-bevel даёт грани.
    /// </summary>
    public static class RuntimeMaterialUtil
    {
        private static Texture2D _bevelTex;
        private static Texture2D _sphereShadeTex;

        /// <summary>Плоский цвет (фон / fallback).</summary>
        public static Material CreateColored(Color color)
        {
            return CreateInternal(color, lit: false, emission: 0f, useBevelMap: false, sphereShade: false);
        }

        /// <summary>Lit + soft emission — имитация объёма.</summary>
        public static Material CreatePseudo3d(Color color, float emissionIntensity = 0.12f)
        {
            return CreateInternal(color, lit: true, emission: emissionIntensity, useBevelMap: true, sphereShade: false);
        }

        /// <summary>Мяч: сферический градиент + Lit.</summary>
        public static Material CreatePseudo3dSphere(Color color, float emissionIntensity = 0.22f)
        {
            return CreateInternal(color, lit: true, emission: emissionIntensity, useBevelMap: false, sphereShade: true);
        }

        private static Material CreateInternal(
            Color color,
            bool lit,
            float emission,
            bool useBevelMap,
            bool sphereShade)
        {
            var hasUrp = GraphicsSettings.defaultRenderPipeline != null;
            Shader shader = null;

            if (lit && hasUrp)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (lit && shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null && hasUrp)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Lit");
            }

            shader ??= Shader.Find("Unlit/Color")
                       ?? Shader.Find("Sprites/Default")
                       ?? Shader.Find("Standard");

            if (shader == null)
            {
                return null;
            }

            var mat = new Material(shader);
            ApplyColor(mat, color);

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", 0.22f);
            }

            if (mat.HasProperty("_Glossiness"))
            {
                mat.SetFloat("_Glossiness", 0.62f);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.62f);
            }

            Texture2D map = null;
            if (useBevelMap)
            {
                map = GetBevelTexture();
            }
            else if (sphereShade)
            {
                map = GetSphereShadeTexture();
            }

            if (map != null)
            {
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", map);
                }

                if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", map);
                }
            }

            if (emission > 0.001f)
            {
                var emit = color * emission;
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emit);
                }
            }

            return mat;
        }

        public static void ApplyColor(Material mat, Color color)
        {
            if (mat == null)
            {
                return;
            }

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
        }

        /// <summary>Скос: светлый верх, тёмные края — объём на фронт-грани куба.</summary>
        public static Texture2D GetBevelTexture()
        {
            if (_bevelTex != null)
            {
                return _bevelTex;
            }

            const int size = 64;
            _bevelTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Pseudo3dBevel",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var edge = 10;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Min(x, size - 1 - x);
                    var dy = Mathf.Min(y, size - 1 - y);
                    var border = Mathf.Min(dx, dy);
                    var edgeShade = border < edge
                        ? Mathf.Lerp(0.35f, 1f, border / (float)edge)
                        : 1f;

                    // Верх светлее, низ чуть темнее + лёгкий боковой свет слева
                    var vertical = Mathf.Lerp(0.72f, 1.08f, y / (float)(size - 1));
                    var horizontal = Mathf.Lerp(1.05f, 0.88f, x / (float)(size - 1));
                    var v = Mathf.Clamp01(edgeShade * vertical * horizontal);
                    _bevelTex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }

            _bevelTex.Apply(false, true);
            return _bevelTex;
        }

        private static Texture2D GetSphereShadeTexture()
        {
            if (_sphereShadeTex != null)
            {
                return _sphereShadeTex;
            }

            const int size = 64;
            _sphereShadeTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Pseudo3dSphereShade",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var cx = (size - 1) * 0.5f;
            var cy = (size - 1) * 0.5f;
            var r = size * 0.48f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var nx = (x - cx) / r;
                    var ny = (y - cy) / r;
                    var d = nx * nx + ny * ny;
                    float v;
                    if (d > 1f)
                    {
                        v = 0.2f;
                    }
                    else
                    {
                        // Простой lambert от верхнего-левого света
                        var nz = Mathf.Sqrt(1f - d);
                        var ndotl = Mathf.Clamp01(nx * -0.35f + ny * 0.55f + nz * 0.75f);
                        v = 0.35f + ndotl * 0.75f;
                    }

                    _sphereShadeTex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }

            _sphereShadeTex.Apply(false, true);
            return _sphereShadeTex;
        }
    }
}
