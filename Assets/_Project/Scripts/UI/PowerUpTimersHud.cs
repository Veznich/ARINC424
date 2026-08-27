using System;
using Arkanoid.Core;
using Arkanoid.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Arkanoid.UI
{
    /// <summary>Иконки активных бонусов + fill-таймер (низ слева).</summary>
    public sealed class PowerUpTimersHud : MonoBehaviour
    {
        private IEventBus _eventBus;
        private IDisposable _sub;
        private RectTransform _row;
        private Font _font;
        private readonly System.Collections.Generic.List<Slot> _slots =
            new System.Collections.Generic.List<Slot>(8);

        private struct Slot
        {
            public GameObject Root;
            public Image Fill;
            public Text Label;
            public PowerUpType Type;
        }

        public void Configure(IEventBus eventBus)
        {
            _eventBus = eventBus;
            EnsureUi();
            _sub?.Dispose();
            if (_eventBus != null)
            {
                _sub = _eventBus.Subscribe<PowerUpTimersChangedEvent>(OnTimers);
            }
        }

        private void OnDestroy()
        {
            _sub?.Dispose();
        }

        private void EnsureUi()
        {
            if (_row != null)
            {
                return;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasGo = new GameObject("PowerUpTimersHUD");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var rowGo = new GameObject("Row", typeof(RectTransform));
            rowGo.transform.SetParent(canvasGo.transform, false);
            _row = rowGo.GetComponent<RectTransform>();
            _row.anchorMin = new Vector2(0f, 0f);
            _row.anchorMax = new Vector2(0f, 0f);
            _row.pivot = new Vector2(0f, 0f);
            _row.anchoredPosition = new Vector2(28f, 36f);
            _row.sizeDelta = new Vector2(700f, 90f);
            var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            layout.padding = new RectOffset(0, 0, 0, 0);
        }

        private void OnTimers(PowerUpTimersChangedEvent e)
        {
            EnsureUi();
            var timers = e.Timers ?? Array.Empty<PowerUpTimerInfo>();

            while (_slots.Count > timers.Length)
            {
                var last = _slots[_slots.Count - 1];
                if (last.Root != null)
                {
                    Destroy(last.Root);
                }

                _slots.RemoveAt(_slots.Count - 1);
            }

            while (_slots.Count < timers.Length)
            {
                _slots.Add(CreateSlot());
            }

            for (var i = 0; i < timers.Length; i++)
            {
                var t = timers[i];
                var s = _slots[i];
                s.Type = t.Type;
                if (s.Label != null)
                {
                    s.Label.text = ShortName(t.Type);
                    s.Label.color = Color.white;
                }

                if (s.Fill != null)
                {
                    s.Fill.color = PowerUpDrop.ColorFor(t.Type);
                    s.Fill.fillAmount = Mathf.Clamp01(t.Normalized);
                }

                _slots[i] = s;
            }
        }

        private Slot CreateSlot()
        {
            var go = new GameObject("Slot", typeof(RectTransform));
            go.transform.SetParent(_row, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(88f, 88f);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 88f;
            le.minHeight = 88f;
            le.preferredWidth = 88f;
            le.preferredHeight = 88f;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.12f, 0.85f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(go.transform, false);
            var frt = fillGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(6f, 6f);
            frt.offsetMax = new Vector2(-6f, -6f);
            var fill = fillGo.AddComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = false;
            fill.color = Color.cyan;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.font = _font;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            return new Slot { Root = go, Fill = fill, Label = label };
        }

        private static string ShortName(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Fireball: return "FIRE";
                case PowerUpType.WidePaddle: return "WIDE";
                case PowerUpType.SlowTime: return "SLOW";
                case PowerUpType.MultiBall: return "MULTI";
                case PowerUpType.Laser: return "LASER";
                case PowerUpType.Shield: return "SHIELD";
                case PowerUpType.Magnet: return "MAG";
                case PowerUpType.ExtraLife: return "LIFE";
                default: return "?";
            }
        }
    }
}
