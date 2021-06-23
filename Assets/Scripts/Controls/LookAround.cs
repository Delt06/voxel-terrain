using UnityEngine;

namespace Controls
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class LookAround : MonoBehaviour
    {
        [SerializeField, Range(0f, 90f)] private float _maxXRotation = 80f;
        [SerializeField] private Transform _xRotation = default;

        public void RotateX(float degrees)
        {
            _xRotation.Rotate(Vector3.right, degrees, Space.Self);
            var angles = _xRotation.eulerAngles;
            angles.x = NormalizeAngle(angles.x);
            angles.x = Mathf.Clamp(angles.x, -_maxXRotation, _maxXRotation);
            _xRotation.eulerAngles = angles;
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
            {
                angle -= 360f;
            }

            while (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }

        public void RotateY(float degrees)
        {
            _rigidbody.rotation = Quaternion.AngleAxis(degrees, Vector3.up) * _rigidbody.rotation;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private Rigidbody _rigidbody;
    }
}