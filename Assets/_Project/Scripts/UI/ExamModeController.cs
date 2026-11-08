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
    /// Modu açıp kapatırken kısa ve sade bir geçiş oynar: haritadan hafif buhar yükselir, renk solar ve
    /// üstte küçük bir bilgi kartı belirir. Sinema şeridi ya da kamera hareketi yoktur.
    /// Sınav modu yalnızca il tıklanan bölümlerde geçerlidir (<see cref="Applies"/>); diğerlerinde görünüm değişmez.
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

        const float CinematicDuration = 1.7f;
        const int PuffCount = 18;

        MapCameraController camController;
        SfxPlayer sfx;
        RectTransform root;
        Image band;
        Image wash;
        Image accent;
        Text titleText;
        Text subtitleText;
        readonly List<Puff> puffs = new List<Puff>();
        Coroutine run;

        /// <summary>Mod değiştirildiğinde görünüm şu an değişmeli mi? (seçili bölüm sınav modunu destekliyor mu)</summary>
        public System.Func<bool> Applies;

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

        void OnExamToggled()
        {
            // Sınav modunun geçerli olmadığı bölümde harita renkli kalır; geçiş oynatılmaz
            if (Applies != null && !Applies()) return;
            SetLook(GameSettings.ExamMode, true);
        }

        /// <summary>
        /// Harita görünümünü sınav (true) ya da normal (false) yapar.
        /// <paramref name="cinematic"/> = sinematik sahneyle; değilse yalnızca yumuşak geçiş.
        /// </summary>
        public void SetLook(bool exam, bool cinematic, float softDuration = 1.2f)
        {
            float target = exam ? 1f : 0f;
            if (!cinematic && !IsCinematic && Mathf.Approximately(ExamLook.Blend, target) && run == null) return;
            if (!cinematic && IsCinematic && Mathf.Approximately(target, cinematicTarget)) return; // aynı yöne giden geçişi bölme

            CancelRun();
            run = StartCoroutine(cinematic ? Cinematic(exam) : Soft(target, softDuration));
        }

        float cinematicTarget = -1f;

        void CancelRun()
        {
            if (run != null) StopCoroutine(run);
            run = null;
            ResetOverlay();
        }

        // ================== Animasyonlar ==================

        IEnumerator Soft(float target, float duration)
        {
            float from = ExamLook.Blend;
            yield return Tween.Run(duration, x =>
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
            cinematicTarget = to;
            PrepareOverlay(exam);
            sfx?.Play(Sfx.Vapor);

            float t = 0f;
            while (t < CinematicDuration)
            {
                // Harita rengi yumuşakça solar / geri gelir
                float b = Mathf.Clamp01((t - 0.1f) / 1.3f);
                b = b * b * (3f - 2f * b);
                ExamLook.Blend = Mathf.Lerp(from, to, b);

                // Üstte küçük bilgi kartı: kayarak gelir, sonra kaybolur
                float a = Mathf.Min(Mathf.Clamp01(t / 0.3f), 1f - Mathf.Clamp01((t - 1.3f) / 0.4f));
                band.color = new Color(0.03f, 0.09f, 0.15f, 0.85f * a);
                titleText.color = new Color(1f, 1f, 1f, a);
                subtitleText.color = new Color(0.82f, 0.9f, 1f, a);
                accent.color = new Color(1f, 0.72f, 0.1f, a);
                float slide = 24f * (1f - Tween.EaseOutCubic(Mathf.Clamp01(t / 0.35f)));
                band.rectTransform.anchoredPosition = new Vector2(0f, -150f + slide);

                // Çok hafif sis
                float w = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.5f)) * (exam ? 0.12f : 0.06f);
                wash.color = exam ? new Color(1f, 1f, 1f, w) : new Color(0.85f, 0.93f, 1f, w);

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
            titleText.text = exam ? "SINAV MODU AÇIK" : "SINAV MODU KAPALI";
            subtitleText.text = exam
                ? "Renk yok  •  İpucu yok  •  Süre %20 kısa  •  Puan ×1,5"
                : "Renkli harita ve ipuçları geri geldi";

            Color tint = exam ? new Color(1f, 1f, 1f) : new Color(0.84f, 0.93f, 1f);
            foreach (var p in puffs)
            {
                p.delay = Random.Range(0.0f, 0.6f);
                p.life = Random.Range(0.9f, 1.1f);
                p.size = Random.Range(160f, 300f);
                p.maxAlpha = Random.Range(0.08f, 0.18f);
                // Normal modda buhar kayıp aşağıya iner; sınav modunda yükselir
                float dirY = exam ? 1f : -1f;
                p.start = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-340f, 280f));
                p.drift = new Vector2(Random.Range(-40f, 40f), dirY * Random.Range(80f, 160f));
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
            cinematicTarget = -1f;
            if (camController != null) camController.ZoomPunch = 0f;
            if (band == null) return;
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
            // Expand: kullanılabilir alan her en-boy oranında en az 1920×1080 kalır; geniş/dar pencerelerde
            // (Free Aspect, 20:9 telefon, tablet) ekranlar üst üste binmez, fazlalık kenarlarda boşluk olur.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
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

            // Üstte küçük bilgi kartı (başlık + kurallar)
            band = UIFactory.Panel("Card", root, Color.clear);
            band.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(860, 120));

            accent = UIFactory.Solid("Accent", band.transform, Color.clear);
            accent.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0, 0), new Vector2(8, 120));

            titleText = UIFactory.Label("Title", band.transform, "", 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(820, 56));

            subtitleText = UIFactory.Label("Subtitle", band.transform, "", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
            subtitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            subtitleText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(820, 36));
        }
    }
}
