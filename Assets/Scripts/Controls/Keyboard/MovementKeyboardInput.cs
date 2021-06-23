using UnityEngine;

namespace Controls.Keyboard
{
    [RequireComponent(typeof(Movement))]
    public sealed class MovementKeyboardInput : MonoBehaviour
    {
#if UNITY_EDITOR || UNITY_STANDALONE

        private void Update()
        {
            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");
            var extraDirection = new Vector3(horizontal, 0f, vertical);
            _movement.Direction += extraDirection;
        }

        private void Awake()
        {
            _movement = GetComponent<Movement>();
        }

        private Movement _movement;

#endif
    }
}