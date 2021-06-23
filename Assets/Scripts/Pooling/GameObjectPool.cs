using UnityEngine;

namespace Pooling
{
    public sealed class GameObjectPool : PoolBase<GameObject>
    {
        [SerializeField] private GameObject _prefab = default;

        public GameObject Prefab => _prefab;

        protected override bool IsBusy(GameObject @object) => @object.activeSelf;

        protected override void Initialize(GameObject @object, Vector3 position)
        {
            @object.transform.position = position;
            @object.SetActive(true);
        }

        protected override GameObject InstantiateDisabled(Transform parent)
        {
            var @object = Instantiate(_prefab, transform);
            @object.gameObject.SetActive(false);
            return @object;
        }
    }
}