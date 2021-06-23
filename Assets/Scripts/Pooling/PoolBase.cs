using System.Collections.Generic;
using UnityEngine;

namespace Pooling
{
    public abstract class PoolBase<T> : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int _initialCapacity = 100;

        public T GetObject(Vector3 position)
        {
            var @object = FindFreeObject();
            Initialize(@object, position);
            return @object;
        }

        protected abstract void Initialize(T @object, Vector3 position);

        private T FindFreeObject()
        {
            foreach (var @object in _objects)
            {
                if (IsBusy(@object)) continue;
                return @object;
            }

            return CreateNewObject();
        }

        protected abstract bool IsBusy(T @object);

        public void IncreaseCapacityTo(int newCapacity)
        {
            _objects.Capacity = Mathf.Max(_objects.Capacity, newCapacity);

            while (_objects.Count < newCapacity)
            {
                CreateNewObject();
            }
        }

        protected virtual void Awake()
        {
            _objects = new List<T>(_initialCapacity);
            IncreaseCapacityTo(_initialCapacity);
        }

        private T CreateNewObject()
        {
            var @object = InstantiateDisabled(transform);
            _objects.Add(@object);
            return @object;
        }

        protected abstract T InstantiateDisabled(Transform parent);

        private List<T> _objects;
    }
}