using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KPSSGeography.UI
{
    /// <summary>Karusel alanındaki sürükleme, tekerlek ve tıklama olaylarını ileten görünmez yüzey.</summary>
    public class CarouselInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerClickHandler
    {
        public event Action<Vector2> DragBegan;
        public event Action<Vector2> Dragged;
        public event Action DragEnded;
        public event Action<float> Scrolled;
        public event Action<Vector2> Clicked;

        public void OnBeginDrag(PointerEventData e) => DragBegan?.Invoke(e.position);
        public void OnDrag(PointerEventData e) => Dragged?.Invoke(e.delta);
        public void OnEndDrag(PointerEventData e) => DragEnded?.Invoke();
        public void OnScroll(PointerEventData e) => Scrolled?.Invoke(e.scrollDelta.y);

        public void OnPointerClick(PointerEventData e)
        {
            if (!e.dragging) Clicked?.Invoke(e.position);
        }
    }
}
