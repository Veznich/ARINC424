using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Цвет = оставшиеся HP: 3 красный → 2 жёлтый → 1 зелёный → уничтожен.
    /// Тип при спавне задаёт стартовый лвл.
    /// </summary>
    public sealed class BlockView : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private Material _materialInstance;

        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public BlockType Type { get; private set; }
        public int HitsRemaining { get; private set; }
        public bool IsAlive => HitsRemaining > 0 && Type != BlockType.Empty;

        public float HalfExtent { get; private set; } = 0.45f;

        public void Setup(int x, int y, BlockType type, int hits, float cellSize, float scale)
        {
            GridX = x;
            GridY = y;
            Type = type;
            HitsRemaining = hits;
            HalfExtent = cellSize * scale * 0.5f;
            var xy = cellSize * scale;
            transform.localScale = new Vector3(xy, xy, xy * 0.5f);
            ApplyColor();
        }

        public void ResetForPool()
        {
            Type = BlockType.Empty;
            HitsRemaining = 0;
            GridX = -1;
            GridY = -1;
        }

        /// <summary>Урон: HP−1 и цвет на более низкий лвл. true — уничтожен.</summary>
        public bool ApplyHit()
        {
            if (!IsAlive)
            {
                return false;
            }

            HitsRemaining--;
            if (HitsRemaining > 0)
            {
                Type = TypeFromHits(HitsRemaining);
                ApplyColor();
                return false;
            }

            return true;
        }

        /// <summary>Мгновенное уничтожение (Fireball / Laser full clear).</summary>
        public void ForceKill()
        {
            HitsRemaining = 0;
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

            if (_renderer == null)
            {
                return;
            }

            var color = ColorForHits(HitsRemaining);
            if (_materialInstance == null)
            {
                _materialInstance = Utils.RuntimeMaterialUtil.CreatePseudo3d(
                    color,
                    GameplayVisualBootstrap.BlockEmission);
                if (_materialInstance != null)
                {
                    _renderer.sharedMaterial = _materialInstance;
                    _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    _renderer.receiveShadows = true;
                }
            }
            else
            {
                Utils.RuntimeMaterialUtil.ApplyColor(_materialInstance, color);
                if (_materialInstance.HasProperty("_EmissionColor"))
                {
                    _materialInstance.EnableKeyword("_EMISSION");
                    _materialInstance.SetColor(
                        "_EmissionColor",
                        color * GameplayVisualBootstrap.BlockEmission);
                }

                if (_renderer.sharedMaterial != _materialInstance)
                {
                    _renderer.sharedMaterial = _materialInstance;
                }
            }
        }

        private static BlockType TypeFromHits(int hits)
        {
            if (hits >= 3)
            {
                return BlockType.Red;
            }

            if (hits == 2)
            {
                return BlockType.Yellow;
            }

            return BlockType.Green;
        }

        private static Color ColorForHits(int hits)
        {
            if (hits >= 3)
            {
                return new Color(0.95f, 0.22f, 0.18f); // Red
            }

            if (hits == 2)
            {
                return new Color(1f, 0.85f, 0.15f); // Yellow
            }

            if (hits == 1)
            {
                return new Color(0.25f, 0.85f, 0.4f); // Green
            }

            return Color.gray;
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
                _materialInstance = null;
            }
        }
    }
}
