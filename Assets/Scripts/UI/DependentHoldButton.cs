using System;
using UnityEngine;

namespace UI
{
    public sealed class DependentHoldButton : MonoBehaviour
    {
        [SerializeField] private HoldButton _dependent = default;
        [SerializeField] private DependenciesMode _dependenciesMode = DependenciesMode.All;
        [SerializeField] private HoldButton[] _dependencies = default;
        [SerializeField] private bool _activeWhenHeld = true;

        private void Update()
        {
            var active = DependenciesAreMet || _dependent.IsHeld ? _activeWhenHeld : !_activeWhenHeld;
            SetActive(active);
        }

        private void SetActive(bool active)
        {
            var dependentGameObject = _dependent.gameObject;
            if (dependentGameObject.activeSelf != active)
                dependentGameObject.SetActive(active);
        }

        private bool DependenciesAreMet => _dependenciesMode switch
        {
            DependenciesMode.All => AllButtonsAreHeld,
            DependenciesMode.Any => AnyButtonIsHeld,
            _ => throw new ArgumentOutOfRangeException(nameof(_dependenciesMode)),
        };

        private bool AllButtonsAreHeld
        {
            get
            {
                foreach (var button in _dependencies)
                {
                    if (!button.IsHeld)
                        return false;
                }

                return true;
            }
        }

        private bool AnyButtonIsHeld
        {
            get
            {
                foreach (var button in _dependencies)
                {
                    if (button.IsHeld)
                        return true;
                }

                return false;
            }
        }

        private enum DependenciesMode
        {
            All,
            Any,
        }
    }
}