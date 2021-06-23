using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IDragHandler
    {
        public bool IsHeld => _pointerId != null;

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerId ??= eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _pointerId ??= eventData.pointerId;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pointerId == eventData.pointerId)
                _pointerId = null;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pointerId == eventData.pointerId)
                _pointerId = null;
        }

        protected virtual void OnDisable()
        {
            _pointerId = null;
        }

        private int? _pointerId;
    }
}