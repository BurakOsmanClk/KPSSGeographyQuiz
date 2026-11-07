using KPSSGeography.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KPSSGeography.Map
{
    /// <summary>
    /// Ortografik harita kamerası. İki mod:
    /// • Vitrin (menüler): harita yavaşça süzülür, giriş kapalıdır.
    /// • Etkileşimli (oyun): seçili bölgeye odaklanır. Tekerlek imlecin olduğu yere yakınlaştırır,
    ///   sağ tık + sürükle kaydırır, orta tık görünümü sıfırlar.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MapCameraController : MonoBehaviour
    {
        [SerializeField] float maxZoomIn = 6f;
        [SerializeField] float zoomStep = 1.25f;
        [SerializeField] float interactiveSmoothing = 9f;
        [SerializeField] float attractSmoothing = 2.5f;

        Camera cam;
        Rect mapBounds;
        Rect focusBounds;
        Rect viewport = new Rect(0f, 0f, 1f, 1f);
        bool attract = true;

        float fitSize;
        Vector2 fitPosition;
        float targetSize;
        Vector2 targetPos;
        Vector2 currentPos;
        float lastAspect;
        bool isDefaultView = true;
        float attractTime;

        float shakeTime;
        float shakeDuration;
        float shakeAmplitude;

        /// <summary>Kullanıcı girişi (zoom/pan) açık mı? Duraklatma gibi durumlarda kapatılır.</summary>
        public bool InputEnabled { get; set; } = true;

        public void Setup(Rect bounds)
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            mapBounds = bounds;
            SetAttractMode();
            currentPos = targetPos;
            cam.orthographicSize = targetSize;
            ApplyTransform(Vector2.zero);
        }

        /// <summary>Menü arka planı: haritanın tamamı ekranda, yavaşça süzülür.</summary>
        public void SetAttractMode()
        {
            attract = true;
            focusBounds = mapBounds;
            viewport = new Rect(0.04f, 0.06f, 0.92f, 0.86f);
            ComputeFit();
            targetSize = fitSize;
            targetPos = fitPosition;
        }

        /// <summary>
        /// Oyun: verilen harita bölgesini ekranın <paramref name="screenViewport"/> (0–1 normalleştirilmiş)
        /// alanına sığdırır. HUD için boşluk bırakmak üzere kullanılır.
        /// </summary>
        public void FocusOn(Rect worldRect, Rect screenViewport)
        {
            attract = false;
            focusBounds = worldRect;
            viewport = screenViewport;
            ComputeFit();
            ResetView();
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

        void ComputeFit()
        {
            lastAspect = cam.aspect;
            var b = focusBounds;
            // Küçük bölgelerde aşırı yakınlaşmayı engellemek için asgari boyut
            float minW = mapBounds.width * 0.3f, minH = mapBounds.height * 0.3f;
            if (b.width < minW) b = new Rect(b.center.x - minW / 2f, b.y, minW, b.height);
            if (b.height < minH) b = new Rect(b.x, b.center.y - minH / 2f, b.width, minH);

            float byHeight = b.height / (2f * viewport.height);
            float byWidth = b.width / (2f * cam.aspect * viewport.width);
            fitSize = Mathf.Max(byHeight, byWidth);

            Vector2 vc = viewport.center;
            fitPosition = new Vector2(
                b.center.x - (vc.x - 0.5f) * 2f * fitSize * cam.aspect,
                b.center.y - (vc.y - 0.5f) * 2f * fitSize);
        }

        void Update()
        {
            if (cam == null) return;

            if (!Mathf.Approximately(cam.aspect, lastAspect))
            {
                ComputeFit();
                if (attract || isDefaultView) { targetSize = fitSize; targetPos = fitPosition; }
            }

            float smoothing;
            if (attract)
            {
                attractTime += Time.unscaledDeltaTime;
                targetSize = fitSize * (0.92f + 0.05f * Mathf.Sin(attractTime * 0.11f));
                targetPos = fitPosition + new Vector2(
                    Mathf.Sin(attractTime * 0.07f) * mapBounds.width * 0.05f,
                    Mathf.Sin(attractTime * 0.05f + 1f) * mapBounds.height * 0.04f);
                smoothing = attractSmoothing;
            }
            else
            {
                if (InputEnabled) HandleInput();
                smoothing = interactiveSmoothing;
            }

            float k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, k);
            currentPos = Vector2.Lerp(currentPos, targetPos, k);

            Vector2 shake = Vector2.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float strength = shakeAmplitude * Mathf.Clamp01(shakeTime / shakeDuration) * (cam.orthographicSize / 4f);
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

            float scroll = mouse.scroll.ReadValue().y;
            if (!overUI && Mathf.Abs(scroll) > 0.01f)
            {
                Vector2 before = ScreenToWorld(screen, targetPos, targetSize);
                float newSize = scroll > 0 ? targetSize / zoomStep : targetSize * zoomStep;
                newSize = Mathf.Clamp(newSize, fitSize / maxZoomIn, fitSize);
                Vector2 after = ScreenToWorld(screen, targetPos, newSize);
                targetSize = newSize;
                targetPos += before - after;
                if (Mathf.Approximately(newSize, fitSize)) ResetView();
                else isDefaultView = false;
            }

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

            if (!isDefaultView)
            {
                float mx = targetSize * cam.aspect * 0.5f, my = targetSize * 0.5f;
                targetPos.x = Mathf.Clamp(targetPos.x, focusBounds.xMin - mx, focusBounds.xMax + mx);
                targetPos.y = Mathf.Clamp(targetPos.y, focusBounds.yMin - my, focusBounds.yMax + my);
            }
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
