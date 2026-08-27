using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>3D-значки бонусов из примитивов (дропы + HUD).</summary>
    public static class PowerUpIcon3D
    {
        public static void Clear(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }

        public static void Build(Transform parent, PowerUpType type)
        {
            Clear(parent);
            var c = PowerUpDrop.ColorFor(type);
            switch (type)
            {
                case PowerUpType.MultiBall:
                    AddSphere(parent, new Vector3(-0.22f, -0.12f, 0f), 0.28f, c);
                    AddSphere(parent, new Vector3(0.22f, -0.12f, 0f), 0.28f, c);
                    AddSphere(parent, new Vector3(0f, 0.2f, 0f), 0.28f, c);
                    break;
                case PowerUpType.Fireball:
                    AddSphere(parent, Vector3.zero, 0.42f, c);
                    AddCube(parent, new Vector3(0f, 0.32f, 0f), new Vector3(0.18f, 0.28f, 0.18f),
                        new Color(1f, 0.85f, 0.2f), Quaternion.Euler(0f, 0f, 45f));
                    break;
                case PowerUpType.WidePaddle:
                    AddCube(parent, Vector3.zero, new Vector3(0.85f, 0.18f, 0.28f), c, Quaternion.identity);
                    break;
                case PowerUpType.SlowTime:
                    AddCylinder(parent, Vector3.zero, new Vector3(0.45f, 0.08f, 0.45f), c * 0.75f);
                    AddCube(parent, new Vector3(0f, 0.12f, 0f), new Vector3(0.06f, 0.28f, 0.06f), Color.white, Quaternion.identity);
                    AddCube(parent, new Vector3(0.12f, 0.05f, 0f), new Vector3(0.2f, 0.06f, 0.06f), Color.white, Quaternion.identity);
                    break;
                case PowerUpType.Laser:
                    AddCube(parent, Vector3.zero, new Vector3(0.1f, 0.85f, 0.1f), c, Quaternion.identity);
                    AddCube(parent, new Vector3(0f, 0.35f, 0f), new Vector3(0.28f, 0.1f, 0.1f),
                        new Color(1f, 0.65f, 0.7f), Quaternion.identity);
                    break;
                case PowerUpType.Shield:
                    AddSphere(parent, Vector3.zero, 0.55f, new Color(c.r, c.g, c.b, 1f) * 0.55f);
                    AddSphere(parent, Vector3.zero, 0.32f, c);
                    break;
                case PowerUpType.Magnet:
                    AddCube(parent, new Vector3(-0.18f, 0.05f, 0f), new Vector3(0.16f, 0.5f, 0.16f),
                        new Color(0.9f, 0.25f, 0.25f), Quaternion.identity);
                    AddCube(parent, new Vector3(0.18f, 0.05f, 0f), new Vector3(0.16f, 0.5f, 0.16f),
                        new Color(0.3f, 0.45f, 1f), Quaternion.identity);
                    AddCube(parent, new Vector3(0f, -0.22f, 0f), new Vector3(0.52f, 0.16f, 0.16f), c, Quaternion.identity);
                    break;
                case PowerUpType.ExtraLife:
                    AddSphere(parent, new Vector3(-0.16f, 0.1f, 0f), 0.26f, c);
                    AddSphere(parent, new Vector3(0.16f, 0.1f, 0f), 0.26f, c);
                    AddCube(parent, new Vector3(0f, -0.14f, 0f), new Vector3(0.32f, 0.32f, 0.2f), c,
                        Quaternion.Euler(0f, 0f, 45f));
                    break;
                default:
                    AddCube(parent, Vector3.zero, Vector3.one * 0.4f, c, Quaternion.identity);
                    break;
            }
        }

        private static void AddSphere(Transform parent, Vector3 localPos, float diameter, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Part";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * diameter;
            StripCollider(go);
            ApplyMat(go, color, sphere: true);
        }

        private static void AddCube(
            Transform parent,
            Vector3 localPos,
            Vector3 scale,
            Color color,
            Quaternion localRot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Part";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            StripCollider(go);
            ApplyMat(go, color, sphere: false);
        }

        private static void AddCylinder(Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Part";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            StripCollider(go);
            ApplyMat(go, color, sphere: false);
        }

        private static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.Destroy(col);
            }
        }

        private static void ApplyMat(GameObject go, Color color, bool sphere)
        {
            var rend = go.GetComponent<MeshRenderer>();
            if (rend == null)
            {
                return;
            }

            var mat = sphere
                ? Utils.RuntimeMaterialUtil.CreatePseudo3dSphere(color, 0.22f)
                : Utils.RuntimeMaterialUtil.CreatePseudo3d(color, 0.18f);
            if (mat != null)
            {
                rend.sharedMaterial = mat;
            }
        }
    }
}
