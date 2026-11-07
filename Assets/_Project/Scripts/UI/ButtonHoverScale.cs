using UnityEngine;
using UnityEngine.EventSystems;

namespace KPSSGeography.UI
{
    /// <summary>Fare üzerine gelince butonu hafifçe büyütür.</summary>
    public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float hoverScale = 1.06f;
        float target = 1f;

        public void OnPointerEnter(PointerEventData e) => target = hoverScale;
        public void OnPointerExit(PointerEventData e) => target = 1f;

        void OnDisable()
        {
            target = 1f;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
