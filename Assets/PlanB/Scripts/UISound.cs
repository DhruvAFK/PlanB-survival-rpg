using UnityEngine;
using UnityEngine.EventSystems;

namespace PlanB
{
    public sealed class UISound : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData) { AudioFactory.Play("ui_hover", .22f); }
    }
}
