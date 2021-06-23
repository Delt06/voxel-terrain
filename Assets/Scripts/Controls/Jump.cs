using UnityEngine;

namespace Controls
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Jump : MonoBehaviour
    {
        [SerializeField] private LayerMask _layerMask = default;
        [SerializeField, Min(0f)] private float _maxDistance = 0.1f;
        [SerializeField, Min(0f)] private float _jumpHeight = 1.25f;
        [SerializeField] private Transform _origin = default;

        public void Try()
        {
            if (!Physics.Raycast(_origin.position, Vector3.down, _maxDistance, _layerMask)) return;
            var velocity = _rigidbody.velocity;
            velocity.y = JumpSpeed;
            _rigidbody.velocity = velocity;
        }

        private float JumpSpeed => Mathf.Sqrt(2 * G * _jumpHeight);

        private static float G => Mathf.Abs(Physics.gravity.y);

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private Rigidbody _rigidbody;

        private void OnDrawGizmos()
        {
            if (!_origin) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(_origin.position, Vector3.down * _maxDistance);
        }
    }
}