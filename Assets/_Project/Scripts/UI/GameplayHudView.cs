using System;
using System.Collections.Generic;
using Arkanoid.Core;
using Arkanoid.Difficulty;
using Arkanoid.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Arkanoid.UI
{
    /// <summary>
    /// Верхний статус-бар + Info (пауза + справка). Иконки бонусов — крутящиеся 3D.
    /// </summary>
    public sealed class GameplayHudView : MonoBehaviour
    {
        private IEventBus _eventBus;
        private IGameStateMachine _stateMachine;
        private DifficultyDirector _difficulty;
        private Camera _uiCamera;

        private Text _livesText;
        private Text _levelText;
        private Button _infoButton;
        private GameObject _infoPanel;
        private Text _infoStatsText;
        private bool _infoOpen;
        private Canvas _hudCanvas;

        private readonly List<InfoIconSlot> _infoIcons = new List<InfoIconSlot>(8);
        private readonly List<float> _infoIconYaw = new List<float>(8);
        private Transform _infoIconsRoot;

        private IDisposable _livesSub;
        private IDisposable _levelSub;
        private IDisposable _stateSub;
        private IDisposable _diffSub;
        private IDisposable _statsSub;
        private int _lives = 3;
        private int _maxLives = 5;
        private int _level = 1;
        private int _firstTryClears;
        private Text _toastText;
        private float _toastUntil;

        private struct InfoIconSlot
        {
            public RectTransform Anchor;
            public Transform Visual;
        }

        public void Configure(
            IEventBus eventBus,
            IGameStateMachine stateMachine,
            int lives,
            int maxLives,
            int level = 1,
            DifficultyDirector difficulty = null,
            Camera uiCamera = null)
        {
            _eventBus = eventBus;
            _stateMachine = stateMachine;
            _difficulty = difficulty;
            _uiCamera = uiCamera != null ? uiCamera : Camera.main;
            _lives = lives;
            _maxLives = maxLives;
            _level = level < 1 ? 1 : level;
            if (_difficulty != null)
            {
                _firstTryClears = _difficulty.FirstTryClears;
            }

            EnsureUi();
            BindCanvasCamera();
            Subscribe();
            RefreshLives();
            RefreshLevel();
            RefreshInfoVisibility();
        }

        private void OnDestroy()
        {
            _livesSub?.Dispose();
            _levelSub?.Dispose();
            _stateSub?.Dispose();
            _diffSub?.Dispose();
            _statsSub?.Dispose();
        }

        private void BindCanvasCamera()
        {
            if (_hudCanvas == null)
            {
                return;
            }

            if (_uiCamera == null)
            {
                _uiCamera = Camera.main;
            }

            _hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _hudCanvas.worldCamera = _uiCamera;
            _hudCanvas.planeDistance = 1.4f;
        }

        private void Subscribe()
        {
            _livesSub?.Dispose();
            _levelSub?.Dispose();
            _stateSub?.Dispose();
            _diffSub?.Dispose();
            _statsSub?.Dispose();
            if (_eventBus == null)
            {
                return;
            }

            _livesSub = _eventBus.Subscribe<LivesChangedEvent>(e =>
            {
                _lives = e.Current;
                _maxLives = e.Max;
                RefreshLives();
                if (_infoOpen)
                {
                    RefreshInfoStats();
                }
            });
            _levelSub = _eventBus.Subscribe<LevelStartedEvent>(e =>
            {
                _level = e.LevelNumber;
                RefreshLevel();
                if (_infoOpen)
                {
                    RefreshInfoStats();
                }
            });
            _stateSub = _eventBus.Subscribe<GameStateChangedEvent>(_ => RefreshInfoVisibility());
            _diffSub = _eventBus.Subscribe<DifficultyChangedEvent>(OnDifficultyChanged);
            _statsSub = _eventBus.Subscribe<SessionStatsChangedEvent>(e =>
            {
                _firstTryClears = e.FirstTryClears;
                if (_infoOpen)
                {
                    RefreshInfoStats();
                }
            });
        }

        private void OnDifficultyChanged(DifficultyChangedEvent e)
        {
            if (string.IsNullOrEmpty(e.ToastMessage) || _toastText == null)
            {
                return;
            }

            _toastText.text = e.ToastMessage;
            _toastText.color = e.Bias == DifficultyBias.Assist
                ? new Color(0.45f, 1f, 0.65f, 1f)
                : new Color(1f, 0.55f, 0.35f, 1f);
            _toastText.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + 1.6f;
        }

        private void Update()
        {
            if (_toastText != null && _toastText.gameObject.activeSelf && Time.unscaledTime >= _toastUntil)
            {
                _toastText.gameObject.SetActive(false);
            }

            if (!_infoOpen)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            for (var i = 0; i < _infoIconYaw.Count; i++)
            {
                _infoIconYaw[i] += 110f * dt;
            }
        }

        private void LateUpdate()
        {
            if (!_infoOpen || _uiCamera == null)
            {
                return;
            }

            SyncInfoIconPositions();
        }

        private void SyncInfoIconPositions()
        {
            var cam = _uiCamera;
            var forward = cam.transform.forward;
            var up = cam.transform.up;
            for (var i = 0; i < _infoIcons.Count; i++)
            {
                var slot = _infoIcons[i];
                if (slot.Anchor == null || slot.Visual == null)
                {
                    continue;
                }

                slot.Visual.position = slot.Anchor.position - forward * 0.22f;
                var yaw = i < _infoIconYaw.Count ? _infoIconYaw[i] : 0f;
                slot.Visual.rotation = Quaternion.LookRotation(forward, up) *
                                       Quaternion.Euler(12f, yaw, 18f);
            }
        }

        private void EnsureUi()
        {
            if (_livesText != null)
            {
                BindCanvasCamera();
                return;
            }

            var canvasGo = new GameObject("GameplayHUD");
            canvasGo.transform.SetParent(transform, false);
            _hudCanvas = canvasGo.AddComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _hudCanvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            BindCanvasCamera();

            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var moduleType = Type.GetType(
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

            var infoGo = CreateRect("InfoButton", bar.transform);
            var infoRt = infoGo.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(1f, 0.5f);
            infoRt.anchorMax = new Vector2(1f, 0.5f);
            infoRt.pivot = new Vector2(1f, 0.5f);
            infoRt.sizeDelta = new Vector2(200f, 80f);
            infoRt.anchoredPosition = new Vector2(-28f, 0f);
            var infoImg = infoGo.AddComponent<Image>();
            infoImg.color = new Color(0.12f, 0.42f, 0.62f, 0.95f);
            _infoButton = infoGo.AddComponent<Button>();
            _infoButton.targetGraphic = infoImg;
            _infoButton.onClick.AddListener(OnInfoClicked);

            var infoLabelGo = CreateRect("Label", infoGo.transform);
            var infoLabelRt = infoLabelGo.GetComponent<RectTransform>();
            infoLabelRt.anchorMin = Vector2.zero;
            infoLabelRt.anchorMax = Vector2.one;
            infoLabelRt.offsetMin = Vector2.zero;
            infoLabelRt.offsetMax = Vector2.zero;
            var infoButtonLabel = infoLabelGo.AddComponent<Text>();
            infoButtonLabel.font = ResolveFont();
            infoButtonLabel.fontSize = 40;
            infoButtonLabel.fontStyle = FontStyle.Bold;
            infoButtonLabel.alignment = TextAnchor.MiddleCenter;
            infoButtonLabel.color = Color.white;
            infoButtonLabel.text = "INFO";

            BuildInfoPanel(canvasGo.transform);
            EnsureInfoIconsRoot();

            var toastGo = CreateRect("DifficultyToast", canvasGo.transform);
            var toastRt = toastGo.GetComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0.15f, 0.72f);
            toastRt.anchorMax = new Vector2(0.85f, 0.82f);
            toastRt.offsetMin = Vector2.zero;
            toastRt.offsetMax = Vector2.zero;
            _toastText = toastGo.AddComponent<Text>();
            _toastText.font = ResolveFont();
            _toastText.fontSize = 48;
            _toastText.fontStyle = FontStyle.Bold;
            _toastText.alignment = TextAnchor.MiddleCenter;
            _toastText.color = Color.white;
            _toastText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _toastText.verticalOverflow = VerticalWrapMode.Overflow;
            toastGo.SetActive(false);
        }

        private void EnsureInfoIconsRoot()
        {
            if (_infoIconsRoot != null)
            {
                return;
            }

            var go = new GameObject("InfoPowerUpIcons3D");
            go.transform.SetParent(transform, false);
            _infoIconsRoot = go.transform;
            go.SetActive(false);
        }

        private void BuildInfoPanel(Transform canvas)
        {
            _infoPanel = CreateRect("InfoPanel", canvas);
            var panelRt = _infoPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var dim = _infoPanel.AddComponent<Image>();
            dim.color = new Color(0.01f, 0.02f, 0.05f, 0.82f);

            var card = CreateRect("Card", _infoPanel.transform);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.06f, 0.08f);
            cardRt.anchorMax = new Vector2(0.94f, 0.88f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.06f, 0.1f, 0.16f, 0.98f);

            var titleGo = CreateRect("Title", card.transform);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.9f);
            titleRt.anchorMax = new Vector2(0.95f, 0.98f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            var title = titleGo.AddComponent<Text>();
            title.font = ResolveFont();
            title.fontSize = 64; // 75 −15%
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.9f, 0.96f, 1f);
            title.text = "INFO";

            var statsGo = CreateRect("Stats", card.transform);
            var statsRt = statsGo.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.06f, 0.72f);
            statsRt.anchorMax = new Vector2(0.94f, 0.9f);
            statsRt.offsetMin = Vector2.zero;
            statsRt.offsetMax = Vector2.zero;
            _infoStatsText = statsGo.AddComponent<Text>();
            _infoStatsText.font = ResolveFont();
            _infoStatsText.fontSize = 46; // 54 −15%
            _infoStatsText.alignment = TextAnchor.UpperLeft;
            _infoStatsText.color = new Color(0.85f, 0.92f, 1f);
            _infoStatsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _infoStatsText.verticalOverflow = VerticalWrapMode.Overflow;
            _infoStatsText.lineSpacing = 1.15f;

            var listTitleGo = CreateRect("BonusesTitle", card.transform);
            var listTitleRt = listTitleGo.GetComponent<RectTransform>();
            listTitleRt.anchorMin = new Vector2(0.06f, 0.66f);
            listTitleRt.anchorMax = new Vector2(0.94f, 0.72f);
            listTitleRt.offsetMin = Vector2.zero;
            listTitleRt.offsetMax = Vector2.zero;
            var listTitle = listTitleGo.AddComponent<Text>();
            listTitle.font = ResolveFont();
            listTitle.fontSize = 43; // 51 −15%
            listTitle.fontStyle = FontStyle.Bold;
            listTitle.alignment = TextAnchor.MiddleLeft;
            listTitle.color = new Color(0.7f, 0.85f, 1f);
            listTitle.text = "Бонусы";

            var listRoot = CreateRect("BonusList", card.transform);
            var listRt = listRoot.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.05f, 0.14f);
            listRt.anchorMax = new Vector2(0.95f, 0.66f);
            listRt.offsetMin = Vector2.zero;
            listRt.offsetMax = Vector2.zero;

            var entries = new[]
            {
                (PowerUpType.Fireball, "Fireball", "Мяч пробивает блоки"),
                (PowerUpType.WidePaddle, "Wide", "Платформа шире"),
                (PowerUpType.SlowTime, "Slow", "Замедление мяча"),
                (PowerUpType.MultiBall, "Multi", "+2 мяча"),
                (PowerUpType.Laser, "Laser", "Луч вверх с платформы"),
                (PowerUpType.Shield, "Shield", "Спасает 1 раз от потери"),
                (PowerUpType.Magnet, "Magnet", "Мяч липнет к платформе"),
                (PowerUpType.ExtraLife, "+1 Life", "Дополнительная жизнь")
            };

            EnsureInfoIconsRoot();
            _infoIcons.Clear();
            _infoIconYaw.Clear();
            for (var i = 0; i < entries.Length; i++)
            {
                var anchor = CreateBonusRow(
                    listRoot.transform, i, entries.Length, entries[i].Item2, entries[i].Item3);
                var visual = CreateSpinningIcon(entries[i].Item1, i);
                _infoIcons.Add(new InfoIconSlot { Anchor = anchor, Visual = visual });
                _infoIconYaw.Add(i * 37f);
            }

            var closeGo = CreateRect("CloseButton", card.transform);
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.25f, 0.03f);
            closeRt.anchorMax = new Vector2(0.75f, 0.12f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.18f, 0.55f, 0.78f, 1f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(OnCloseInfoClicked);

            var closeLabelGo = CreateRect("Label", closeGo.transform);
            var closeLabelRt = closeLabelGo.GetComponent<RectTransform>();
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;
            var closeLabel = closeLabelGo.AddComponent<Text>();
            closeLabel.font = ResolveFont();
            closeLabel.fontSize = 52; // 61 −15%
            closeLabel.fontStyle = FontStyle.Bold;
            closeLabel.alignment = TextAnchor.MiddleCenter;
            closeLabel.color = Color.white;
            closeLabel.text = "ЗАКРЫТЬ";

            _infoPanel.SetActive(false);
        }

        private Transform CreateSpinningIcon(PowerUpType type, int index)
        {
            EnsureInfoIconsRoot();
            var go = new GameObject("Icon3D_" + type);
            go.transform.SetParent(_infoIconsRoot, false);
            go.transform.localScale = Vector3.one * PowerUpDrop.VisualScale;
            PowerUpIcon3D.Build(go.transform, type);
            return go.transform;
        }

        private static RectTransform CreateBonusRow(
            Transform parent,
            int index,
            int total,
            string title,
            string desc)
        {
            var rowH = 1f / total;
            var yMax = 1f - index * rowH;
            var yMin = yMax - rowH;

            var row = CreateRect("Row_" + title, parent);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, yMin);
            rowRt.anchorMax = new Vector2(1f, yMax);
            rowRt.offsetMin = new Vector2(0f, 4f);
            rowRt.offsetMax = new Vector2(0f, -4f);

            // Якорь под 3D-иконку (прозрачный слот)
            var icon = CreateRect("IconAnchor", row.transform);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.15f);
            iconRt.anchorMax = new Vector2(0f, 0.85f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(120f, 0f);
            iconRt.anchoredPosition = new Vector2(56f, 0f);

            var textGo = CreateRect("Text", row.transform);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(130f, 0f);
            textRt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = ResolveFont();
            text.fontSize = 41; // 48 −15%
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.9f, 0.95f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = title + " — " + desc;

            return iconRt;
        }

        private void OnInfoClicked()
        {
            if (_eventBus == null || _stateMachine == null)
            {
                return;
            }

            if (_stateMachine.CurrentState == GameState.Gameplay)
            {
                OpenInfo();
                _eventBus.Publish(new RequestPauseEvent());
            }
            else if (_stateMachine.CurrentState == GameState.Pause)
            {
                if (_infoOpen)
                {
                    CloseInfoAndResume();
                }
                else
                {
                    OpenInfo();
                }
            }
        }

        private void OnCloseInfoClicked()
        {
            CloseInfoAndResume();
        }

        private void OpenInfo()
        {
            _infoOpen = true;
            RefreshInfoStats();
            if (_infoPanel != null)
            {
                _infoPanel.SetActive(true);
            }

            if (_infoIconsRoot != null)
            {
                _infoIconsRoot.gameObject.SetActive(true);
            }

            Canvas.ForceUpdateCanvases();
            SyncInfoIconPositions();
        }

        private void CloseInfoAndResume()
        {
            _infoOpen = false;
            if (_infoPanel != null)
            {
                _infoPanel.SetActive(false);
            }

            if (_infoIconsRoot != null)
            {
                _infoIconsRoot.gameObject.SetActive(false);
            }

            if (_eventBus != null && _stateMachine != null &&
                _stateMachine.CurrentState == GameState.Pause)
            {
                _eventBus.Publish(new RequestResumeEvent());
            }
        }

        private void RefreshInfoStats()
        {
            if (_infoStatsText == null)
            {
                return;
            }

            var firstTry = _difficulty != null
                ? Mathf.Max(_firstTryClears, _difficulty.FirstTryClears)
                : _firstTryClears;
            _infoStatsText.text =
                $"Уровень: {_level}\n" +
                $"Жизни: {_lives} / {_maxLives}\n" +
                $"Пройдено без потери жизни: {firstTry}";
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

        private void RefreshInfoVisibility()
        {
            var paused = _stateMachine != null && _stateMachine.CurrentState == GameState.Pause;
            if (!paused)
            {
                _infoOpen = false;
                if (_infoPanel != null)
                {
                    _infoPanel.SetActive(false);
                }

                if (_infoIconsRoot != null)
                {
                    _infoIconsRoot.gameObject.SetActive(false);
                }
            }
            else if (_infoOpen)
            {
                if (_infoPanel != null)
                {
                    _infoPanel.SetActive(true);
                }

                if (_infoIconsRoot != null)
                {
                    _infoIconsRoot.gameObject.SetActive(true);
                }

                RefreshInfoStats();
            }
            else
            {
                OpenInfo();
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
