using UnityEngine;

namespace Controls
{
    [RequireComponent(typeof(Movement))]
    public class Movement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _speed = 1f;

        public Vector3 Direction;

        public void AddForwardMotion(float motion)
        {
            Direction += Vector3.forward * motion;
        }

        public void AddSidewaysMotion(float motion)
        {
            Direction += Vector3.right * motion;
        }

        private void Update()
        {
            var direction = GetDirection();
            var velocity = direction * _speed;
            velocity.y = _rigidbody.velocity.y;
            _rigidbody.velocity = velocity;
            Direction = Vector3.zero;
        }

        private Vector3 GetDirection()
        {
            var direction = Direction;

            if (direction.magnitude > 1f)
                direction.Normalize();

            return _rigidbody.transform.TransformDirection(direction);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private Rigidbody _rigidbody;
    }
}