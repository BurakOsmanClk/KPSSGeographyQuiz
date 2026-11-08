using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Arayüzü her ekranda taşmadan ve üst üste binmeden yerleştirir:
    /// • İçerik, cihazın güvenli alanına (çentik, kamera deliği, alt hareket çubuğu dışı) oturan bir alt kökte durur.
    /// • Ölçek, güvenli alanın her zaman en az <see cref="Reference"/> (1920×1080) birim olacağı şekilde hesaplanır
    ///   ("Expand" mantığı, ama güvenli alana göre). Böylece 1920×1080'e göre tasarlanan yerleşimler hiçbir
    ///   telefonda, tablette ya da pencere boyutunda çakışmaz; fazla alan kenarlarda boşluk olarak kalır.
    /// Ekran boyutu, yönü ya da güvenli alan değişince kendini yeniden hesaplar.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class SafeAreaFitter : MonoBehaviour
    {
        public static readonly Vector2 Reference = new Vector2(1920f, 1080f);

        CanvasScaler scaler;
        RectTransform content;
        Rect lastSafe;
        Vector2Int lastScreen;

        /// <summary>Kanvasın altına güvenli alan kökünü kurar ve döndürür (tüm ekranlar bunun altına kurulur).</summary>
        public RectTransform Initialize()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var go = new GameObject("SafeArea", typeof(RectTransform));
            go.layer = gameObject.layer;
            content = (RectTransform)go.transform;
            content.SetParent(transform, false);
            Refresh(true);
            return content;
        }

        void Update() => Refresh(false);

        void Refresh(bool force)
        {
            if (content == null || scaler == null) return;
            Rect safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == lastSafe && size == lastScreen) return;
            lastSafe = safe;
            lastScreen = size;
            if (size.x <= 0 || size.y <= 0) return;
            if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, size.x, size.y);

            // Güvenli alan en az 1920×1080 birim olacak şekilde ölçek
            scaler.scaleFactor = Mathf.Max(0.01f, Mathf.Min(safe.width / Reference.x, safe.height / Reference.y));

            content.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            content.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
