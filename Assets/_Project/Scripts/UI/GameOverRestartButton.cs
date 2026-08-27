using System;
using Arkanoid.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arkanoid.UI
{
    /// <summary>
    /// 3D-кнопка RESTART на Game Over. Клик → RequestRestartLevelEvent (тот же уровень сначала).
    /// </summary>
    public sealed class GameOverRestartButton : MonoBehaviour
    {
        private IEventBus _eventBus;
        private IGameStateMachine _stateMachine;
        private Camera _camera;
        private GameObject _root;
        private Collider _collider;
        private IDisposable _stateSub;
        private bool _visible;
        private bool _wasPressed;

        public void Configure(IEventBus eventBus, IGameStateMachine stateMachine, Camera camera)
        {
            _eventBus = eventBus;
            _stateMachine = stateMachine;
            _camera = camera != null ? camera : Camera.main;
            EnsureVisual();
            Subscribe();
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            _stateSub?.Dispose();
        }

        private void Subscribe()
        {
            _stateSub?.Dispose();
            if (_eventBus == null)
            {
                return;
            }

            _stateSub = _eventBus.Subscribe<GameStateChangedEvent>(_ => RefreshVisibility());
        }

        private void EnsureVisual()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("RestartButton3D");
            _root.transform.SetParent(transform, false);
            // Центр поля, чуть ближе к камере
            _root.transform.localPosition = new Vector3(0f, 0.8f, -0.8f);
            _root.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            _root.transform.localScale = Vector3.one;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(_root.transform, false);
            body.transform.localPosition = Vector3.zero;
            // Шире, чтобы RESTART не вылезал за края
            body.transform.localScale = new Vector3(5.2f, 1.15f, 0.6f);
            _collider = body.GetComponent<Collider>();

            // Тёмный корпус — светлая надпись читается лучше, чем white-on-cyan
            var mat = Arkanoid.Utils.RuntimeMaterialUtil.CreatePseudo3d(
                new Color(0.08f, 0.12f, 0.2f),
                0.08f);
            var renderer = body.GetComponent<MeshRenderer>();
            if (renderer != null && mat != null)
            {
                renderer.sharedMaterial = mat;
            }

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.02f, -0.35f);
            labelGo.transform.localRotation = Quaternion.identity;
            labelGo.transform.localScale = Vector3.one * 0.11f;

            var text = labelGo.AddComponent<TextMesh>();
            text.text = "RESTART";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 1f;
            text.fontSize = 60;
            text.color = new Color(0.35f, 0.95f, 0.75f); // мятный на тёмном
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font != null)
            {
                text.font = font;
                var mr = labelGo.GetComponent<MeshRenderer>();
                if (mr != null && font.material != null)
                {
                    mr.sharedMaterial = font.material;
                }
            }

            // Подпись GAME OVER над кнопкой
            var titleGo = new GameObject("GameOverTitle");
            titleGo.transform.SetParent(_root.transform, false);
            titleGo.transform.localPosition = new Vector3(0f, 1.45f, -0.2f);
            titleGo.transform.localScale = Vector3.one * 0.13f;
            var title = titleGo.AddComponent<TextMesh>();
            title.text = "GAME OVER";
            title.anchor = TextAnchor.MiddleCenter;
            title.alignment = TextAlignment.Center;
            title.characterSize = 1f;
            title.fontSize = 72;
            title.color = new Color(1f, 0.45f, 0.4f);
            if (font != null)
            {
                title.font = font;
                var tmr = titleGo.GetComponent<MeshRenderer>();
                if (tmr != null && font.material != null)
                {
                    tmr.sharedMaterial = font.material;
                }
            }

            _root.SetActive(false);
        }

        private void RefreshVisibility()
        {
            _visible = _stateMachine != null && _stateMachine.CurrentState == GameState.GameOver;
            if (_root != null)
            {
                _root.SetActive(_visible);
            }
        }

        private void Update()
        {
            if (!_visible || _eventBus == null || _collider == null)
            {
                _wasPressed = false;
                return;
            }

            var pressed = IsPointerPressed();
            if (pressed && !_wasPressed && TryHitButton())
            {
                _eventBus.Publish(new RequestRestartLevelEvent());
                Debug.Log("[GameOver] Restart → текущий уровень сначала");
            }

            _wasPressed = pressed;
        }

        private bool TryHitButton()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                return false;
            }

            Vector2 screenPos;
            if (Pointer.current != null)
            {
                screenPos = Pointer.current.position.ReadValue();
            }
            else if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else
            {
                return false;
            }

            var ray = _camera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 100f))
            {
                return false;
            }

            return hit.collider == _collider || hit.collider.transform.IsChildOf(_root.transform);
        }

        private static bool IsPointerPressed()
        {
            if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return true;
            }

            return false;
        }
    }
}
