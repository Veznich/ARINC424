using System;
using System.Collections.Generic;
using Arkanoid.Core;
using Arkanoid.Gameplay;
using UnityEngine;

namespace Arkanoid.UI
{
    /// <summary>
    /// Активные бонусы — ряд 3D-значков ПОД платформой.
    /// </summary>
    public sealed class PowerUpTimersHud : MonoBehaviour
    {
        private IEventBus _eventBus;
        private IDisposable _sub;
        private Transform _row;
        private readonly List<Slot> _slots = new List<Slot>(8);

        private struct Slot
        {
            public Transform Root;
            public Transform Icon;
            public Transform TimerBar;
            public PowerUpType Type;
        }

        public void Configure(IEventBus eventBus)
        {
            _eventBus = eventBus;
            EnsureRoot();
            // На случай если корень уже был со старой позицией
            if (_row != null)
            {
                _row.localPosition = new Vector3(-3.6f, PlayfieldLayout.PaddleY - 1.25f, -0.55f);
            }

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

        private void EnsureRoot()
        {
            if (_row != null)
            {
                return;
            }

            var go = new GameObject("PowerUpTimers3D");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(-3.6f, PlayfieldLayout.PaddleY - 1.25f, -0.55f);
            go.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
            _row = go.transform;
        }

        private void OnTimers(PowerUpTimersChangedEvent e)
        {
            EnsureRoot();
            var timers = e.Timers ?? Array.Empty<PowerUpTimerInfo>();

            while (_slots.Count > timers.Length)
            {
                var last = _slots[_slots.Count - 1];
                if (last.Root != null)
                {
                    Destroy(last.Root.gameObject);
                }

                _slots.RemoveAt(_slots.Count - 1);
            }

            while (_slots.Count < timers.Length)
            {
                _slots.Add(CreateSlot(_slots.Count));
            }

            for (var i = 0; i < timers.Length; i++)
            {
                var t = timers[i];
                var s = _slots[i];
                s.Root.localPosition = new Vector3(i * 1.2f, 0f, 0f);

                if (s.Type != t.Type || s.Icon.childCount == 0)
                {
                    s.Type = t.Type;
                    PowerUpIcon3D.Build(s.Icon, t.Type);
                    s.Icon.localScale = Vector3.one * 0.715f; // +30% к 0.55
                }

                // Полоска таймера под значком (масштаб по X = remaining)
                if (s.TimerBar != null)
                {
                    var n = Mathf.Clamp01(t.Normalized);
                    s.TimerBar.localScale = new Vector3(Mathf.Max(0.05f, n) * 0.7f, 0.08f, 0.12f);
                    var rend = s.TimerBar.GetComponent<MeshRenderer>();
                    if (rend != null && rend.sharedMaterial != null)
                    {
                        var col = PowerUpDrop.ColorFor(t.Type);
                        Arkanoid.Utils.RuntimeMaterialUtil.ApplyColor(rend.sharedMaterial, col);
                    }
                }

                _slots[i] = s;
            }
        }

        private Slot CreateSlot(int index)
        {
            var root = new GameObject("Slot" + index).transform;
            root.SetParent(_row, false);
            root.localPosition = new Vector3(index * 1.2f, 0f, 0f);

            var icon = new GameObject("Icon").transform;
            icon.SetParent(root, false);

            var barGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barGo.name = "TimerBar";
            barGo.transform.SetParent(root, false);
            barGo.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            barGo.transform.localScale = new Vector3(0.7f, 0.08f, 0.12f);
            var col = barGo.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            var mat = Arkanoid.Utils.RuntimeMaterialUtil.CreatePseudo3d(Color.white, 0.1f);
            var mr = barGo.GetComponent<MeshRenderer>();
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
            }

            return new Slot
            {
                Root = root,
                Icon = icon,
                TimerBar = barGo.transform,
                Type = (PowerUpType)255
            };
        }

        private void Update()
        {
            // Лёгкое вращение значков
            for (var i = 0; i < _slots.Count; i++)
            {
                var icon = _slots[i].Icon;
                if (icon != null)
                {
                    icon.Rotate(0f, 60f * Time.deltaTime, 0f, Space.Self);
                }
            }
        }
    }
}
