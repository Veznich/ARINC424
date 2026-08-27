using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>Падающий бонус (визуал + тип).</summary>
    public sealed class PowerUpDrop : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private Material _mat;

        public PowerUpType Type { get; private set; }
        public float LifeLeft { get; set; }
        public bool IsAlive { get; private set; }

        public void Setup(PowerUpType type, float lifetime, Vector3 position)
        {
            Type = type;
            LifeLeft = lifetime;
            IsAlive = true;
            transform.position = position;
            transform.localScale = Vector3.one * 0.45f;
            transform.rotation = Quaternion.identity;
            ApplyColor();
            gameObject.SetActive(true);
        }

        public void ResetForPool()
        {
            IsAlive = false;
            Type = PowerUpType.Fireball;
            LifeLeft = 0f;
        }

        public void TickVisual(float dt)
        {
            transform.Rotate(0f, 90f * dt, 45f * dt, Space.Self);
        }

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        private void ApplyColor()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
            }

            var c = ColorFor(Type);
            if (_mat == null)
            {
                _mat = Utils.RuntimeMaterialUtil.CreatePseudo3d(c, 0.25f);
                if (_mat != null && _renderer != null)
                {
                    _renderer.sharedMaterial = _mat;
                }
            }
            else
            {
                Utils.RuntimeMaterialUtil.ApplyColor(_mat, c);
                if (_mat.HasProperty("_EmissionColor"))
                {
                    _mat.SetColor("_EmissionColor", c * 0.25f);
                }
            }
        }

        public static Color ColorFor(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Fireball: return new Color(1f, 0.4f, 0.1f);
                case PowerUpType.WidePaddle: return new Color(0.3f, 0.7f, 1f);
                case PowerUpType.SlowTime: return new Color(0.6f, 0.5f, 1f);
                case PowerUpType.MultiBall: return new Color(0.3f, 1f, 0.4f);
                case PowerUpType.Laser: return new Color(1f, 0.2f, 0.35f);
                case PowerUpType.Shield: return new Color(0.4f, 0.85f, 1f);
                case PowerUpType.Magnet: return new Color(0.85f, 0.35f, 1f);
                case PowerUpType.ExtraLife: return new Color(1f, 0.35f, 0.55f);
                default: return Color.white;
            }
        }

        private void OnDestroy()
        {
            if (_mat != null)
            {
                Destroy(_mat);
                _mat = null;
            }
        }
    }
}
