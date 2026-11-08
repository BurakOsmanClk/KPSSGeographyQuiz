using System;
using System.Collections.Generic;
using System.Linq;
using KPSSGeography.Core;
using KPSSGeography.Data;
using KPSSGeography.Map;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Eğitim (serbest çalışma) ekranı. Solda konu listesi (ana başlık + alt türler), sağda harita.
    /// Bir başlık seçilince o konudaki bütün yerler adlarıyla gösterilir (ör. Dağlar); alt tür seçilince
    /// yalnızca o tür (ör. Kıvrım dağları). Haritadaki bir yere ya da adına dokununca bilgi notu açılır.
    /// Soru, süre ve puan yoktur.
    /// </summary>
    public class StudyScreen : UIScreen
    {
        const float SidebarWidth = 440f;
        const float Margin = 24f;

        /// <summary>Soldaki listede bir satır: ana başlık ya da alt tür.</summary>
        class Topic
        {
            public string title;
            public string category;   // yer şekli kategorisi (GeoFeature.*); bölgeler için null
            public string subtype;    // alt tür (ör. "Kıvrım"); null = kategorinin tamamı
            public bool regions;      // Bölgeler başlığı
            public string region;     // tek bölge
            public readonly List<Topic> children = new List<Topic>();
            public Button button;
            public bool expanded;
            public Topic parent;
        }

        public event Action BackRequested;

        TurkeyMap map;
        FeatureLayer features;
        MapCameraController cameraController;
        MapLabelLayer labels;
        MapEffects effects;
        ReliefLayer relief;
        SfxPlayer sfx;
        MapPalette palette;

        readonly List<Topic> topics = new List<Topic>();
        Topic selected;
        IMapTarget focused;
        RectTransform listContent;
        RectTransform infoCard;
        Text infoTitle;
        Text infoBody;
        Text emptyHint;

        public bool AcceptsMapInput { get; private set; }
        public bool AcceptsFeatureInput { get; private set; }

        public void Initialize(Transform parent, TurkeyMap turkeyMap, FeatureLayer featureLayer, MapCameraController camController,
            MapLabelLayer mapLabels, MapEffects mapEffects, ReliefLayer reliefLayer, SfxPlayer sfxPlayer, MapPalette mapPalette)
        {
            map = turkeyMap;
            features = featureLayer;
            cameraController = camController;
            labels = mapLabels;
            effects = mapEffects;
            relief = reliefLayer;
            sfx = sfxPlayer;
            palette = mapPalette;
            CreateRoot(parent, "StudyScreen");
            BuildTopics();
            Build();
        }

        // ================== Konu listesi ==================

        void BuildTopics()
        {
            Add("Akarsular", GeoFeature.River);
            Add("Dağlar", GeoFeature.Mountain, "Kıvrım", "Kırık (horst)");
            Add("Volkanik Dağlar", GeoFeature.Volcano);
            Add("Masif Araziler", GeoFeature.Massif);
            Add("Ovalar", GeoFeature.Plain, "Delta", "Tektonik", "Karstik (polye)", "Göl tabanı");
            Add("Platolar", GeoFeature.Plateau, "Aşınım", "Tabaka", "Karstik", "Lav örtülü");
            Add("Göller", GeoFeature.Lake, "Tektonik", "Volkanik", "Karstik", "Heyelan set", "Alüvyal set", "Kıyı set", "Baraj (yapay)");
            Add("Barajlar", GeoFeature.Dam);
            Add("Boğaz ve Körfezler", GeoFeature.Gulf, "strait", "gulf");
            Add("Yarımada, Ada, Burun", GeoFeature.Peninsula, "peninsula", "island", "cape");
            Add("Fay Hatları", GeoFeature.Fault);

            var regionsTopic = new Topic { title = "Bölgeler", regions = true };
            foreach (var r in Regions.All)
                regionsTopic.children.Add(new Topic { title = r, regions = true, region = r, parent = regionsTopic });
            topics.Add(regionsTopic);

            Add("UNESCO Mirasları", GeoFeature.Unesco);
            Add("Milli Parklar", GeoFeature.Park);
            Add("Komşu Ülkeler", GeoFeature.Country);
            Add("Sınır Kapıları", GeoFeature.Gate, "Yunanistan", "Bulgaristan", "Gürcistan", "Ermenistan", "Nahçıvan", "İran", "Irak", "Suriye");
        }

        void Add(string title, string category, params string[] subtypes)
        {
            var t = new Topic { title = title, category = category };
            foreach (var s in subtypes)
                t.children.Add(new Topic { title = SubtypeTitle(s), category = category, subtype = s, parent = t });
            topics.Add(t);
        }

        static string SubtypeTitle(string subtype) => subtype switch
        {
            "Kıvrım" => "Kıvrım Dağları",
            "Kırık (horst)" => "Kırık Dağlar (Horst)",
            "strait" => "Boğazlar",
            "gulf" => "Körfezler",
            "peninsula" => "Yarımadalar",
            "island" => "Adalar",
            "cape" => "Burunlar",
            _ => subtype
        };

        // ================== Arayüz ==================

        void Build()
        {
            // --- Sol panel ---
            var side = UIFactory.Card("Sidebar", Root, Theme.NavyPanel, true);
            var srt = side.rectTransform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(0f, 1f);
            srt.pivot = new Vector2(0f, 0.5f);
            srt.offsetMin = new Vector2(Margin, Margin);
            srt.offsetMax = new Vector2(Margin + SidebarWidth, -Margin);

            UIFactory.Button("Back", side.transform, "◀  GERİ", Theme.NavyLight, new Vector2(150, 60), () =>
                {
                    sfx.Play(Sfx.Click);
                    BackRequested?.Invoke();
                }, null, 24)
                .GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18, -18), new Vector2(150, 60));
            UIFactory.Label("Title", side.transform, "EĞİTİM", 40, Theme.Amber, TextAnchor.MiddleRight, FontStyle.Bold)
                .rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22, -18), new Vector2(240, 60));

            // Kaydırılabilir liste
            var viewport = UIFactory.Rect("Viewport", side.transform);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(12, 12);
            viewport.offsetMax = new Vector2(-12, -96);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f); // kaydırma için boş alana da dokunulabilsin

            listContent = UIFactory.Rect("Content", viewport);
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.offsetMin = Vector2.zero;
            listContent.offsetMax = Vector2.zero;
            var layout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = side.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = listContent;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            foreach (var t in topics)
            {
                t.button = Row(t, false);
                foreach (var c in t.children)
                {
                    c.button = Row(c, true);
                    c.button.gameObject.SetActive(false);
                }
            }

            // --- Bilgi kartı (haritanın sağ altında) ---
            var card = UIFactory.Card("Info", Root, Theme.NavyPanel, true);
            infoCard = card.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-Margin, Margin), new Vector2(640, 190));
            infoTitle = UIFactory.Label("Title", card.transform, "", 30, Theme.Amber, TextAnchor.MiddleLeft, FontStyle.Bold);
            infoTitle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -18), new Vector2(540, 44));
            infoBody = UIFactory.Label("Body", card.transform, "", 24, Theme.Text, TextAnchor.UpperLeft);
            infoBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoBody.lineSpacing = 1.1f;
            infoBody.rectTransform.Stretch(24, 24, 66, 16);
            UIFactory.Button("Close", card.transform, "✕", Theme.NavyLight, new Vector2(52, 52), () =>
                {
                    sfx.Play(Sfx.Click);
                    HideInfo();
                }, null, 26)
                .GetComponent<RectTransform>().Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12, -12), new Vector2(52, 52));
            infoCard.gameObject.SetActive(false);

            emptyHint = UIFactory.Label("Hint", Root, "Soldan bir konu seç: haritada bütün yerler adlarıyla görünür.\nBir yere ya da adına dokununca bilgi notu açılır.",
                30, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            emptyHint.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((SidebarWidth + Margin) / 2f, 0), new Vector2(1200, 120));
        }

        Button Row(Topic t, bool child)
        {
            string text = child ? "      " + t.title : (t.children.Count > 0 ? "▸  " : "•  ") + t.title;
            var btn = UIFactory.Button(child ? "Sub_" + t.title : "Topic_" + t.title, listContent, text,
                child ? Theme.NavyCard : Theme.NavyLight, new Vector2(SidebarWidth - 24, child ? 50 : 62), () => Select(t), null, child ? 23 : 27);
            var label = btn.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.Stretch(18, 10, 0, 0);
            var le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = child ? 50 : 62;
            // Liste kaydırılırken butonların büyümesi rahatsız etmesin
            var hover = btn.GetComponent<ButtonHoverScale>();
            if (hover != null) Destroy(hover);
            return btn;
        }

        // ================== Akış ==================

        public void Open()
        {
            Show();
            labels.SetProvinceLabels(false);
            labels.SetCountryLabels(true);
            emptyHint.gameObject.SetActive(selected == null);
            if (selected != null) Apply(selected);
            else FocusMap(map.Bounds);
        }

        public override void Hide()
        {
            if (!IsVisible) return;
            base.Hide();
            AcceptsMapInput = false;
            AcceptsFeatureInput = false;
            SetFocused(null);
            labels.ClearStudy();
            labels.SetCountryLabels(true);
            HideInfo();
        }

        void Select(Topic t)
        {
            sfx.Play(Sfx.Click);
            // Ana başlığa basınca alt türler açılır/kapanır ve konunun tamamı gösterilir
            // (tekrar basınca kapanır); alt türü olmayan başlık seçilince açık listeler kapanır
            if (t.parent == null)
            {
                bool open = t.children.Count > 0 && !(selected == t && t.expanded);
                foreach (var other in topics) SetExpanded(other, other == t && open);
            }
            selected = t;
            RefreshRows();
            emptyHint.gameObject.SetActive(false);
            Apply(t);
        }

        void SetExpanded(Topic t, bool value)
        {
            t.expanded = value;
            if (t.children.Count > 0)
                UIFactory.SetButtonLabel(t.button, (value ? "▾  " : "▸  ") + t.title);
            foreach (var c in t.children) c.button.gameObject.SetActive(value);
        }

        void RefreshRows()
        {
            foreach (var t in topics)
            {
                Paint(t.button, t == selected, false);
                foreach (var c in t.children) Paint(c.button, c == selected, true);
            }
        }

        static void Paint(Button b, bool on, bool child)
        {
            UIFactory.SetButtonColor(b, on ? Theme.Amber : child ? Theme.NavyCard : Theme.NavyLight);
            b.GetComponentInChildren<Text>().color = on ? Theme.Ink : Color.white;
        }

        // ================== Haritayı hazırlama ==================

        void Apply(Topic t)
        {
            SetFocused(null);
            HideInfo();
            labels.ClearStudy();
            effects.Clear();
            map.ResetAll();
            features.ShowDefault();
            AcceptsMapInput = false;
            AcceptsFeatureInput = false;

            if (t.regions) ApplyRegions(t.region);
            else ApplyFeatures(t);
        }

        void ApplyFeatures(Topic t)
        {
            string cat = t.category;
            bool keepProvinces = cat == GeoFeature.Unesco || cat == GeoFeature.Park || cat == GeoFeature.Country || cat == GeoFeature.Gate;
            // Oyundaki yer şekli haritalarıyla aynı görünüm: iller soluk, yer şekli bölümlerinde il sınırları gizli
            map.SetFocus(_ => false);
            map.SetBordersHidden(!keepProvinces);
            features.ShowWhere(f => f.Category == cat && (t.subtype == null || f.Data.subtype == t.subtype));
            labels.SetCountryLabels(cat != GeoFeature.Country); // ülke adları kendi etiketleriyle gelir
            relief?.SetStrength(keepProvinces ? 0.55f : 1f);
            AcceptsFeatureInput = true;

            var shown = features.Features.Where(f => f.gameObject.activeSelf)
                .OrderByDescending(f => f.Data.IsPolygon ? 1 : 0).ToList();
            labels.ShowStudy(shown.Select(f => (f.LabelWorldPosition, f.DisplayName, (Action)(() => ShowFeature(f)))));

            FocusMap(cat == GeoFeature.Country ? CountriesView() : map.Bounds);
        }

        void ApplyRegions(string region)
        {
            relief?.SetStrength(0.55f);
            labels.SetCountryLabels(true);
            AcceptsMapInput = true;

            if (region == null)
            {
                // Bütün bölgeler: bölge renkleri, dış hatları ve adları
                map.SetFocus(null);
                foreach (var r in Regions.All) effects.DrawOutline(map.GetRegionRings(r), Color.Lerp(palette.RegionColor(r), Color.black, 0.35f));
                labels.ShowStudy(Regions.All.Select(r =>
                {
                    var ps = map.Provinces.Where(p => p.Data.region == r).ToList();
                    Vector3 c = Vector3.zero;
                    foreach (var p in ps) c += p.transform.position;
                    c /= Mathf.Max(1, ps.Count);
                    return (c, r.ToUpper(new System.Globalization.CultureInfo("tr-TR")), (Action)(() => ShowRegion(r)));
                }), 24);
                FocusMap(map.Bounds);
                return;
            }

            // Tek bölge: bölge vurgulu, illeri adlarıyla
            Rect rect = map.SetFocus(p => p.Data.region == region);
            effects.DrawOutline(map.GetRegionRings(region), new Color(1f, 0.72f, 0.1f, 1f));
            var provinces = map.Provinces.Where(p => p.Data.region == region)
                .OrderByDescending(p => p.WorldRect.width * p.WorldRect.height).ToList();
            labels.ShowStudy(provinces.Select(p => (p.transform.position, p.DisplayName, (Action)(() => ShowProvince(p)))));
            float pad = Mathf.Max(rect.width, rect.height) * 0.12f;
            FocusMap(new Rect(rect.x - pad, rect.y - pad, rect.width + pad * 2f, rect.height + pad * 2f));
        }

        Rect CountriesView()
        {
            Rect b = map.Bounds;
            float padX = b.width * 0.14f, padTop = b.height * 0.18f, padBottom = b.height * 0.3f;
            return new Rect(b.x - padX, b.y - padBottom, b.width + padX * 2f, b.height + padTop + padBottom);
        }

        /// <summary>Haritayı sol panelin sağında kalan alana sığdırır (panel haritanın üstüne binmez).</summary>
        void FocusMap(Rect world)
        {
            var canvas = Root.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            float left = Mathf.Clamp((Margin * 2f + SidebarWidth) * scale / Mathf.Max(1, Screen.width), 0.1f, 0.6f);
            cameraController.FocusOn(world, new Rect(left, 0.03f, 0.985f - left, 0.94f));
        }

        // ================== Dokunma ve bilgi notu ==================

        public void OnFeatureClicked(FeatureView f)
        {
            if (!IsVisible || !AcceptsFeatureInput || f == null) return;
            ShowFeature(f);
        }

        public void OnProvinceClicked(ProvinceView p)
        {
            if (!IsVisible || !AcceptsMapInput || p == null) return;
            ShowProvince(p);
        }

        void ShowFeature(FeatureView f)
        {
            sfx.Play(Sfx.Click);
            SetFocused(f);
            string type = string.IsNullOrEmpty(f.Data.subtype) || f.Category == GeoFeature.Dam || f.Category == GeoFeature.Country
                ? "" : SubtypeTitle(f.Data.subtype);
            if (f.Category == GeoFeature.Gate) type = f.Data.subtype + " sınırı";
            ShowInfo(f.DisplayName + (type.Length > 0 ? $"  •  {type}" : ""),
                string.IsNullOrEmpty(f.Data.info) ? "Bu yer için bilgi notu yok." : f.Data.info);
        }

        void ShowProvince(ProvinceView p)
        {
            sfx.Play(Sfx.Click);
            SetFocused(p);
            var neighbors = p.Data.neighbors == null ? "" : string.Join(", ",
                p.Data.neighbors.Select(n => map.GetByPlate(n)).Where(n => n != null).Select(n => n.DisplayName));
            ShowInfo($"{p.DisplayName}  •  {p.Data.plate:00}",
                $"{p.Data.region} Bölgesi." + (neighbors.Length > 0 ? $"\nKomşu illeri: {neighbors}." : ""));
        }

        void ShowRegion(string region)
        {
            sfx.Play(Sfx.Click);
            var names = map.Provinces.Where(p => p.Data.region == region).Select(p => p.DisplayName).OrderBy(n => n, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false));
            ShowInfo($"{region} Bölgesi", $"İlleri: {string.Join(", ", names)}.");
        }

        void SetFocused(IMapTarget target)
        {
            if (focused == target) return;
            focused?.SetFocused(false);
            focused = target;
            focused?.SetFocused(true);
        }

        void ShowInfo(string title, string body)
        {
            infoTitle.text = title;
            infoBody.text = body;
            float h = Mathf.Clamp(infoBody.preferredHeight + 96f, 150f, 420f);
            infoCard.sizeDelta = new Vector2(infoCard.sizeDelta.x, h);
            infoCard.gameObject.SetActive(true);
            infoCard.SetAsLastSibling();
            PopIn("info", infoCard, 0.92f, 0.25f);
        }

        void HideInfo()
        {
            if (infoCard != null) infoCard.gameObject.SetActive(false);
            SetFocused(null);
        }
    }
}
