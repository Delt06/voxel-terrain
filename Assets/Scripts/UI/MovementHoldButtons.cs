using System;
using Controls;
using UnityEngine;

namespace UI
{
    public sealed class MovementHoldButtons : MonoBehaviour
    {
        [SerializeField] private MovementButton[] _movementButtons = default;
        [SerializeField] private Movement _movement = default;

        private void Update()
        {
            var direction = Vector3.zero;

            foreach (var movementButton in _movementButtons)
            {
                movementButton.ContributeTo(ref direction);
            }

            _movement.Direction += direction;
        }

        [Serializable]
        private struct MovementButton
        {
            public HoldButton HoldButton;
            public Vector3 Motion;

            public void ContributeTo(ref Vector3 totalDirection)
            {
                if (HoldButton.IsHeld)
                    totalDirection += Motion;
            }
        }
    }
}