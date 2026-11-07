using KPSSGeography.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KPSSGeography.Map
{
    /// <summary>
    /// Ortografik harita kamerası.
    /// Tekerlek: imlecin olduğu noktaya yakınlaştır/uzaklaştır • Sağ tık sürükle: kaydır • Orta tık: görünümü sıfırla.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MapCameraController : MonoBehaviour
    {
        [Tooltip("Haritanın ekranda kapladığı dikey alan (üstte HUD için boşluk bırakılır).")]
        [SerializeField] float verticalFill = 0.76f;
        [Tooltip("Haritanın dikey merkezinin ekrandaki konumu (0 alt, 1 üst).")]
        [SerializeField] float verticalCenter = 0.44f;
        [SerializeField] float horizontalFill = 0.96f;
        [SerializeField] float maxZoomIn = 6f;
        [SerializeField] float zoomStep = 1.25f;
        [SerializeField] float smoothing = 14f;

        Camera cam;
        Rect bounds;
        float fitSize;
        Vector2 fitPosition;
        float targetSize;
        Vector2 targetPos;
        Vector2 currentPos;
        float lastAspect;
        bool isDefaultView = true;

        float shakeTime;
        float shakeDuration;
        float shakeAmplitude;

        public void Setup(Rect mapBounds)
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            bounds = mapBounds;
            ComputeFit();
            SnapToDefault();
        }

        public void ResetView()
        {
            isDefaultView = true;
            targetSize = fitSize;
            targetPos = fitPosition;
        }

        public void Shake(float amplitude = 0.08f, float duration = 0.25f)
        {
            shakeAmplitude = amplitude;
            shakeDuration = duration;
            shakeTime = duration;
        }

        void SnapToDefault()
        {
            ResetView();
            currentPos = targetPos;
            cam.orthographicSize = targetSize;
            ApplyTransform(Vector2.zero);
        }

        void ComputeFit()
        {
            lastAspect = cam.aspect;
            float byHeight = bounds.height / (2f * verticalFill);
            float byWidth = bounds.width / (2f * cam.aspect * horizontalFill);
            fitSize = Mathf.Max(byHeight, byWidth);
            fitPosition = new Vector2(bounds.center.x, bounds.center.y + (0.5f - verticalCenter) * 2f * fitSize);
        }

        void Update()
        {
            if (cam == null) return;

            if (!Mathf.Approximately(cam.aspect, lastAspect))
            {
                ComputeFit();
                if (isDefaultView) ResetView();
            }

            HandleInput();

            float k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, k);
            currentPos = Vector2.Lerp(currentPos, targetPos, k);

            Vector2 shake = Vector2.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float strength = shakeAmplitude * Mathf.Clamp01(shakeTime / shakeDuration) * (cam.orthographicSize / fitSize);
                shake = Random.insideUnitCircle * strength;
            }
            ApplyTransform(shake);
        }

        void HandleInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screen = mouse.position.ReadValue();
            bool overUI = PointerUtil.IsOverUI();

            // Yakınlaştırma (imleç altındaki nokta sabit kalır)
            float scroll = mouse.scroll.ReadValue().y;
            if (!overUI && Mathf.Abs(scroll) > 0.01f)
            {
                Vector2 before = ScreenToWorld(screen, targetPos, targetSize);
                float newSize = scroll > 0 ? targetSize / zoomStep : targetSize * zoomStep;
                newSize = Mathf.Clamp(newSize, fitSize / maxZoomIn, fitSize);
                Vector2 after = ScreenToWorld(screen, targetPos, newSize);
                targetSize = newSize;
                targetPos += before - after;
                if (Mathf.Approximately(newSize, fitSize)) { targetPos = fitPosition; isDefaultView = true; }
                else isDefaultView = false;
            }

            // Kaydırma (sağ ya da orta tuş basılıyken sürükle)
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (delta.sqrMagnitude > 0f)
                {
                    float unitsPerPixel = 2f * cam.orthographicSize / Screen.height;
                    Vector2 move = delta * unitsPerPixel;
                    targetPos -= move;
                    currentPos -= move;
                    isDefaultView = false;
                }
            }

            if (mouse.middleButton.wasPressedThisFrame && !overUI) ResetView();

            ClampTarget();
        }

        void ClampTarget()
        {
            if (isDefaultView) return;
            float marginY = targetSize * 0.5f;
            float marginX = targetSize * cam.aspect * 0.5f;
            targetPos.x = Mathf.Clamp(targetPos.x, bounds.xMin - marginX * 0.2f, bounds.xMax + marginX * 0.2f);
            targetPos.y = Mathf.Clamp(targetPos.y, bounds.yMin - marginY * 0.2f, bounds.yMax + marginY * 0.6f);
        }

        Vector2 ScreenToWorld(Vector2 screen, Vector2 camPos, float size)
        {
            float nx = screen.x / Screen.width - 0.5f;
            float ny = screen.y / Screen.height - 0.5f;
            return camPos + new Vector2(nx * 2f * size * cam.aspect, ny * 2f * size);
        }

        void ApplyTransform(Vector2 offset)
        {
            transform.position = new Vector3(currentPos.x + offset.x, currentPos.y + offset.y, -10f);
        }
    }
}
