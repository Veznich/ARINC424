using System;
using System.Collections.Generic;
using Arkanoid.Configs;
using Arkanoid.Core;
using Arkanoid.Difficulty;
using Arkanoid.Pool;
using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Спавн блоков из LevelStartedEvent, пул, коллизии мяча.
    /// </summary>
    public sealed class BlockField : MonoBehaviour
    {
        [SerializeField]
        private Transform blocksRoot;

        private LevelConfig _config;
        private IEventBus _eventBus;
        private PlayfieldBounds _bounds;
        private DifficultyDirector _difficulty;
        private ObjectPool<BlockView> _pool;
        private BlockView _prefab;

        private readonly List<BlockView> _active = new List<BlockView>(128);
        private BlockView[,] _grid;
        private int _width;
        private int _height;
        private float _cellSize;
        private Vector3 _origin;
        private bool _completing;
        private bool _pendingComplete;
        private IDisposable _startedSub;

        private LevelLayout _layout;

        public int ActiveCount => _active.Count;

        public void Configure(
            LevelConfig config,
            IEventBus eventBus,
            PlayfieldBounds bounds = null,
            DifficultyDirector difficulty = null)
        {
            _config = config;
            _eventBus = eventBus;
            _bounds = bounds;
            _difficulty = difficulty;
            EnsurePool();
            Subscribe();
        }

        private void OnDestroy()
        {
            _startedSub?.Dispose();
            _startedSub = null;
        }

        private void Subscribe()
        {
            _startedSub?.Dispose();
            if (_eventBus == null)
            {
                return;
            }

            _startedSub = _eventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
        }

        private void EnsurePool()
        {
            if (_pool != null)
            {
                return;
            }

            if (blocksRoot == null)
            {
                var go = new GameObject("Blocks");
                go.transform.SetParent(transform, false);
                blocksRoot = go.transform;
            }

            var prefabGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefabGo.name = "BlockPrefab";
            prefabGo.SetActive(false);
            prefabGo.transform.SetParent(blocksRoot, false);
            Destroy(prefabGo.GetComponent<BoxCollider>());
            _prefab = prefabGo.AddComponent<BlockView>();

            var prewarm = _config != null
                ? Mathf.Max(32, _config.gridWidth * _config.gridHeight)
                : 80;
            _pool = new ObjectPool<BlockView>(_prefab, blocksRoot, prewarm);
        }

        private void OnLevelStarted(LevelStartedEvent e)
        {
            Build(e.Layout);
        }

        public void Build(LevelLayout layout)
        {
            EnsurePool();
            Clear();
            _completing = false;
            _pendingComplete = false;
            _layout = layout;
            if (layout == null || layout.Cells == null)
            {
                return;
            }

            _width = layout.Width;
            _height = layout.Height;
            _cellSize = layout.CellSize;
            _origin = layout.Origin;
            _grid = new BlockView[_width, _height];
            FitBoundsToLayout(layout);

            var scale = _config != null ? _config.blockScale : 0.9f;
            for (var i = 0; i < layout.Cells.Length; i++)
            {
                var cell = layout.Cells[i];
                if (cell.Type == BlockType.Empty)
                {
                    continue;
                }

                var hits = cell.Hits;
                if (_difficulty != null && hits > 0)
                {
                    hits += _difficulty.ExtraBlockHits;
                }

                SpawnAt(cell.X, cell.Y, cell.Type, hits, scale);
            }

            if (_active.Count == 0)
            {
                Debug.LogWarning("[BlockField] Пустая раскладка — LevelCompleted сразу.");
                CheckCompletion();
            }
        }

        public void Clear()
        {
            if (_pool != null)
            {
                _pool.ReleaseAll(_active);
            }
            else
            {
                _active.Clear();
            }

            _grid = null;
        }

        private void Update()
        {
            if (_pendingComplete)
            {
                _pendingComplete = false;
                PublishCompleted();
            }
        }

        private void PublishCompleted()
        {
            if (_completing)
            {
                return;
            }

            _completing = true;
            var level = _layout != null ? _layout.LevelNumber : 1;
            var seed = _layout != null ? _layout.Seed : 0;
            _eventBus?.Publish(new LevelCompletedEvent(level, seed));
        }

        private void FitBoundsToLayout(LevelLayout layout)
        {
            var scale = _config != null ? _config.blockScale : 0.9f;
            PlayfieldLayout.FitBounds(_bounds, layout, scale);
            PlayfieldLayout.ConfigureCamera(Camera.main, _bounds);
        }

        private BlockView SpawnAt(int x, int y, BlockType type, int hits, float scale)
        {
            if (_grid == null || x < 0 || y < 0 || x >= _width || y >= _height)
            {
                return null;
            }

            if (_grid[x, y] != null)
            {
                return _grid[x, y];
            }

            var view = _pool.Get();
            view.Setup(x, y, type, hits, _cellSize, scale);
            view.transform.position = CellToWorld(x, y);
            _grid[x, y] = view;
            _active.Add(view);
            return view;
        }

        public Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(
                _origin.x + x * _cellSize,
                _origin.y + y * _cellSize,
                _origin.z);
        }

        /// <summary>Fireball: мгновенно снести блок (игнор HP).</summary>
        public void InstantDestroy(BlockView block)
        {
            if (block == null || !block.IsAlive)
            {
                return;
            }

            var type = block.Type;
            var gx = block.GridX;
            var gy = block.GridY;
            var worldPos = block.transform.position;
            block.ForceKill();
            RemoveBlock(block);
            _eventBus?.Publish(new BlockDestroyedEvent(gx, gy, type, worldPos));
            CheckCompletion();
        }

        /// <summary>
        /// Коллизия мяча. fireball+pierce: блок сносится без отскока.
        /// </summary>
        public bool ResolveBall(
            ref Vector3 position,
            ref Vector3 velocity,
            float radius,
            out bool appliedFrozen,
            bool fireball = false,
            int pierceLeft = 0)
        {
            appliedFrozen = false;
            if (_grid == null || velocity.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            position.z = 0f;

            BlockView best = null;
            var bestPen = 0f;
            var bestAxisX = false;
            var bestSign = 1f;

            for (var i = 0; i < _active.Count; i++)
            {
                var b = _active[i];
                if (b == null || !b.IsAlive)
                {
                    continue;
                }

                var c = b.transform.position;
                var hx = b.HalfExtent + radius;
                var hy = b.HalfExtent + radius;
                var dx = position.x - c.x;
                var dy = position.y - c.y;
                var ax = Mathf.Abs(dx);
                var ay = Mathf.Abs(dy);
                if (ax >= hx || ay >= hy)
                {
                    continue;
                }

                var penX = hx - ax;
                var penY = hy - ay;
                if (penX < penY)
                {
                    if (penX > bestPen)
                    {
                        bestPen = penX;
                        best = b;
                        bestAxisX = true;
                        bestSign = dx >= 0f ? 1f : -1f;
                    }
                }
                else if (penY > bestPen)
                {
                    bestPen = penY;
                    best = b;
                    bestAxisX = false;
                    bestSign = dy >= 0f ? 1f : -1f;
                }
            }

            if (best == null)
            {
                return false;
            }

            if (fireball && pierceLeft > 0)
            {
                InstantDestroy(best);
                position += velocity.normalized * 0.05f;
                return true;
            }

            const float skin = 0.02f;
            if (bestAxisX)
            {
                position.x += bestSign * (bestPen + skin);
                if (velocity.x * bestSign < 0f)
                {
                    velocity.x = -velocity.x;
                }
            }
            else
            {
                position.y += bestSign * (bestPen + skin);
                if (velocity.y * bestSign < 0f)
                {
                    velocity.y = -velocity.y;
                }
            }

            DamageBlock(best);
            return true;
        }

        private void DamageBlock(BlockView block)
        {
            if (block == null || !block.IsAlive)
            {
                return;
            }

            var destroyed = block.ApplyHit();
            _eventBus?.Publish(new BlockHitEvent(
                block.GridX,
                block.GridY,
                block.Type,
                block.HitsRemaining));

            if (!destroyed)
            {
                return;
            }

            var type = block.Type;
            var gx = block.GridX;
            var gy = block.GridY;
            var worldPos = block.transform.position;
            RemoveBlock(block);
            _eventBus?.Publish(new BlockDestroyedEvent(gx, gy, type, worldPos));
            CheckCompletion();
        }

        /// <summary>Laser: урон по клетке сетки.</summary>
        public bool DamageCell(int x, int y)
        {
            if (_grid == null || x < 0 || y < 0 || x >= _width || y >= _height)
            {
                return false;
            }

            var b = _grid[x, y];
            if (b == null || !b.IsAlive)
            {
                return false;
            }

            DamageBlock(b);
            return true;
        }

        /// <summary>Колонка по world X (платформа ниже сетки — Y не используем).</summary>
        public bool TryWorldToColumn(float worldX, out int x)
        {
            x = 0;
            if (_cellSize < 0.001f || _width <= 0)
            {
                return false;
            }

            x = Mathf.RoundToInt((worldX - _origin.x) / _cellSize);
            return x >= 0 && x < _width;
        }

        public bool TryWorldToCell(Vector3 world, out int x, out int y)
        {
            x = 0;
            y = 0;
            if (!TryWorldToColumn(world.x, out x))
            {
                return false;
            }

            y = Mathf.RoundToInt((world.y - _origin.y) / _cellSize);
            return y >= 0 && y < _height;
        }

        public int GridHeight => _height;
        public int GridWidth => _width;

        /// <summary>Мир-центр клетки (для VFX лазера).</summary>
        public bool TryGetCellWorld(int x, int y, out Vector3 world)
        {
            world = Vector3.zero;
            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                return false;
            }

            world = CellToWorld(x, y);
            return true;
        }

        private void RemoveBlock(BlockView block)
        {
            if (_grid != null &&
                block.GridX >= 0 && block.GridY >= 0 &&
                block.GridX < _width && block.GridY < _height &&
                _grid[block.GridX, block.GridY] == block)
            {
                _grid[block.GridX, block.GridY] = null;
            }

            _active.Remove(block);
            block.ResetForPool();
            _pool.Release(block);
        }

        private void CheckCompletion()
        {
            if (_completing || _pendingComplete)
            {
                return;
            }

            for (var i = 0; i < _active.Count; i++)
            {
                if (_active[i] != null && _active[i].IsAlive)
                {
                    return;
                }
            }

            // Отложить: иначе LevelStarted→Build во время Publish.
            _pendingComplete = true;
        }
    }
}
