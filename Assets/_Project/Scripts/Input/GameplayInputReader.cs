using Arkanoid.Configs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arkanoid.Input
{
    /// <summary>Снимок ввода за кадр (для платформы и запуска мяча).</summary>
    public readonly struct GameplayInputFrame
    {
        public readonly float MoveAxis;
        public readonly float TargetWorldX;
        public readonly bool HasPointer;
        public readonly bool PointerPressed;
        public readonly bool PointerInControlZone;
        public readonly bool LaunchRequested;
        public readonly Vector2 PointerScreen;
        public readonly Vector2 PointerDelta;

        public GameplayInputFrame(
            float moveAxis,
            float targetWorldX,
            bool hasPointer,
            bool pointerPressed,
            bool pointerInControlZone,
            bool launchRequested,
            Vector2 pointerScreen,
            Vector2 pointerDelta)
        {
            MoveAxis = moveAxis;
            TargetWorldX = targetWorldX;
            HasPointer = hasPointer;
            PointerPressed = pointerPressed;
            PointerInControlZone = pointerInControlZone;
            LaunchRequested = launchRequested;
            PointerScreen = pointerScreen;
            PointerDelta = pointerDelta;
        }
    }

    public interface IGameplayInput
    {
        GameplayInputFrame Current { get; }
        void SetCamera(Camera camera);
    }

    /// <summary>
    /// New Input System: клавиатура + pointer/touch.
    /// Drag в нижней зоне экрана; one-hand — следование к X касания; launch — tap / swipe up.
    /// </summary>
    public sealed class GameplayInputReader : MonoBehaviour, IGameplayInput
    {
        [SerializeField]
        private Camera worldCamera;

        [SerializeField]
        private float swipeUpThresholdPixels = 40f;

        [SerializeField]
        private float tapMaxDuration = 0.25f;

        [SerializeField]
        private float tapMaxMovePixels = 18f;

        private PaddleConfig _paddleConfig;
        private InputAction _moveAction;
        private InputAction _pointAction;
        private InputAction _pressAction;
        private InputAction _deltaAction;

        private bool _pressWasDown;
        private float _pressStartTime;
        private Vector2 _pressStartScreen;
        private Vector2 _pressAccumDelta;
        public GameplayInputFrame Current { get; private set; }

        public void Configure(PaddleConfig paddleConfig)
        {
            _paddleConfig = paddleConfig;
        }

        public void SetCamera(Camera camera)
        {
            if (camera != null)
            {
                worldCamera = camera;
            }
        }

        private void Awake()
        {
            BuildActions();
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _pointAction?.Enable();
            _pressAction?.Enable();
            _deltaAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _pointAction?.Disable();
            _pressAction?.Disable();
            _deltaAction?.Disable();
        }

        private void OnDestroy()
        {
            _moveAction?.Dispose();
            _pointAction?.Dispose();
            _pressAction?.Dispose();
            _deltaAction?.Dispose();
        }

        private void Update()
        {
            Sample();
        }

        private void BuildActions()
        {
            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
            _moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/d")
                .With("Positive", "<Keyboard>/rightArrow");

            _pointAction = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
            _pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            _deltaAction = new InputAction("Delta", InputActionType.Value, "<Pointer>/delta");
        }

        private void Sample()
        {
            var moveAxis = _moveAction != null ? _moveAction.ReadValue<float>() : 0f;
            var pointerScreen = _pointAction != null ? _pointAction.ReadValue<Vector2>() : Vector2.zero;
            var pointerDelta = _deltaAction != null ? _deltaAction.ReadValue<Vector2>() : Vector2.zero;
            var pressed = _pressAction != null && _pressAction.IsPressed();
            var hasPointer = Pointer.current != null || Touchscreen.current != null || Mouse.current != null;

            var zoneFrac = _paddleConfig != null ? _paddleConfig.controlZoneScreenFraction : 0.333f;
            var inZone = hasPointer && pointerScreen.y <= Screen.height * zoneFrac;

            var launch = false;
            if (pressed && !_pressWasDown)
            {
                _pressWasDown = true;
                _pressStartTime = Time.unscaledTime;
                _pressStartScreen = pointerScreen;
                _pressAccumDelta = Vector2.zero;
            }
            else if (pressed && _pressWasDown)
            {
                _pressAccumDelta += pointerDelta;
            }
            else if (!pressed && _pressWasDown)
            {
                _pressWasDown = false;
                var duration = Time.unscaledTime - _pressStartTime;
                var releaseDelta = pointerScreen - _pressStartScreen;
                var totalMove = (_pressAccumDelta + releaseDelta).magnitude;

                var swipeUp = _pressAccumDelta.y >= swipeUpThresholdPixels ||
                              releaseDelta.y >= swipeUpThresholdPixels;
                var tap = duration <= tapMaxDuration && totalMove <= tapMaxMovePixels;
                launch = swipeUp || tap;
            }

            var targetWorldX = 0f;
            if (hasPointer && worldCamera != null)
            {
                var depth = Mathf.Abs(worldCamera.transform.position.z);
                var world = worldCamera.ScreenToWorldPoint(new Vector3(pointerScreen.x, pointerScreen.y, depth));
                targetWorldX = world.x;
            }

            Current = new GameplayInputFrame(
                moveAxis,
                targetWorldX,
                hasPointer,
                pressed,
                inZone,
                launch,
                pointerScreen,
                pointerDelta);
        }
    }
}
