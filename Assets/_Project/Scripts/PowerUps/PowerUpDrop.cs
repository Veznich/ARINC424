using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>Падающий бонус — 3D-значок эффекта.</summary>
    public sealed class PowerUpDrop : MonoBehaviour
    {
        public PowerUpType Type { get; private set; }
        public float LifeLeft { get; set; }
        public bool IsAlive { get; private set; }

        private Transform _visual;

        public void Setup(PowerUpType type, float lifetime, Vector3 position)
        {
            Type = type;
            LifeLeft = lifetime;
            IsAlive = true;
            transform.position = position;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            if (_visual == null)
            {
                var go = new GameObject("Visual");
                go.transform.SetParent(transform, false);
                _visual = go.transform;
            }

            PowerUpIcon3D.Build(_visual, type);
            _visual.localScale = Vector3.one * 2.04f; // −40% от 3.4
            gameObject.SetActive(true);
        }

        public void ResetForPool()
        {
            IsAlive = false;
            Type = PowerUpType.Fireball;
            LifeLeft = 0f;
            if (_visual != null)
            {
                PowerUpIcon3D.Clear(_visual);
            }
        }

        public void TickVisual(float dt)
        {
            if (_visual != null)
            {
                _visual.Rotate(0f, 110f * dt, 35f * dt, Space.Self);
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
    }
}
