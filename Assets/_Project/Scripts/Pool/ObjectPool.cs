using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arkanoid.Pool
{
    /// <summary>
    /// Простой пул компонентов. Без Instantiate/Destroy в горячем цикле после Prewarm.
    /// </summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _free = new Stack<T>(64);
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        public ObjectPool(
            T prefab,
            Transform parent,
            int prewarmCount = 0,
            Action<T> onGet = null,
            Action<T> onRelease = null)
        {
            _prefab = prefab;
            _parent = parent;
            _onGet = onGet;
            _onRelease = onRelease;

            for (var i = 0; i < prewarmCount; i++)
            {
                _free.Push(CreateInstance());
            }
        }

        public T Get()
        {
            var item = _free.Count > 0 ? _free.Pop() : CreateInstance();
            item.gameObject.SetActive(true);
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null)
            {
                return;
            }

            _onRelease?.Invoke(item);
            item.gameObject.SetActive(false);
            if (item.transform.parent != _parent)
            {
                item.transform.SetParent(_parent, false);
            }

            _free.Push(item);
        }

        public void ReleaseAll(IList<T> active)
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                Release(active[i]);
            }

            active.Clear();
        }

        private T CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
