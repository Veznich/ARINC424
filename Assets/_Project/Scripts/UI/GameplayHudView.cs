using System;
using Arkanoid.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Arkanoid.UI
{
    /// <summary>
    /// Верхний статус-бар: слева жизни, центр — уровень, справа пауза.
    /// Собирается runtime (без префаба).
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour
    {
        private IEventBus _eventBus;
        private IGameStateMachine _stateMachine;
        private Text _livesText;
        private Text _levelText;
        private Text _pauseLabel;
        private Button _pauseButton;
        private GameObject _pauseOverlay;
        private IDisposable _livesSub;
        private IDisposable _levelSub;
        private IDisposable _stateSub;
        private int _lives = 3;
        private int _maxLives = 5;
        private int _level = 1;

        public void Configure(IEventBus eventBus, IGameStateMachine stateMachine, int lives, int maxLives, int level = 1)
        {
            _eventBus = eventBus;
            _stateMachine = stateMachine;
            _lives = lives;
            _maxLives = maxLives;
            _level = level < 1 ? 1 : level;
            EnsureUi();
            Subscribe();
            RefreshLives();
            RefreshLevel();
            RefreshPauseVisual();
        }

        private void OnDestroy()
        {
            _livesSub?.Dispose();
            _levelSub?.Dispose();
            _stateSub?.Dispose();
        }

        private void Subscribe()
        {
            _livesSub?.Dispose();
            _levelSub?.Dispose();
            _stateSub?.Dispose();
            if (_eventBus == null)
            {
                return;
            }

            _livesSub = _eventBus.Subscribe<LivesChangedEvent>(e =>
            {
                _lives = e.Current;
                _maxLives = e.Max;
                RefreshLives();
            });
            _levelSub = _eventBus.Subscribe<LevelStartedEvent>(e =>
            {
                _level = e.LevelNumber;
                RefreshLevel();
            });
            _stateSub = _eventBus.Subscribe<GameStateChangedEvent>(_ => RefreshPauseVisual());
        }

        private void EnsureUi()
        {
            if (_livesText != null)
            {
                return;
            }

            var canvasGo = new GameObject("GameplayHUD");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Нужен EventSystem для кнопки (New Input System)
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var moduleType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (moduleType != null)
                {
                    es.AddComponent(moduleType);
                }
                else
                {
                    es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            var bar = CreateRect("StatusBar", canvasGo.transform);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(0f, 120f);
            barRt.anchoredPosition = Vector2.zero;
            var barImg = bar.AddComponent<Image>();
            barImg.color = new Color(0.02f, 0.04f, 0.08f, 0.72f);

            var livesGo = CreateRect("Lives", bar.transform);
            var livesRt = livesGo.GetComponent<RectTransform>();
            livesRt.anchorMin = new Vector2(0f, 0f);
            livesRt.anchorMax = new Vector2(0.32f, 1f);
            livesRt.offsetMin = new Vector2(28f, 8f);
            livesRt.offsetMax = new Vector2(-4f, -8f);
            _livesText = livesGo.AddComponent<Text>();
            _livesText.font = ResolveFont();
            _livesText.fontSize = 36;
            _livesText.alignment = TextAnchor.MiddleLeft;
            _livesText.color = new Color(0.85f, 0.95f, 1f, 1f);
            _livesText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _livesText.verticalOverflow = VerticalWrapMode.Overflow;

            var levelGo = CreateRect("Level", bar.transform);
            var levelRt = levelGo.GetComponent<RectTransform>();
            levelRt.anchorMin = new Vector2(0.32f, 0f);
            levelRt.anchorMax = new Vector2(0.68f, 1f);
            levelRt.offsetMin = new Vector2(4f, 8f);
            levelRt.offsetMax = new Vector2(-4f, -8f);
            _levelText = levelGo.AddComponent<Text>();
            _levelText.font = ResolveFont();
            _levelText.fontSize = 40;
            _levelText.fontStyle = FontStyle.Bold;
            _levelText.alignment = TextAnchor.MiddleCenter;
            _levelText.color = new Color(0.95f, 0.98f, 1f, 1f);
            _levelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _levelText.verticalOverflow = VerticalWrapMode.Overflow;

            var pauseGo = CreateRect("PauseButton", bar.transform);
            var pauseRt = pauseGo.GetComponent<RectTransform>();
            pauseRt.anchorMin = new Vector2(1f, 0.5f);
            pauseRt.anchorMax = new Vector2(1f, 0.5f);
            pauseRt.pivot = new Vector2(1f, 0.5f);
            pauseRt.sizeDelta = new Vector2(200f, 80f);
            pauseRt.anchoredPosition = new Vector2(-28f, 0f);
            var pauseImg = pauseGo.AddComponent<Image>();
            pauseImg.color = new Color(0.15f, 0.35f, 0.55f, 0.95f);
            _pauseButton = pauseGo.AddComponent<Button>();
            _pauseButton.targetGraphic = pauseImg;
            _pauseButton.onClick.AddListener(OnPauseClicked);

            var pauseLabelGo = CreateRect("Label", pauseGo.transform);
            var pauseLabelRt = pauseLabelGo.GetComponent<RectTransform>();
            pauseLabelRt.anchorMin = Vector2.zero;
            pauseLabelRt.anchorMax = Vector2.one;
            pauseLabelRt.offsetMin = Vector2.zero;
            pauseLabelRt.offsetMax = Vector2.zero;
            _pauseLabel = pauseLabelGo.AddComponent<Text>();
            _pauseLabel.font = ResolveFont();
            _pauseLabel.fontSize = 36;
            _pauseLabel.alignment = TextAnchor.MiddleCenter;
            _pauseLabel.color = Color.white;
            _pauseLabel.text = "II";

            _pauseOverlay = CreateRect("PauseOverlay", canvasGo.transform);
            var overlayRt = _pauseOverlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var overlayImg = _pauseOverlay.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.45f);
            var overlayBtn = _pauseOverlay.AddComponent<Button>();
            overlayBtn.targetGraphic = overlayImg;
            overlayBtn.onClick.AddListener(OnPauseClicked);

            var overlayTextGo = CreateRect("PauseText", _pauseOverlay.transform);
            var otRt = overlayTextGo.GetComponent<RectTransform>();
            otRt.anchorMin = new Vector2(0.1f, 0.4f);
            otRt.anchorMax = new Vector2(0.9f, 0.6f);
            otRt.offsetMin = Vector2.zero;
            otRt.offsetMax = Vector2.zero;
            var ot = overlayTextGo.AddComponent<Text>();
            ot.font = ResolveFont();
            ot.fontSize = 64;
            ot.alignment = TextAnchor.MiddleCenter;
            ot.color = Color.white;
            ot.text = "ПАУЗА\nнажми чтобы продолжить";
            _pauseOverlay.SetActive(false);
        }

        private void OnPauseClicked()
        {
            if (_eventBus == null || _stateMachine == null)
            {
                return;
            }

            if (_stateMachine.CurrentState == GameState.Gameplay)
            {
                _eventBus.Publish(new RequestPauseEvent());
            }
            else if (_stateMachine.CurrentState == GameState.Pause)
            {
                _eventBus.Publish(new RequestResumeEvent());
            }
        }

        private void RefreshLives()
        {
            if (_livesText == null)
            {
                return;
            }

            _livesText.text = $"Lives  {_lives}/{_maxLives}";
        }

        private void RefreshLevel()
        {
            if (_levelText == null)
            {
                return;
            }

            _levelText.text = $"Level {_level}";
        }

        private void RefreshPauseVisual()
        {
            var paused = _stateMachine != null && _stateMachine.CurrentState == GameState.Pause;
            if (_pauseLabel != null)
            {
                _pauseLabel.text = paused ? "▶" : "II";
            }

            if (_pauseOverlay != null)
            {
                _pauseOverlay.SetActive(paused);
            }
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Font ResolveFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }
}
