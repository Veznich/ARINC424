using Arkanoid.Configs;
using Arkanoid.Core;
using Arkanoid.Input;
using System;
using UnityEngine;
using VContainer;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Мяч: кинематический Rigidbody, скорость вручную в FixedUpdate.
    /// Dock → Launch (tap/swipe) → отскоки от стен/платформы.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BallController : MonoBehaviour
    {
        [SerializeField]
        private float dockOffsetY = 0.55f;

        [SerializeField]
        private float defaultRadius = 0.25f;

        private BallConfig _config;
        private PaddleConfig _paddleConfig;
        private IGameplayInput _input;
        private IGameStateMachine _stateMachine;
        private IEventBus _eventBus;
        private PaddleController _paddle;
        private PlayfieldBounds _bounds;
        private BlockField _blocks;
        private LevelConfig _levelConfig;
        private PowerUpController _powerUps;

        private Rigidbody _body;
        private Vector3 _velocity;
        private float _currentSpeed;
        private float _speedTimer;
        private float _slowTimer;
        private float _slowMultiplier = 1f;
        private bool _docked = true;
        private bool _wasLaunchPressed;
        private bool _fireball;
        private int _fireballPierce;
        private float _dockOffsetX;
        private IDisposable _levelStartedSub;

        public bool IsDocked => _docked;
        public Vector3 Velocity => _velocity;

        public void SetFireball(bool enabled, int pierceLeft)
        {
            _fireball = enabled;
            _fireballPierce = pierceLeft;
        }

        [Inject]
        public void Construct(
            BallConfig config,
            PaddleConfig paddleConfig,
            IGameplayInput input,
            IGameStateMachine stateMachine,
            IEventBus eventBus)
        {
            _config = config;
            _paddleConfig = paddleConfig;
            _input = input;
            _stateMachine = stateMachine;
            _eventBus = eventBus;
            SubscribeLevelStarted();
        }

        private void SubscribeLevelStarted()
        {
            _levelStartedSub?.Dispose();
            if (_eventBus == null)
            {
                return;
            }

            _levelStartedSub = _eventBus.Subscribe<LevelStartedEvent>(_ => DockToPaddle(publishEvent: false));
        }

        private void OnDestroy()
        {
            _levelStartedSub?.Dispose();
            _levelStartedSub = null;
        }

        public void Configure(
            BallConfig config,
            PaddleConfig paddleConfig,
            IGameplayInput input,
            IGameStateMachine stateMachine,
            IEventBus eventBus,
            PaddleController paddle,
            PlayfieldBounds bounds,
            BlockField blocks = null,
            LevelConfig levelConfig = null,
            PowerUpController powerUps = null)
        {
            Construct(config, paddleConfig, input, stateMachine, eventBus);
            Bind(paddle, bounds, blocks, levelConfig, powerUps);
        }

        public void Bind(
            PaddleController paddle,
            PlayfieldBounds bounds,
            BlockField blocks = null,
            LevelConfig levelConfig = null,
            PowerUpController powerUps = null)
        {
            _paddle = paddle;
            _bounds = bounds;
            _blocks = blocks;
            _levelConfig = levelConfig;
            _powerUps = powerUps;
            if (_config != null)
            {
                _currentSpeed = _config.baseSpeed;
            }
        }

        /// <summary>Замедление от Frozen-блока.</summary>
        public void ApplyTemporarySlow(float duration, float multiplier)
        {
            _slowTimer = Mathf.Max(_slowTimer, duration);
            _slowMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            if (_body == null)
            {
                _body = gameObject.AddComponent<Rigidbody>();
            }

            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _currentSpeed = 10f;
        }

        private void OnEnable()
        {
            DockToPaddle(publishEvent: false);
        }

        private void Update()
        {
            if (!IsGameplay())
            {
                return;
            }

            if (_docked)
            {
                FollowPaddle();
                TryLaunchFromInput();
            }
        }

        private void FixedUpdate()
        {
            if (!IsGameplay() || _docked)
            {
                return;
            }

            TickSpeedRamp(Time.fixedDeltaTime);
            TickSlow(Time.fixedDeltaTime);
            // Субстепы — иначе при baseSpeed 10+ мяч туннелит сквозь верх блоков
            const int steps = 6;
            var stepDt = Time.fixedDeltaTime / steps;
            for (var i = 0; i < steps; i++)
            {
                Integrate(stepDt);
                ResolveBounds();
                ResolveBlocks();
            }

            ResolvePaddle();
        }

        public void DockToPaddle(bool publishEvent = true)
        {
            _docked = true;
            _velocity = Vector3.zero;
            _slowTimer = 0f;
            _slowMultiplier = 1f;
            _dockOffsetX = 0f;
            _currentSpeed = _config != null ? _config.baseSpeed : 10f;
            _speedTimer = 0f;
            FollowPaddle();
            if (publishEvent)
            {
                _eventBus?.Publish(new BallDockedEvent());
            }
        }

        /// <summary>Магнит: прилипание в точке удара (смещение по X сохраняется).</summary>
        public void CatchOnPaddle(float offsetX)
        {
            _docked = true;
            _velocity = Vector3.zero;
            _dockOffsetX = offsetX;
            FollowPaddle();
            _eventBus?.Publish(new BallDockedEvent());
        }

        public void Launch(Vector3 direction)
        {
            if (!_docked)
            {
                return;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.up;
            }

            _docked = false;
            _currentSpeed = _config != null ? _config.baseSpeed : 10f;
            _velocity = direction.normalized * _currentSpeed;
            _eventBus?.Publish(new BallLaunchedEvent(_velocity.normalized));
        }

        private void TryLaunchFromInput()
        {
            if (_input == null)
            {
                return;
            }

            var launch = _input.Current.LaunchRequested;
            // Также Space / W как desktop launch
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                {
                    launch = true;
                }
            }

            if (launch && !_wasLaunchPressed)
            {
                Launch(Vector3.up);
            }

            _wasLaunchPressed = launch;
        }

        private void FollowPaddle()
        {
            if (_paddle == null)
            {
                return;
            }

            var p = _paddle.Position;
            var halfW = _paddle.HalfWidth;
            var ox = Mathf.Clamp(_dockOffsetX, -halfW, halfW);
            transform.position = new Vector3(p.x + ox, p.y + dockOffsetY, 0f);
        }

        private void TickSpeedRamp(float dt)
        {
            if (_config == null)
            {
                return;
            }

            _speedTimer += dt;
            if (_speedTimer < _config.speedIncrementInterval)
            {
                return;
            }

            _speedTimer = 0f;
            _currentSpeed = Mathf.Min(
                _currentSpeed * (1f + _config.speedIncrement),
                _config.maxSpeed);
            if (_velocity.sqrMagnitude > 0.0001f)
            {
                _velocity = _velocity.normalized * _currentSpeed;
            }
        }

        private void TickSlow(float dt)
        {
            if (_slowTimer <= 0f)
            {
                _slowMultiplier = 1f;
                return;
            }

            _slowTimer -= dt;
            if (_slowTimer <= 0f)
            {
                _slowTimer = 0f;
                _slowMultiplier = 1f;
                if (_velocity.sqrMagnitude > 0.0001f)
                {
                    _velocity = _velocity.normalized * _currentSpeed;
                }
            }
        }

        private void Integrate(float dt)
        {
            var speedScale = _slowTimer > 0f ? _slowMultiplier : 1f;
            var next = transform.position + _velocity * (dt * speedScale);
            next.z = 0f;
            transform.position = next;
            _body.MovePosition(next);
        }

        private void ResolveBlocks()
        {
            if (_blocks == null)
            {
                return;
            }

            var pos = transform.position;
            pos.z = 0f;
            var vel = _velocity;
            var pierce = _fireballPierce;
            if (!_blocks.ResolveBall(
                    ref pos,
                    ref vel,
                    defaultRadius,
                    out var frozen,
                    _fireball,
                    pierce))
            {
                return;
            }

            if (_fireball && pierce > 0)
            {
                _powerUps?.NotifyFireballPierce();
                _fireballPierce = _powerUps != null ? _powerUps.FireballPierce : pierce - 1;
                // Fireball: направление не меняем при pierce — ResolveBall уже не отразил
            }
            else
            {
                _velocity = vel.sqrMagnitude > 0.0001f
                    ? vel.normalized * _currentSpeed
                    : vel;
            }

            pos.z = 0f;
            transform.position = pos;

            if (frozen && _levelConfig != null)
            {
                ApplyTemporarySlow(_levelConfig.frozenSlowDuration, _levelConfig.frozenSpeedMultiplier);
            }
        }

        private void ResolveBounds()
        {
            if (_bounds == null)
            {
                return;
            }

            var pos = transform.position;
            pos.z = 0f;
            var r = defaultRadius;
            var minSpeed = _config != null ? _config.baseSpeed * 0.5f : 5f;
            var wallAngle = _config != null ? _config.wallBounceAngle : 15f;
            var hitWall = false;

            if (pos.x - r < _bounds.MinX)
            {
                pos.x = _bounds.MinX + r;
                _velocity = BallBounceCalculator.ReflectOffWall(_velocity, Vector3.right, wallAngle, minSpeed);
                hitWall = true;
            }
            else if (pos.x + r > _bounds.MaxX)
            {
                pos.x = _bounds.MaxX - r;
                _velocity = BallBounceCalculator.ReflectOffWall(_velocity, Vector3.left, wallAngle, minSpeed);
                hitWall = true;
            }

            // Потолок поля = ниже статус-бара (MaxY уже с запасом под HUD)
            if (pos.y + r > _bounds.MaxY)
            {
                pos.y = _bounds.MaxY - r;
                _velocity = BallBounceCalculator.ReflectOffWall(_velocity, Vector3.down, wallAngle, minSpeed);
                hitWall = true;
            }

            if (pos.y - r < _bounds.MinY)
            {
                if (_powerUps != null && _powerUps.ConsumeShield())
                {
                    transform.position = pos;
                    DockToPaddle();
                    return;
                }

                transform.position = pos;
                _eventBus?.Publish(new BallLostEvent());
                DockToPaddle();
                return;
            }

            if (hitWall)
            {
                _velocity = _velocity.normalized * _currentSpeed;
                transform.position = pos;
                _eventBus?.Publish(new BallHitWallEvent());
            }
            else
            {
                transform.position = pos;
            }
        }

        private void ResolvePaddle()
        {
            if (_paddle == null || _velocity.y >= 0f)
            {
                return;
            }

            var pos = transform.position;
            var paddlePos = _paddle.Position;
            var halfW = _paddle.HalfWidth;
            var halfH = _paddleConfig != null ? _paddleConfig.height * 0.5f : 0.2f;
            var r = defaultRadius;

            var paddleTop = paddlePos.y + halfH;
            var withinX = Mathf.Abs(pos.x - paddlePos.x) <= halfW + r;
            var crossing = pos.y - r <= paddleTop && pos.y + r >= paddlePos.y - halfH;

            if (!withinX || !crossing)
            {
                return;
            }

            // Магнит: мяч прилипает к платформе, запуск — tap / Space
            if (_powerUps != null && _powerUps.IsMagnetActive)
            {
                var offsetX = Mathf.Clamp(pos.x - paddlePos.x, -halfW, halfW);
                CatchOnPaddle(offsetX);
                _eventBus?.Publish(new BallHitPaddleEvent(
                    BallBounceCalculator.ComputeHitFactor(pos.x, paddlePos.x, halfW)));
                return;
            }

            var hitFactor = BallBounceCalculator.ComputeHitFactor(pos.x, paddlePos.x, halfW);
            var maxAngle = _config != null ? _config.maxPaddleBounceAngle : 60f;
            var impact = _config != null ? _config.paddleImpactMultiplier : 2f;
            _velocity = BallBounceCalculator.DirectionFromPaddleHit(
                hitFactor,
                _paddle.VelocityX,
                maxAngle,
                impact,
                _currentSpeed);

            pos.y = paddleTop + r + 0.01f;
            transform.position = pos;
            _eventBus?.Publish(new BallHitPaddleEvent(hitFactor));
        }

        private bool IsGameplay()
        {
            return _stateMachine == null || _stateMachine.CurrentState == GameState.Gameplay;
        }
    }
}
