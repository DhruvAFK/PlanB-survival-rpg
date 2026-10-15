using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlanB
{
    /// <summary>Shared quiet feedback for pointer and keyboard menu navigation.</summary>
    public sealed class UISound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        Selectable control;
        void Awake() { control = GetComponent<Selectable>(); }
        void Hover()
        {
            if (control == null) control = GetComponent<Selectable>();
            if (control != null && control.IsInteractable()) AudioFactory.Play("ui_hover", .18f);
        }
        public void OnPointerEnter(PointerEventData eventData) { Hover(); }
        public void OnSelect(BaseEventData eventData) { Hover(); }
    }
}
