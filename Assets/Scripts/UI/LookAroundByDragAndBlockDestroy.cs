using System.Collections.Generic;
using Controls;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class LookAroundByDragAndBlockDestroy : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerExitHandler,
        IPointerDownHandler
    {
        [SerializeField] private float _pixelsToAngles = 10f;
        [SerializeField] private bool _invertHorizontal = true;
        [SerializeField] private bool _invertVertical = true;
        [SerializeField, Min(0f)] private float _maxClickTimeForDestroy = 0.1f;
        [SerializeField, Min(0f)] private float _minDistanceInViewportToNoticeMovement = 0.01f;

        public LookAround LookAround;
        public RaycastBlockDestroy BlockDestroy;

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            foreach (var pointerState in _pointerStates.Values)
            {
                if (pointerState.Stationary && TimeNow > pointerState.ClickTime + _maxClickTimeForDestroy)
                    BlockDestroy.TryDestroyAtScreenPosition(pointerState.LastPosition, deltaTime);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_pointerStates.TryGetValue(eventData.pointerId, out var pointerState)) return;

            pointerState.LastPosition = eventData.position;

            var delta = eventData.delta;
            if (ToViewport(delta).magnitude >= _minDistanceInViewportToNoticeMovement) pointerState.Stationary = false;

            var yRotation = delta.x * _pixelsToAngles;
            if (_invertVertical) yRotation *= -1f;
            LookAround.RotateY(yRotation);

            var xRotation = delta.y * _pixelsToAngles;
            if (_invertHorizontal) xRotation *= -1f;
            LookAround.RotateX(xRotation);

            _pointerStates[eventData.pointerId] = pointerState;
        }

        private Vector2 ToViewport(Vector2 screenPosition) => _camera.ScreenToViewportPoint(screenPosition);

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pointerStates.TryGetValue(eventData.pointerId, out var pointerState)) return;
            if (!pointerState.Stationary) return;

            if (TimeNow <= pointerState.ClickTime + _maxClickTimeForDestroy)
                BlockDestroy.TryPlaceAtScreenPosition(eventData.position);

            _pointerStates.Remove(eventData.pointerId);
        }

        private static float TimeNow => Time.unscaledTime;

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerStates.Remove(eventData.pointerId);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerStates[eventData.pointerId] = new PointerState
            {
                ClickTime = TimeNow,
                Stationary = true,
                LastPosition = eventData.position,
            };
        }

        private void OnDisable()
        {
            _pointerStates.Clear();
        }

        private void Awake()
        {
            _camera = Camera.main;
        }

        private Camera _camera;
        private readonly Dictionary<int, PointerState> _pointerStates = new Dictionary<int, PointerState>();

        private struct PointerState
        {
            public float ClickTime;
            public bool Stationary;
            public Vector2 LastPosition;
        }
    }
}