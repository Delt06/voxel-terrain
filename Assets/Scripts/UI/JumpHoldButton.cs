using Controls;
using UnityEngine;

namespace UI
{
    public sealed class JumpHoldButton : HoldButton
    {
        [SerializeField] private Jump _jump = default;

        private void Update()
        {
            if (IsHeld)
                _jump.Try();
        }
    }
}