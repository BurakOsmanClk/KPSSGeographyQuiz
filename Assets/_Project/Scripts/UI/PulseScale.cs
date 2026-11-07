using UnityEngine;

namespace KPSSGeography.UI
{
    /// <summary>Birincil butonu dikkat çekmek için hafifçe "nefes aldırır".</summary>
    public class PulseScale : MonoBehaviour
    {
        public float amount = 0.04f;
        public float speed = 2.4f;

        void Awake()
        {
            // Üzerine gelince büyütme ile çakışmasın
            var hover = GetComponent<ButtonHoverScale>();
            if (hover != null) hover.enabled = false;
        }

        void Update()
        {
            float s = 1f + amount * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
