using Arkanoid.Configs;
using Arkanoid.Core;
using Arkanoid.Input;
using UnityEngine;
using VContainer;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Платформа: движение по X из New Input System.
    /// Update — ввод; позиция применяется в Update (UI/input контракт).
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public sealed class PaddleController : MonoBehaviour
    {
        [SerializeField]
        private float defaultHalfWidth = 1f;

        private PaddleConfig _config;
        private IGameplayInput _input;
        private IGameStateMachine _stateMachine;

        private float _velocityX;
        private float _prevX;
        private float _widthScale = 1f;

        public float VelocityX => _velocityX;
        public float HalfWidth => GetHalfWidth();
        public Vector3 Position => transform.position;

        [Inject]
        public void Construct(
            PaddleConfig config,
            IGameplayInput input,
            IGameStateMachine stateMachine)
        {
            _config = config;
            _input = input;
            _stateMachine = stateMachine;
            ApplyVisualWidth();
        }

        /// <summary>Позволяет задать конфиг без DI (smoke / префаб).</summary>
        public void Configure(
            PaddleConfig config,
            IGameplayInput input,
            IGameStateMachine stateMachine)
        {
            Construct(config, input, stateMachine);
        }

        public void SetWidthScale(float scale)
        {
            _widthScale = Mathf.Max(0.1f, scale);
            ApplyVisualWidth();
        }

        private void Awake()
        {
            _prevX = transform.position.x;
        }

        private void Update()
        {
            if (!CanControl())
            {
                _velocityX = 0f;
                return;
            }

            var dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            var frame = _input.Current;
            var x = transform.position.x;
            var maxX = _config != null ? _config.maxX : 4.5f;
            var moveSpeed = _config != null ? _config.moveSpeed : 20f;
            var oneHandSpeed = _config != null ? _config.oneHandMoveSpeed : 12f;
            var sensitivity = _config != null ? _config.dragSensitivity : 0.01f;

            // 1) Клавиатура
            if (Mathf.Abs(frame.MoveAxis) > 0.01f)
            {
                x += frame.MoveAxis * moveSpeed * dt;
            }
            // 2) Drag в нижней зоне
            else if (frame.PointerPressed && frame.PointerInControlZone)
            {
                x += frame.PointerDelta.x * sensitivity;
            }
            // 3) One-hand: тянем к X касания (если палец на экране вне/внутри зоны)
            else if (frame.HasPointer && frame.PointerPressed)
            {
                x = Mathf.MoveTowards(x, frame.TargetWorldX, oneHandSpeed * dt);
            }

            x = Mathf.Clamp(x, -maxX, maxX);
            var pos = transform.position;
            pos.x = x;
            transform.position = pos;

            _velocityX = (x - _prevX) / dt;
            _prevX = x;
        }

        private bool CanControl()
        {
            if (_input == null)
            {
                return false;
            }

            if (_stateMachine == null)
            {
                return enabled;
            }

            return _stateMachine.CurrentState == GameState.Gameplay;
        }

        private float GetHalfWidth()
        {
            var w = _config != null ? _config.width : defaultHalfWidth * 2f;
            return (w * _widthScale) * 0.5f;
        }

        private void ApplyVisualWidth()
        {
            if (_config == null)
            {
                return;
            }

            var scale = transform.localScale;
            scale.x = _config.width * _widthScale;
            scale.y = _config.height;
            scale.z = Mathf.Max(GameplayVisualBootstrap.PaddleDepth, _config.height * 1.8f);
            transform.localScale = scale;
        }
    }
}
