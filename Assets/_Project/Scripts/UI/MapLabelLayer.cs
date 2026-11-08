using System;
using System.Collections.Generic;
using KPSSGeography.Core;
using KPSSGeography.Map;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Haritanın üzerindeki yazılar (ayrı bir canvas, HUD'un altında):
    /// • Deniz, boğaz ve komşu ülke adları (her zaman).
    /// • İl adları: açıkken yakınlaşma düzeyine göre büyükten küçüğe belirir, çakışanlar gizlenir.
    /// • "Haritayı incele" etiketleri: turda sorulan yerler, doğru/yanlış renginde ve dokunulabilir.
    /// • Eğitim etiketleri: seçilen konudaki yerlerin adları; çakışanlar gizlenir, yakınlaşınca belirir.
    /// </summary>
    public class MapLabelLayer : MonoBehaviour
    {
        enum Kind
        {
            Sea,
            Strait,
            Country,
            Province,
            Review,
            Study
        }

        class Label
        {
            public Kind kind;
            public RectTransform rt;
            public Text text;
            public Vector3 world;
            public float baseSize;
            public float minZoom;
            public Color baseColor;
            public Vector2 size;
        }

        Camera cam;
        Func<float> referenceSize;
        RectTransform root;
        readonly List<Label> fixedLabels = new List<Label>();
        readonly List<Label> provinceLabels = new List<Label>();
        readonly List<Label> reviewLabels = new List<Label>();
        readonly List<Label> studyLabels = new List<Label>();
        readonly List<Label> markerLabels = new List<Label>();
        bool studyLabelsOn = true;
        readonly List<Rect> placed = new List<Rect>();
        bool provincesOn;
        bool countriesOn = true;
        bool reviewing;
        float lastBlend = -1f;
        Vector3 lastCamPos;
        float lastCamSize;
        bool dirty = true;

        public void Initialize(Camera camera, Func<float> fitSize, TurkeyMap map)
        {
            cam = camera;
            referenceSize = fitSize;

            var go = new GameObject("MapLabels", typeof(RectTransform));
            go.layer = 5;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            root = (RectTransform)go.transform;
            transform.SetParent(root, false);

            // Denizler (italik, harf aralıklı) ve boğazlar
            AddFixed("K A R A D E N İ Z", Kind.Sea, map, 34.6f, 42.55f, 30);
            AddFixed("A K D E N İ Z", Kind.Sea, map, 31.8f, 35.75f, 30);
            AddFixed("E G E\nD E N İ Z İ", Kind.Sea, map, 25.75f, 38.25f, 24);
            AddFixed("MARMARA DENİZİ", Kind.Sea, map, 28.15f, 40.72f, 15);
            AddFixed("İstanbul Boğazı", Kind.Strait, map, 29.25f, 41.28f, 14);
            AddFixed("Çanakkale Boğazı", Kind.Strait, map, 26.15f, 40.02f, 14);
            // Komşu ülkeler
            AddFixed("YUNANİSTAN", Kind.Country, map, 25.35f, 41.25f, 16);
            AddFixed("BULGARİSTAN", Kind.Country, map, 26.8f, 42.25f, 16);
            AddFixed("GÜRCİSTAN", Kind.Country, map, 43.0f, 41.85f, 16);
            AddFixed("ERMENİSTAN", Kind.Country, map, 44.55f, 40.55f, 15);
            AddFixed("NAHÇIVAN", Kind.Country, map, 45.25f, 39.3f, 14);
            AddFixed("İRAN", Kind.Country, map, 44.95f, 38.2f, 16);
            AddFixed("IRAK", Kind.Country, map, 43.6f, 36.85f, 16);
            AddFixed("SURİYE", Kind.Country, map, 38.6f, 36.25f, 16);
            AddFixed("KIBRIS", Kind.Country, map, 33.3f, 35.1f, 15);

            // İl adları (alanı büyük olan önce görünür)
            var sorted = new List<ProvinceView>(map.Provinces);
            sorted.Sort((a, b) => (b.WorldRect.width * b.WorldRect.height).CompareTo(a.WorldRect.width * a.WorldRect.height));
            for (int i = 0; i < sorted.Count; i++)
            {
                var l = CreateLabel(sorted[i].DisplayName, Kind.Province, 18, Theme.Hex(0x1B2A3A, 0.9f), FontStyle.Bold);
                l.world = sorted[i].transform.position;
                l.minZoom = 1.15f + 1.6f * i / sorted.Count;
                provinceLabels.Add(l);
                l.rt.gameObject.SetActive(false);
            }

            GameSettings.Changed += OnSettingsChanged;
        }

        void OnDestroy() => GameSettings.Changed -= OnSettingsChanged;

        void OnSettingsChanged()
        {
            dirty = true;
        }

        /// <summary>İl adlarını göster/gizle (öğrenme ve inceleme görünümleri).</summary>
        public void SetProvinceLabels(bool on)
        {
            provincesOn = on;
            if (!on) foreach (var l in provinceLabels) l.rt.gameObject.SetActive(false);
            dirty = true;
        }

        /// <summary>Komşu ülke adlarını göster/gizle ("Komşu ülkeyi bul" bölümünde gizli).</summary>
        public void SetCountryLabels(bool on)
        {
            countriesOn = on;
            dirty = true;
        }

        /// <summary>"Haritayı incele" etiketlerini kurar.</summary>
        public void ShowReview(IEnumerable<(IMapTarget target, string text, bool correct, Action onClick)> items)
        {
            ClearReview();
            reviewing = true;
            foreach (var (target, text, correct, onClick) in items)
            {
                if (target == null) continue;
                var bg = UIFactory.Panel("Review", root, correct ? Theme.Success : Theme.Error, true);
                var t = UIFactory.Label("Text", bg.transform, text, 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Stretch(10, 10, 2, 2);
                float w = t.preferredWidth + 28f;
                bg.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(w, 34));
                var btn = bg.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                if (onClick != null) btn.onClick.AddListener(() => onClick());
                reviewLabels.Add(new Label
                {
                    kind = Kind.Review, rt = bg.rectTransform, text = t, world = target.LabelWorldPosition,
                    baseSize = 20, size = new Vector2(w, 34)
                });
            }
            TextScaler.Apply(root);
            dirty = true;
        }

        /// <summary>Eğitim ekranı: verilen konumlara dokunulabilir isim etiketleri koyar (sıra = öncelik).</summary>
        public void ShowStudy(IEnumerable<(Vector3 world, string text, Action onClick)> items, int fontSize = 18)
        {
            ClearStudy();
            foreach (var (world, text, onClick) in items)
            {
                var bg = UIFactory.Panel("Study", root, Theme.NavyPanel, true);
                var t = UIFactory.Label("Text", bg.transform, text, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Stretch(8, 8, 2, 2);
                float w = t.preferredWidth + 22f, h = fontSize + 12f;
                bg.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(w, h));
                var btn = bg.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                if (onClick != null) btn.onClick.AddListener(() => onClick());
                studyLabels.Add(new Label
                {
                    kind = Kind.Study, rt = bg.rectTransform, text = t, world = world, baseSize = fontSize, size = new Vector2(w, h)
                });
            }
            TextScaler.Apply(root);
            dirty = true;
        }

        /// <summary>Eğitim etiketlerini gizle/göster (kendini sınama: adlar kapalıyken yerler yine dokunulabilir).</summary>
        public void SetStudyLabelsVisible(bool on)
        {
            studyLabelsOn = on;
            dirty = true;
        }

        public void ClearStudy()
        {
            foreach (var l in studyLabels) Destroy(l.rt.gameObject);
            studyLabels.Clear();
            dirty = true;
        }

        /// <summary>
        /// Numaralı harita sorusu: işaretli yerlere I, II, III… rozetleri koyar. Her zaman görünür
        /// (sınav görünümünde de), çakışma yüzünden gizlenmez.
        /// </summary>
        public void ShowMarkers(IEnumerable<(Vector3 world, string text)> items)
        {
            ClearMarkers();
            foreach (var (world, text) in items)
            {
                var bg = UIFactory.Panel("Marker", root, Theme.Ink, false);
                var t = UIFactory.Label("Text", bg.transform, text, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Stretch(4, 4, 0, 0);
                float w = Mathf.Max(44f, t.preferredWidth + 20f), h = 40f;
                bg.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));
                var outline = bg.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(2f, -2f);
                markerLabels.Add(new Label { kind = Kind.Study, rt = bg.rectTransform, text = t, world = world, baseSize = 26, size = new Vector2(w, h) });
            }
            TextScaler.Apply(root);
            dirty = true;
        }

        public void ClearMarkers()
        {
            foreach (var l in markerLabels) Destroy(l.rt.gameObject);
            markerLabels.Clear();
            dirty = true;
        }

        public void ClearReview()
        {
            foreach (var l in reviewLabels) Destroy(l.rt.gameObject);
            reviewLabels.Clear();
            reviewing = false;
            dirty = true;
        }

        void AddFixed(string text, Kind kind, TurkeyMap map, float lon, float lat, int size)
        {
            Color c = kind == Kind.Country ? Theme.Hex(0x6B7785, 0.85f)
                : kind == Kind.Strait ? Theme.Hex(0x0B4F8A, 0.8f) : Theme.Hex(0x0B4F8A, 0.55f);
            var l = CreateLabel(text, kind, size, c, kind == Kind.Country ? FontStyle.Bold : FontStyle.BoldAndItalic);
            l.world = map.LonLatToMap(lon, lat);
            fixedLabels.Add(l);
        }

        Label CreateLabel(string text, Kind kind, int size, Color color, FontStyle style)
        {
            var t = UIFactory.Label(kind + "Label", root, text, size, color, TextAnchor.MiddleCenter, style);
            if (kind == Kind.Province)
            {
                var o = t.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(1f, 1f, 1f, 0.6f);
                o.effectDistance = new Vector2(1.2f, -1.2f);
            }
            t.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 60));
            return new Label { kind = kind, rt = t.rectTransform, text = t, baseSize = size, baseColor = color };
        }

        void LateUpdate()
        {
            if (cam == null) return;
            float blend = ExamLook.Blend;
            if (!Mathf.Approximately(blend, lastBlend))
            {
                lastBlend = blend;
                dirty = true;
            }
            bool moved = (cam.transform.position - lastCamPos).sqrMagnitude > 1e-8f || !Mathf.Approximately(cam.orthographicSize, lastCamSize);
            if (!moved && !dirty) return;
            lastCamPos = cam.transform.position;
            lastCamSize = cam.orthographicSize;
            dirty = false;

            float zoom = Mathf.Max(0.1f, referenceSize() / Mathf.Max(0.01f, cam.orthographicSize));
            float ts = GameSettings.TextScale;
            Vector2 canvasSize = root.rect.size;

            foreach (var l in fixedLabels)
            {
                l.text.fontSize = Mathf.RoundToInt(l.baseSize * ts * Mathf.Clamp(Mathf.Sqrt(zoom), 0.85f, 1.8f));
                l.rt.anchoredPosition = ToCanvas(l.world, canvasSize);
                // Sınav haritası boştur: deniz ve ülke adları buharlaşıp kaybolur
                var bc = l.baseColor;
                l.text.color = new Color(bc.r, bc.g, bc.b, bc.a * (1f - blend));
                bool visible = blend < 0.99f && (countriesOn || l.kind != Kind.Country);
                if (l.rt.gameObject.activeSelf != visible) l.rt.gameObject.SetActive(visible);
            }

            placed.Clear();
            foreach (var l in markerLabels)
            {
                Vector2 p = ToCanvas(l.world, canvasSize);
                l.rt.anchoredPosition = p;
                l.rt.SetAsLastSibling();
                placed.Add(new Rect(p.x - l.size.x / 2f, p.y - l.size.y / 2f, l.size.x, l.size.y));
            }
            foreach (var l in reviewLabels)
            {
                Vector2 p = ToCanvas(l.world, canvasSize) + new Vector2(0f, 10f);
                l.rt.anchoredPosition = p;
                placed.Add(new Rect(p.x - l.size.x / 2f, p.y, l.size.x, l.size.y));
            }

            // Eğitim etiketleri: üst üste binen gizlenir (yakınlaşınca açılır)
            foreach (var l in studyLabels)
            {
                Vector2 p = ToCanvas(l.world, canvasSize) + new Vector2(0f, 6f);
                var r = new Rect(p.x - l.size.x / 2f, p.y, l.size.x, l.size.y);
                bool show = studyLabelsOn && p.x > -80 && p.x < canvasSize.x + 80 && p.y > -40 && p.y < canvasSize.y + 40;
                if (show)
                    foreach (var other in placed)
                        if (other.Overlaps(r)) { show = false; break; }
                if (show)
                {
                    placed.Add(r);
                    l.rt.anchoredPosition = p;
                }
                if (l.rt.gameObject.activeSelf != show) l.rt.gameObject.SetActive(show);
            }

            if (!provincesOn) return;
            foreach (var l in provinceLabels)
            {
                // Sınavda il adları gizlidir (inceleme görünümü hariç)
                bool show = zoom >= l.minZoom && (blend < 0.35f || reviewing);
                if (show)
                {
                    l.text.fontSize = Mathf.RoundToInt(l.baseSize * ts * Mathf.Clamp(0.85f + (zoom - 1f) * 0.12f, 0.85f, 1.3f));
                    Vector2 p = ToCanvas(l.world, canvasSize);
                    float w = l.text.preferredWidth + 6f, h = l.text.fontSize + 4f;
                    var r = new Rect(p.x - w / 2f, p.y - h / 2f, w, h);
                    bool inView = p.x > -50 && p.x < canvasSize.x + 50 && p.y > -30 && p.y < canvasSize.y + 30;
                    foreach (var other in placed)
                        if (other.Overlaps(r)) { show = false; break; }
                    show &= inView;
                    if (show)
                    {
                        placed.Add(r);
                        l.rt.anchoredPosition = p;
                    }
                }
                if (l.rt.gameObject.activeSelf != show) l.rt.gameObject.SetActive(show);
            }
        }

        Vector2 ToCanvas(Vector3 world, Vector2 canvasSize)
        {
            Vector2 screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
            return local + canvasSize * 0.5f;
        }
    }
}
