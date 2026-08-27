using System;
using System.Collections.Generic;
using Arkanoid.Configs;
using Arkanoid.Core;
using Arkanoid.Pool;
using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Drop из блоков, подбор платформой, активация 8 бонусов, таймеры.
    /// </summary>
    public sealed class PowerUpController : MonoBehaviour
    {
        private PowerUpConfig _config;
        private PaddleConfig _paddleConfig;
        private PlayerConfig _playerConfig;
        private IEventBus _eventBus;
        private IGameStateMachine _stateMachine;
        private PaddleController _paddle;
        private BallController _ball;
        private BlockField _blocks;
        private PlayfieldBounds _bounds;
        private LivesService _lives;

        private ObjectPool<PowerUpDrop> _dropPool;
        private readonly List<PowerUpDrop> _activeDrops = new List<PowerUpDrop>(16);
        private readonly List<ExtraBall> _extraBalls = new List<ExtraBall>(4);
        private ObjectPool<ExtraBall> _ballPool;

        private readonly Dictionary<PowerUpType, float> _timers = new Dictionary<PowerUpType, float>(8);
        private readonly Dictionary<PowerUpType, float> _durations = new Dictionary<PowerUpType, float>(8);

        private bool _fireball;
        private int _fireballPierce;
        private bool _shield;
        private bool _magnet;
        private float _laserTimer;
        private float _slowMul = 1f;
        private System.Random _rng = new System.Random();

        private IDisposable _destroyedSub;
        private IDisposable _lostSub;
        private IDisposable _levelSub;
        private Transform _dropRoot;
        private Transform _ballRoot;

        public void Configure(
            PowerUpConfig config,
            PaddleConfig paddleConfig,
            PlayerConfig playerConfig,
            IEventBus eventBus,
            IGameStateMachine stateMachine,
            PaddleController paddle,
            BallController ball,
            BlockField blocks,
            PlayfieldBounds bounds,
            LivesService lives)
        {
            _config = config;
            _paddleConfig = paddleConfig;
            _playerConfig = playerConfig;
            _eventBus = eventBus;
            _stateMachine = stateMachine;
            _paddle = paddle;
            _ball = ball;
            _blocks = blocks;
            _bounds = bounds;
            _lives = lives;
            EnsurePools();
            Subscribe();
            ClearEffects(keepBalls: false);
            PublishTimers();
        }

        private void OnDestroy()
        {
            _destroyedSub?.Dispose();
            _lostSub?.Dispose();
            _levelSub?.Dispose();
        }

        private void Subscribe()
        {
            _destroyedSub?.Dispose();
            _lostSub?.Dispose();
            _levelSub?.Dispose();
            if (_eventBus == null)
            {
                return;
            }

            _destroyedSub = _eventBus.Subscribe<BlockDestroyedEvent>(OnBlockDestroyed);
            _lostSub = _eventBus.Subscribe<BallLostEvent>(_ => OnBallLost());
            _levelSub = _eventBus.Subscribe<LevelStartedEvent>(_ =>
            {
                ClearDrops();
                ClearEffects(keepBalls: false);
                PublishTimers();
            });
        }

        private void EnsurePools()
        {
            if (_dropRoot == null)
            {
                var go = new GameObject("PowerUpDrops");
                go.transform.SetParent(transform, false);
                _dropRoot = go.transform;
            }

            if (_ballRoot == null)
            {
                var go = new GameObject("ExtraBalls");
                go.transform.SetParent(transform, false);
                _ballRoot = go.transform;
            }

            if (_dropPool == null)
            {
                var prefabGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prefabGo.name = "DropPrefab";
                prefabGo.SetActive(false);
                prefabGo.transform.SetParent(_dropRoot, false);
                Destroy(prefabGo.GetComponent<BoxCollider>());
                var drop = prefabGo.AddComponent<PowerUpDrop>();
                _dropPool = new ObjectPool<PowerUpDrop>(drop, _dropRoot, 12);
            }

            if (_ballPool == null)
            {
                var prefabGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                prefabGo.name = "ExtraBallPrefab";
                prefabGo.SetActive(false);
                prefabGo.transform.SetParent(_ballRoot, false);
                Destroy(prefabGo.GetComponent<SphereCollider>());
                prefabGo.transform.localScale = Vector3.one * 0.45f;
                var mat = Utils.RuntimeMaterialUtil.CreatePseudo3dSphere(
                    new Color(0.3f, 1f, 0.45f), 0.2f);
                var rend = prefabGo.GetComponent<MeshRenderer>();
                if (rend != null && mat != null)
                {
                    rend.sharedMaterial = mat;
                }

                var eb = prefabGo.AddComponent<ExtraBall>();
                _ballPool = new ObjectPool<ExtraBall>(eb, _ballRoot, 4);
            }
        }

        private void OnBlockDestroyed(BlockDestroyedEvent e)
        {
            if (_config == null || _stateMachine == null ||
                _stateMachine.CurrentState != GameState.Gameplay)
            {
                return;
            }

            if (_rng.NextDouble() > _config.dropChance)
            {
                return;
            }

            var type = RollType();
            SpawnDrop(type, e.WorldPosition);
        }

        private PowerUpType RollType()
        {
            // ExtraLife — редкая доля
            if (_rng.NextDouble() < _config.lifeBonusShareOfDrops)
            {
                return PowerUpType.ExtraLife;
            }

            var values = new[]
            {
                PowerUpType.Fireball,
                PowerUpType.WidePaddle,
                PowerUpType.SlowTime,
                PowerUpType.MultiBall,
                PowerUpType.Laser,
                PowerUpType.Shield,
                PowerUpType.Magnet
            };
            return values[_rng.Next(0, values.Length)];
        }

        private void SpawnDrop(PowerUpType type, Vector3 pos)
        {
            EnsurePools();
            var drop = _dropPool.Get();
            pos.z = 0f;
            drop.Setup(type, _config.lifetimeSeconds, pos);
            _activeDrops.Add(drop);
        }

        private void Update()
        {
            if (_stateMachine == null || _stateMachine.CurrentState != GameState.Gameplay)
            {
                return;
            }

            var dt = Time.deltaTime * _slowMul;
            TickDrops(dt);
            TickTimers(Time.deltaTime); // таймеры UI в реальном времени
            TickLaser(dt);
            TickExtraBalls(dt);
            SyncBallFireball();
        }

        private void TickDrops(float dt)
        {
            if (_paddle == null)
            {
                return;
            }

            var fall = (_config != null ? _config.fallSpeed : 2f) * dt;
            var magnet = _magnet ? (_config != null ? _config.magnetPullSpeed : 5f) * dt : 0f;
            var halfW = _paddle.HalfWidth;
            var halfH = _paddleConfig != null ? _paddleConfig.height * 0.5f : 0.2f;
            var p = _paddle.Position;

            for (var i = _activeDrops.Count - 1; i >= 0; i--)
            {
                var d = _activeDrops[i];
                if (d == null || !d.IsAlive)
                {
                    _activeDrops.RemoveAt(i);
                    continue;
                }

                var pos = d.transform.position;
                if (_magnet)
                {
                    var to = (p - pos);
                    if (to.sqrMagnitude > 0.001f)
                    {
                        pos += to.normalized * magnet;
                    }
                }

                pos.y -= fall;
                pos.z = 0f;
                d.transform.position = pos;
                d.TickVisual(dt);
                d.LifeLeft -= dt;

                var pickup =
                    Mathf.Abs(pos.x - p.x) <= halfW + 0.3f &&
                    Mathf.Abs(pos.y - p.y) <= halfH + 0.35f;

                if (pickup)
                {
                    var type = d.Type;
                    ReleaseDrop(d, i);
                    Activate(type);
                    continue;
                }

                if (d.LifeLeft <= 0f || (_bounds != null && pos.y < _bounds.MinY))
                {
                    ReleaseDrop(d, i);
                }
            }
        }

        private void ReleaseDrop(PowerUpDrop d, int index)
        {
            d.ResetForPool();
            _dropPool.Release(d);
            if (index >= 0 && index < _activeDrops.Count && _activeDrops[index] == d)
            {
                _activeDrops.RemoveAt(index);
            }
            else
            {
                _activeDrops.Remove(d);
            }
        }

        private void ClearDrops()
        {
            for (var i = _activeDrops.Count - 1; i >= 0; i--)
            {
                ReleaseDrop(_activeDrops[i], i);
            }

            _activeDrops.Clear();
        }

        private void Activate(PowerUpType type)
        {
            _eventBus?.Publish(new PowerUpCollectedEvent(type));

            switch (type)
            {
                case PowerUpType.ExtraLife:
                    _lives?.TryAddLife();
                    PublishTimers();
                    return;
                case PowerUpType.MultiBall:
                    SpawnExtraBall();
                    PublishTimers();
                    return;
                case PowerUpType.Shield:
                    _shield = true;
                    SetTimed(PowerUpType.Shield, 999f); // до срабатывания — большой таймер как индикатор
                    PublishTimers();
                    return;
                case PowerUpType.Fireball:
                    _fireball = true;
                    _fireballPierce = _config != null ? _config.fireballPierceCount : 2;
                    SetTimed(type, _config.fireballDuration);
                    break;
                case PowerUpType.WidePaddle:
                    _paddle?.SetWidthScale(_paddleConfig != null ? _paddleConfig.wideScaleMultiplier : 1.5f);
                    SetTimed(type, _config.widePaddleDuration);
                    break;
                case PowerUpType.SlowTime:
                    _slowMul = _config != null ? _config.slowTimeScale : 0.6f;
                    _ball?.ApplyTemporarySlow(_config.slowTimeDuration, _slowMul);
                    SetTimed(type, _config.slowTimeDuration);
                    break;
                case PowerUpType.Laser:
                    _laserTimer = 0f;
                    SetTimed(type, _config.laserDuration);
                    break;
                case PowerUpType.Magnet:
                    _magnet = true;
                    SetTimed(type, _config.magnetDuration);
                    break;
            }

            PublishTimers();
        }

        private void SetTimed(PowerUpType type, float duration)
        {
            _timers[type] = duration;
            _durations[type] = duration;
        }

        private void TickTimers(float dt)
        {
            if (_timers.Count == 0)
            {
                return;
            }

            var keys = new List<PowerUpType>(_timers.Keys);
            var changed = false;
            for (var i = 0; i < keys.Count; i++)
            {
                var t = keys[i];
                if (t == PowerUpType.Shield)
                {
                    continue; // до срабатывания
                }

                _timers[t] -= dt;
                if (_timers[t] <= 0f)
                {
                    _timers.Remove(t);
                    _durations.Remove(t);
                    Expire(t);
                    changed = true;
                }
                else
                {
                    changed = true;
                }
            }

            if (changed)
            {
                PublishTimers();
            }
        }

        private void Expire(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Fireball:
                    _fireball = false;
                    _fireballPierce = 0;
                    break;
                case PowerUpType.WidePaddle:
                    _paddle?.SetWidthScale(1f);
                    break;
                case PowerUpType.SlowTime:
                    _slowMul = 1f;
                    break;
                case PowerUpType.Laser:
                    break;
                case PowerUpType.Magnet:
                    _magnet = false;
                    break;
            }

            _eventBus?.Publish(new PowerUpExpiredEvent(type));
        }

        private void TickLaser(float dt)
        {
            if (!_timers.ContainsKey(PowerUpType.Laser) || _blocks == null || _paddle == null)
            {
                return;
            }

            _laserTimer -= dt;
            if (_laserTimer > 0f)
            {
                return;
            }

            _laserTimer = _config != null ? _config.laserInterval : 0.5f;
            if (!_blocks.TryWorldToCell(_paddle.Position, out var cx, out var cy))
            {
                return;
            }

            // Луч вверх по колонке
            for (var y = cy + 1; y < _blocks.GridHeight; y++)
            {
                if (_blocks.DamageCell(cx, y))
                {
                    break; // один блок за выстрел
                }
            }
        }

        private void SpawnExtraBall()
        {
            EnsurePools();
            var max = _config != null ? _config.maxBalls : 3;
            var alive = CountAliveBalls();
            if (alive >= max)
            {
                return;
            }

            var eb = _ballPool.Get();
            var origin = _ball != null ? _ball.transform.position : _paddle.Position + Vector3.up;
            var dir = Quaternion.Euler(0f, 0f, _rng.Next(-40, 41)) * Vector3.up;
            var speed = 10f;
            var halfH = _paddleConfig != null ? _paddleConfig.height * 0.5f : 0.2f;
            eb.Launch(origin, dir, speed, _blocks, _bounds, _paddle, halfH);
            if (_fireball)
            {
                eb.SetFireball(true, _fireballPierce);
            }

            _extraBalls.Add(eb);
        }

        private int CountAliveBalls()
        {
            var n = 1; // основной мяч занимает слот
            for (var i = 0; i < _extraBalls.Count; i++)
            {
                if (_extraBalls[i] != null && _extraBalls[i].IsAlive)
                {
                    n++;
                }
            }

            return n;
        }

        private void TickExtraBalls(float dt)
        {
            for (var i = _extraBalls.Count - 1; i >= 0; i--)
            {
                var b = _extraBalls[i];
                if (b == null)
                {
                    _extraBalls.RemoveAt(i);
                    continue;
                }

                if (!b.IsAlive)
                {
                    _ballPool.Release(b);
                    _extraBalls.RemoveAt(i);
                    continue;
                }

                b.Tick(dt * _slowMul);
                if (_fireball)
                {
                    b.SetFireball(true, _fireballPierce);
                }
            }
        }

        private void SyncBallFireball()
        {
            // pierce уменьшаем при InstantDestroy через ResolveBall — BallController передаёт флаг
            _ball?.SetFireball(_fireball, _fireballPierce);
        }

        /// <summary>Вызывается из BallController после pierce-хита.</summary>
        public void NotifyFireballPierce()
        {
            if (_fireballPierce > 0)
            {
                _fireballPierce--;
            }
        }

        public bool ConsumeShield()
        {
            if (!_shield)
            {
                return false;
            }

            _shield = false;
            _timers.Remove(PowerUpType.Shield);
            _durations.Remove(PowerUpType.Shield);
            _eventBus?.Publish(new PowerUpExpiredEvent(PowerUpType.Shield));
            PublishTimers();
            return true;
        }

        private void OnBallLost()
        {
            // Shield обрабатывается в BallController до Publish BallLost — здесь сброс timed кроме multi
            if (_timers.ContainsKey(PowerUpType.Fireball))
            {
                _timers.Remove(PowerUpType.Fireball);
                _durations.Remove(PowerUpType.Fireball);
                Expire(PowerUpType.Fireball);
            }

            if (_timers.ContainsKey(PowerUpType.WidePaddle))
            {
                _timers.Remove(PowerUpType.WidePaddle);
                _durations.Remove(PowerUpType.WidePaddle);
                Expire(PowerUpType.WidePaddle);
            }

            if (_timers.ContainsKey(PowerUpType.SlowTime))
            {
                _timers.Remove(PowerUpType.SlowTime);
                _durations.Remove(PowerUpType.SlowTime);
                Expire(PowerUpType.SlowTime);
            }

            if (_timers.ContainsKey(PowerUpType.Laser))
            {
                _timers.Remove(PowerUpType.Laser);
                _durations.Remove(PowerUpType.Laser);
                Expire(PowerUpType.Laser);
            }

            if (_timers.ContainsKey(PowerUpType.Magnet))
            {
                _timers.Remove(PowerUpType.Magnet);
                _durations.Remove(PowerUpType.Magnet);
                Expire(PowerUpType.Magnet);
            }

            PublishTimers();
        }

        private void ClearEffects(bool keepBalls)
        {
            _fireball = false;
            _fireballPierce = 0;
            _shield = false;
            _magnet = false;
            _slowMul = 1f;
            _laserTimer = 0f;
            _paddle?.SetWidthScale(1f);
            _timers.Clear();
            _durations.Clear();
            if (!keepBalls)
            {
                for (var i = _extraBalls.Count - 1; i >= 0; i--)
                {
                    if (_extraBalls[i] != null)
                    {
                        _extraBalls[i].Kill();
                        _ballPool?.Release(_extraBalls[i]);
                    }
                }

                _extraBalls.Clear();
            }
        }

        private void PublishTimers()
        {
            if (_eventBus == null)
            {
                return;
            }

            var list = new List<PowerUpTimerInfo>(_timers.Count);
            foreach (var kv in _timers)
            {
                var dur = _durations.TryGetValue(kv.Key, out var d) ? d : kv.Value;
                list.Add(new PowerUpTimerInfo
                {
                    Type = kv.Key,
                    Remaining = kv.Key == PowerUpType.Shield ? 1f : kv.Value,
                    Duration = kv.Key == PowerUpType.Shield ? 1f : dur,
                    IsInstant = false
                });
            }

            _eventBus.Publish(new PowerUpTimersChangedEvent(list.ToArray()));
        }

        public bool HasFireball => _fireball;
        public int FireballPierce => _fireballPierce;
    }
}
