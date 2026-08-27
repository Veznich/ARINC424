using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Цвет = оставшиеся HP. Лестница:
    /// зелёный→жёлтый→красный→синий→чёрный→медный→железный→бриллиант.
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
            return LevelGenerator.BlockTypeFromHits(hits);
        }

        private static Color ColorForHits(int hits)
        {
            switch (hits)
            {
                case 1: return new Color(0.25f, 0.85f, 0.4f);   // Зелёный
                case 2: return new Color(1f, 0.85f, 0.15f);     // Жёлтый
                case 3: return new Color(0.95f, 0.22f, 0.18f);  // Красный
                case 4: return new Color(0.2f, 0.45f, 1f);      // Синий
                case 5: return new Color(0.12f, 0.12f, 0.14f);  // Чёрный
                case 6: return new Color(0.85f, 0.48f, 0.22f);  // Медный
                case 7: return new Color(0.62f, 0.66f, 0.72f);  // Железный
                default:
                    if (hits >= 8)
                    {
                        return new Color(0.65f, 0.95f, 1f);     // Бриллиантовый
                    }

                    return Color.gray;
            }
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
