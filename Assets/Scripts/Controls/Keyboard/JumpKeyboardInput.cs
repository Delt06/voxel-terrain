using UnityEngine;

namespace Controls.Keyboard
{
    [RequireComponent(typeof(Jump))]
    public sealed class JumpKeyboardInput : MonoBehaviour
    {
#if UNITY_EDITOR || UNITY_STANDALONE

        private void Update()
        {
            if (Input.GetButton("Jump"))
                _jump.Try();
        }

        private void Awake()
        {
            _jump = GetComponent<Jump>();
        }

        private Jump _jump;

#endif
    }
}