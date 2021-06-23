using Controls;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragMovementInput : MonoBehaviour, IDragHandler
{
    public Movement Movement;

    public void OnDrag(PointerEventData eventData)
    {
        Movement.Direction = eventData.delta.normalized;
    }

    private void LateUpdate()
    {
        Movement.Direction = Vector3.zero;
    }
}