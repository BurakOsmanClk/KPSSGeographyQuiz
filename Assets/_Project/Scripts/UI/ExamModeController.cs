using System.Collections;
using System.Collections.Generic;
using KPSSGeography.Core;
using KPSSGeography.Map;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Sınav modu görünüm denetleyicisi. <see cref="ExamLook.Blend"/> değerini animasyonla 0↔1 arasında değiştirir;
    /// harita katmanları bu değeri okuyup renkli haritadan soluk gri KPSS haritasına geçer.
    /// Modu açıp kapatırken kısa bir sinematik sahne oynar: sinema şeritleri, yükselen buhar, hafif beyaz sis,
    /// kameranın yavaşça yaklaşması ve başlık kartı. Menüler arası geçişlerde yalnızca yumuşak renk geçişi yapılır.
    /// </summary>
    public class ExamModeController : MonoBehaviour
    {
        class Puff
        {
            public RectTransform rt;
            public Image image;
            public Vector2 start;
            public Vector2 drift;
            public float delay;
            public float life;
            public float size;
            public float maxAlpha;
        }

        const float CinematicDuration = 3.0f;
        const float BarHeight = 120f;
        const int PuffCount = 42;

        MapCameraController camController;
        SfxPlayer sfx;
        RectTransform root;
        Image barTop;
        Image barBottom;
        Image band;
        Image wash;
        Image accent;
        Text titleText;
        Text subtitleText;
        readonly List<Puff> puffs = new List<Puff>();
        Coroutine run;

        /// <summary>Sinematik sahne oynuyor mu?</summary>
        public bool IsCinematic { get; private set; }

        public void Initialize(MapCameraController cameraController, SfxPlayer sfxPlayer)
        {
            camController = cameraController;
            sfx = sfxPlayer;
            ExamLook.Blend = 0f;
            BuildOverlay();
            ResetOverlay();
            GameSettings.ExamChanged += OnExamToggled;
        }

        void OnDestroy() => GameSettings.ExamChanged -= OnExamToggled;

        void OnExamToggled() => SetLook(GameSettings.ExamMode, true);

        /// <summary>
        /// Harita görünümünü sınav (true) ya da normal (false) yapar.
        /// <paramref name="cinematic"/> = sinematik sahneyle; değilse yalnızca yumuşak geçiş.
        /// </summary>
        public void SetLook(bool exam, bool cinematic)
        {
            float target = exam ? 1f : 0f;
            if (!cinematic && !IsCinematic && Mathf.Approximately(ExamLook.Blend, target) && run == null) return;

            CancelRun();
            run = StartCoroutine(cinematic ? Cinematic(exam) : Soft(target));
        }

        void CancelRun()
        {
            if (run != null) StopCoroutine(run);
            run = null;
            ResetOverlay();
        }

        // ================== Animasyonlar ==================

        IEnumerator Soft(float target)
        {
            float from = ExamLook.Blend;
            yield return Tween.Run(1.4f, x =>
            {
                float s = x * x * (3f - 2f * x);
                ExamLook.Blend = Mathf.Lerp(from, target, s);
            });
            ExamLook.Blend = target;
            run = null;
        }

        IEnumerator Cinematic(bool exam)
        {
            IsCinematic = true;
            float from = ExamLook.Blend;
            float to = exam ? 1f : 0f;
            PrepareOverlay(exam);
            sfx?.Play(Sfx.Vapor);

            float t = 0f;
            while (t < CinematicDuration)
            {
                float u = t / CinematicDuration;

                // Harita rengi: kısa bir bekleyişten sonra yavaşça "buharlaşır"
                float b = Mathf.Clamp01((t - 0.35f) / 2.2f);
                b = b * b * (3f - 2f * b);
                ExamLook.Blend = Mathf.Lerp(from, to, b);

                // Sinema şeritleri
                float bar = BarHeight * Mathf.Min(Tween.EaseOutCubic(Mathf.Clamp01(t / 0.5f)), 1f - Tween.EaseOutCubic(Mathf.Clamp01((t - 2.5f) / 0.5f)));
                barTop.rectTransform.sizeDelta = new Vector2(4000f, bar);
                barBottom.rectTransform.sizeDelta = new Vector2(4000f, bar);

                // Başlık kartı
                float a = Mathf.Min(Mathf.Clamp01((t - 0.5f) / 0.5f), 1f - Mathf.Clamp01((t - 2.3f) / 0.5f));
                band.color = new Color(0.03f, 0.09f, 0.15f, 0.72f * a);
                titleText.color = new Color(1f, 1f, 1f, a);
                subtitleText.color = new Color(0.82f, 0.9f, 1f, a);
                accent.color = new Color(1f, 1f, 1f, a);
                accent.rectTransform.sizeDelta = new Vector2(560f * Tween.EaseOutCubic(Mathf.Clamp01((t - 0.6f) / 0.7f)), 6f);
                titleText.rectTransform.anchoredPosition = new Vector2(0f, 38f + 10f * (1f - a));

                // Beyaz sis: geçişin ortasında en yoğun
                float peak = exam ? 0.3f : 0.16f;
                float w = Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - 0.3f) / 2.4f)) * peak;
                wash.color = exam ? new Color(1f, 1f, 1f, w) : new Color(0.85f, 0.93f, 1f, w);

                // Kamera yavaşça yaklaşır ve geri döner
                if (camController != null) camController.ZoomPunch = 0.06f * Mathf.Sin(Mathf.PI * u);

                UpdatePuffs(t, exam);

                yield return null;
                t += Time.unscaledDeltaTime;
            }

            ExamLook.Blend = to;
            ResetOverlay();
            run = null;
        }

        void PrepareOverlay(bool exam)
        {
            titleText.text = exam ? "S I N A V    M O D U" : "N O R M A L    M O D";
            subtitleText.text = exam
                ? "Renk yok   •   İpucu yok   •   Süre %20 kısa   •   Puan ×1,5"
                : "Renkli harita ve ipuçları geri geldi";

            Color tint = exam ? new Color(1f, 1f, 1f) : new Color(0.84f, 0.93f, 1f);
            foreach (var p in puffs)
            {
                p.delay = Random.Range(0.25f, 2.0f);
                p.life = Random.Range(1.3f, 2.1f);
                p.size = Random.Range(200f, 460f);
                p.maxAlpha = Random.Range(0.22f, 0.42f);
                // Normal modda buhar kayıp aşağıya iner; sınav modunda yükselir
                float dirY = exam ? 1f : -1f;
                p.start = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-340f, 280f));
                p.drift = new Vector2(Random.Range(-90f, 90f), dirY * Random.Range(160f, 360f));
                p.image.color = new Color(tint.r, tint.g, tint.b, 0f);
                p.rt.sizeDelta = new Vector2(p.size, p.size);
                p.rt.anchoredPosition = p.start;
                p.rt.gameObject.SetActive(true);
            }
        }

        void UpdatePuffs(float t, bool exam)
        {
            foreach (var p in puffs)
            {
                float x = (t - p.delay) / p.life;
                if (x < 0f || x > 1f)
                {
                    if (p.rt.gameObject.activeSelf && x > 1f) p.rt.gameObject.SetActive(false);
                    continue;
                }
                float alpha = Mathf.Pow(Mathf.Sin(Mathf.PI * x), 1.2f) * p.maxAlpha;
                var c = p.image.color;
                c.a = alpha;
                p.image.color = c;
                p.rt.anchoredPosition = p.start + p.drift * x;
                float s = p.size * (0.7f + 0.8f * x);
                p.rt.sizeDelta = new Vector2(s, s);
            }
        }

        void ResetOverlay()
        {
            IsCinematic = false;
            if (camController != null) camController.ZoomPunch = 0f;
            if (barTop == null) return;
            barTop.rectTransform.sizeDelta = new Vector2(4000f, 0f);
            barBottom.rectTransform.sizeDelta = new Vector2(4000f, 0f);
            band.color = Color.clear;
            wash.color = Color.clear;
            accent.color = Color.clear;
            titleText.color = Color.clear;
            subtitleText.color = Color.clear;
            foreach (var p in puffs) p.rt.gameObject.SetActive(false);
        }

        // ================== Kurulum ==================

        void BuildOverlay()
        {
            var go = new GameObject("ExamOverlay", typeof(RectTransform));
            go.layer = 5;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30; // her şeyin üstünde
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)go.transform;
            transform.SetParent(root, false);

            wash = UIFactory.Solid("Wash", root, Color.clear);
            wash.rectTransform.Stretch();

            for (int i = 0; i < PuffCount; i++)
            {
                var img = UIFactory.Icon("Puff" + i, root, ShapeSprites.SoftCircle, Color.white);
                img.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 300));
                puffs.Add(new Puff { rt = img.rectTransform, image = img });
            }

            band = UIFactory.Solid("Band", root, Color.clear);
            band.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 250));

            titleText = UIFactory.Label("Title", root, "", 92, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 38), new Vector2(1800, 120));

            accent = UIFactory.Solid("Accent", root, Color.clear);
            accent.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(0, 6));

            subtitleText = UIFactory.Label("Subtitle", root, "", 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
            subtitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            subtitleText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -66), new Vector2(1800, 50));

            var bg = new Color(0.008f, 0.03f, 0.06f, 1f);
            barTop = UIFactory.Solid("BarTop", root, bg);
            barTop.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(4000, 0));
            barBottom = UIFactory.Solid("BarBottom", root, bg);
            barBottom.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(4000, 0));
        }
    }
}
