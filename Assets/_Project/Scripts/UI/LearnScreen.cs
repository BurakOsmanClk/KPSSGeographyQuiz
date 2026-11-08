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
    /// "Öğren" ekranı: süresiz, puansız konu anlatımı. Sekmeler (İklim, Yer Şekilleri, Sular, Ekonomi, Beşeri)
    /// altında dersler bulunur. Her ders haritayı kendi görünümüne getirir:
    /// • attribute: iller özelliğe göre boyanır (iklim tipleri, bitki örtüsü) + renk açıklaması.
    /// • feature: yer şekli kategorisi gösterilir (il sınırları gizli); öğeye dokununca bilgi kartı.
    /// • topicset: konu setinin başlıkları (ürün, maden...) seçilir; ilgili iller boyanır (varsa paralel/meridyen çizilir).
    /// • notes: yalnızca ders notu; harita normal görünümde.
    /// Haritada kaydırma ve yakınlaştırma serbesttir. Sınav görünümü bu ekranda kullanılmaz.
    /// </summary>
    public class LearnScreen : UIScreen
    {
        const float SidePanelWidth = 400f;
        const float NotesPanelWidth = 560f;
        static readonly Rect MapViewport = new Rect(0.22f, 0.04f, 0.48f, 0.82f);
        static readonly Color Highlight = new Color(1f, 0.72f, 0.1f, 1f);

        public event Action BackRequested;

        /// <summary>Harita yazı katmanı (il adları ve seçilen öğenin etiketi).</summary>
        public MapLabelLayer Labels { get; set; }

        TurkeyMap map;
        FeatureLayer features;
        ReliefLayer relief;
        MapEffects effects;
        MapCameraController cam;
        SfxPlayer sfx;
        TopicDatabase topics;
        LessonDatabase lessons;

        readonly List<Button> tabButtons = new List<Button>();
        readonly List<Button> lessonButtons = new List<Button>();
        readonly List<Button> chipButtons = new List<Button>();
        RectTransform lessonList;
        RectTransform chipArea;
        Text notesTitle;
        Text notesBody;
        Image infoCard;
        Text infoTitle;
        Text infoSubtitle;
        Text infoBody;

        string currentTab;
        Lesson current;
        AttributeDef attribute;
        List<TopicSet> topicList = new List<TopicSet>();
        int chipSelected = -1;
        FeatureView selectedFeature;

        public bool AcceptsProvinceInput => IsVisible && current != null && current.kind != "feature";
        public bool AcceptsFeatureInput => IsVisible && current != null && current.kind == "feature";

        public void Initialize(Transform parent, TurkeyMap turkeyMap, FeatureLayer featureLayer, ReliefLayer reliefLayer, MapEffects mapEffects,
            MapCameraController cameraController, SfxPlayer sfxPlayer, TopicDatabase topicDb, LessonDatabase lessonDb)
        {
            map = turkeyMap;
            features = featureLayer;
            relief = reliefLayer;
            effects = mapEffects;
            cam = cameraController;
            sfx = sfxPlayer;
            topics = topicDb;
            lessons = lessonDb;
            CreateRoot(parent, "LearnScreen");
            Build();
        }

        // ================== Kurulum ==================

        void Build()
        {
            // Üst bar
            var top = UIFactory.Solid("TopBar", Root, Theme.Hex(0x071827, 0.88f), true);
            top.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(4000, 120));

            UIFactory.IconButton("Back", Root, ShapeSprites.ChevronLeft, Theme.NavyPanel, 84, () =>
                {
                    sfx.Play(Sfx.Click);
                    BackRequested?.Invoke();
                })
                .GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28, -18), new Vector2(84, 84));

            var title = UIFactory.Label("Title", Root, "ÖĞREN", 48, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            title.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(132, -18), new Vector2(260, 84));

            // Sekmeler
            var tabs = UIFactory.Rect("Tabs", Root).Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420, -28), new Vector2(1460, 64));
            int n = lessons.tabs.Length;
            float tw = n > 0 ? Mathf.Min(300f, (1460f - (n - 1) * 12f) / n) : 0f;
            for (int i = 0; i < n; i++)
            {
                string id = lessons.tabs[i].id;
                var b = UIFactory.Button("Tab_" + id, tabs, lessons.tabs[i].title, Theme.NavyPanel, new Vector2(tw, 64), () =>
                {
                    sfx.Play(Sfx.Click);
                    SelectTab(id, true);
                }, null, 24);
                b.GetComponent<RectTransform>().Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * (tw + 12f), 0), new Vector2(tw, 64));
                tabButtons.Add(b);
            }

            // Sol: ders listesi
            var left = UIFactory.Card("Lessons", Root, Theme.NavyPanel, true);
            left.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -144), new Vector2(SidePanelWidth, 520));
            UIFactory.Label("Header", left.transform, "DERSLER", 22, Theme.Sky, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -14), new Vector2(340, 36));
            lessonList = UIFactory.Rect("List", left.transform).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(SidePanelWidth - 36, 450));

            // Sol alt: seçilen yerin bilgi kartı
            infoCard = UIFactory.Card("Info", Root, Theme.NavyPanel, true);
            infoCard.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24, 24), new Vector2(SidePanelWidth, 370));
            infoTitle = UIFactory.Label("Title", infoCard.transform, "", 32, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            infoTitle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -18), new Vector2(SidePanelWidth - 48, 44));
            Wrap(infoTitle, 22, 32);
            infoSubtitle = UIFactory.Label("Subtitle", infoCard.transform, "", 22, Theme.Amber, TextAnchor.UpperLeft, FontStyle.Bold);
            infoSubtitle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -66), new Vector2(SidePanelWidth - 48, 30));
            Wrap(infoSubtitle, 16, 22);
            infoBody = UIFactory.Label("Body", infoCard.transform, "", 22, Theme.TextMuted, TextAnchor.UpperLeft);
            infoBody.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -104), new Vector2(SidePanelWidth - 48, 246));
            Wrap(infoBody, 15, 22);

            // Sağ: ders notu + başlıklar / renk açıklaması
            var right = UIFactory.Card("Notes", Root, Theme.NavyPanel, true);
            right.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -144), new Vector2(NotesPanelWidth, 912));
            notesTitle = UIFactory.Label("Title", right.transform, "", 34, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            notesTitle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26, -20), new Vector2(NotesPanelWidth - 52, 46));
            UIFactory.Panel("Accent", right.transform, Theme.Amber)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26, -72), new Vector2(90, 6));
            notesBody = UIFactory.Label("Body", right.transform, "", 22, Theme.Text, TextAnchor.UpperLeft);
            notesBody.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26, -92), new Vector2(NotesPanelWidth - 52, 430));
            Wrap(notesBody, 15, 22);
            notesBody.lineSpacing = 1.08f;
            chipArea = UIFactory.Rect("Chips", right.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 22), new Vector2(NotesPanelWidth - 44, 370));
        }

        static void Wrap(Text t, int min, int max)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = min;
            t.resizeTextMaxSize = max;
        }

        // ================== Gösterim ==================

        public void Open()
        {
            Show();
            map.ResetAll();
            features.ShowDefault();
            SelectTab(currentTab ?? (lessons.tabs.Length > 0 ? lessons.tabs[0].id : ""), false);
        }

        public override void Hide()
        {
            bool wasVisible = IsVisible;
            base.Hide();
            if (!wasVisible) return;
            ClearMap();
            effects?.Clear();
            Labels?.ClearReview();
            Labels?.SetProvinceLabels(false);
            features.ShowDefault();
            relief?.SetStrength(0.8f);
            cam.SetAttractMode();
            current = null;
        }

        void SelectTab(string tab, bool selectFirst)
        {
            currentTab = tab;
            var list = lessons.InTab(tab);
            for (int i = 0; i < tabButtons.Count; i++)
            {
                bool on = lessons.tabs[i].id == tab;
                UIFactory.SetButtonColor(tabButtons[i], on ? Theme.Amber : Theme.NavyPanel);
                tabButtons[i].GetComponentInChildren<Text>().color = on ? Theme.Ink : Theme.TextMuted;
            }

            foreach (var b in lessonButtons) Destroy(b.gameObject);
            lessonButtons.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var lesson = list[i];
                var b = UIFactory.Button("Lesson_" + lesson.id, lessonList, lesson.title, Theme.NavyCard, new Vector2(SidePanelWidth - 36, 64), () =>
                {
                    sfx.Play(Sfx.Click);
                    SelectLesson(lesson);
                }, null, 26);
                b.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -i * 74f), new Vector2(SidePanelWidth - 36, 64));
                lessonButtons.Add(b);
            }
            TextScaler.Apply(lessonList);

            var keep = !selectFirst && current != null && current.tab == tab ? current : null;
            if (keep != null) SelectLesson(keep);
            else if (list.Count > 0) SelectLesson(list[0]);
        }

        void SelectLesson(Lesson lesson)
        {
            current = lesson;
            var list = lessons.InTab(currentTab);
            for (int i = 0; i < lessonButtons.Count && i < list.Count; i++)
            {
                bool on = list[i] == lesson;
                UIFactory.SetButtonColor(lessonButtons[i], on ? Theme.Teal : Theme.NavyCard);
            }

            notesTitle.text = lesson.title;
            notesBody.text = lesson.notes;
            ClearChips();
            ClearMap();
            Labels?.ClearReview();
            attribute = null;
            topicList.Clear();
            chipSelected = -1;
            selectedFeature = null;

            switch (lesson.kind)
            {
                case "attribute": SetupAttribute(lesson.reference); break;
                case "feature": SetupFeature(lesson.reference); break;
                case "topicset": SetupTopics(lesson.reference); break;
                default: SetupNotes(); break;
            }
            // Başlık/renk listesi yoksa ders notu sağ panelin tamamını kullanır
            notesBody.rectTransform.sizeDelta = new Vector2(NotesPanelWidth - 52, chipButtons.Count > 0 ? 430 : 790);
            TextScaler.Apply(Root);
        }

        void SetupNotes()
        {
            features.ShowDefault();
            relief?.SetStrength(0.55f);
            Labels?.SetProvinceLabels(true);
            cam.FocusOn(map.Bounds, MapViewport);
            ShowInfo(current.title, "Ders notu", "Bu derste harita etkileşimi yok; sağdaki notu oku. Haritayı kaydırıp yakınlaştırarak inceleyebilirsin.");
        }

        void ClearMap()
        {
            effects?.Clear();
            map.ResetAll();
            foreach (var p in map.Provinces) p.SetOverride(null);
            selectedFeature = null;
        }

        // ---------- attribute: iller özelliğe göre boyanır ----------

        void SetupAttribute(string id)
        {
            attribute = topics.Attribute(id);
            features.ShowDefault();
            relief?.SetStrength(0.55f);
            Labels?.SetProvinceLabels(true);
            cam.FocusOn(map.Bounds, MapViewport);
            if (attribute == null) return;

            var choices = attribute.choices;
            for (int i = 0; i < choices.Length; i++)
            {
                int c = i;
                AddChip(choices[i], attribute.ChoiceColor(i), i, 1, () => ToggleChip(c, ApplyAttribute));
            }
            ApplyAttribute();
            ShowInfo("Bir ile dokun", "", "Renkler illerin " + (id == "climate" ? "iklim tipini" : "doğal bitki örtüsünü") +
                " gösterir. Gri iller birden fazla özelliğin görüldüğü geçiş alanlarıdır. Alttaki renklere dokunarak tek bir türü öne çıkarabilirsin.");
        }

        void ApplyAttribute()
        {
            foreach (var p in map.Provinces)
            {
                int choice = ChoiceOf(p.Data.plate);
                bool show = choice >= 0 && (chipSelected < 0 || chipSelected == choice);
                p.SetOverride(show ? attribute.ChoiceColor(choice) : (Color?)null);
                p.SetNeutral(!show);
            }
            HighlightChips();
        }

        int ChoiceOf(int plate)
        {
            if (attribute?.groups == null) return -1;
            foreach (var g in attribute.groups)
                if (g.plates != null && Array.IndexOf(g.plates, plate) >= 0) return g.choice;
            return -1;
        }

        // ---------- topicset: başlık seç, iller boyansın ----------

        void SetupTopics(string set)
        {
            topicList = topics.InSet(set).ToList();
            features.ShowDefault();
            relief?.SetStrength(0.55f);
            Labels?.SetProvinceLabels(true);
            cam.FocusOn(map.Bounds, MapViewport);
            int cols = topicList.Count > 6 ? 2 : 1;
            for (int i = 0; i < topicList.Count; i++)
            {
                int c = i;
                AddChip(topicList[i].title, Highlight, i, cols, () => ToggleChip(c, ApplyTopic));
            }
            ApplyTopic();
            ShowInfo("Bir başlık seç", "", "Sağdaki listeden bir başlık seçtiğinde ilgili iller turuncuya boyanır ve bilgi notu burada görünür.");
        }

        void ApplyTopic()
        {
            var t = chipSelected >= 0 && chipSelected < topicList.Count ? topicList[chipSelected] : null;
            foreach (var p in map.Provinces)
            {
                bool on = t != null && t.plates != null && Array.IndexOf(t.plates, p.Data.plate) >= 0;
                p.SetOverride(on ? Highlight : (Color?)null);
                p.SetNeutral(!on);
            }
            effects?.Clear();
            if (t != null && t.HasLine)
            {
                // Paralel / meridyen çizgisi (birkaç ara noktayla, harita projeksiyonunda)
                var pts = new Vector2[16];
                for (int i = 0; i < pts.Length; i++)
                {
                    float u = i / (pts.Length - 1f);
                    Vector3 w = map.LonLatToMap(Mathf.Lerp(t.line[0], t.line[2], u), Mathf.Lerp(t.line[1], t.line[3], u));
                    pts[i] = w;
                }
                effects?.DrawOutline(new List<Vector2[]> { pts }, new Color(0.9f, 0.2f, 0.25f, 1f), true, false);
            }
            if (t != null)
            {
                var names = t.plates.Select(pl => map.GetByPlate(pl)).Where(v => v != null).Select(v => v.DisplayName);
                ShowInfo(t.title, string.Join(", ", names), t.info);
            }
            HighlightChips();
        }

        // ---------- feature: yer şekli kategorisi ----------

        void SetupFeature(string category)
        {
            bool hide = category != GeoFeature.Unesco && category != GeoFeature.Park;
            map.SetFocus(_ => false);
            map.SetBordersHidden(hide);
            features.ShowCategory(category, true);
            relief?.SetStrength(1f);
            Labels?.SetProvinceLabels(!hide);
            cam.FocusOn(map.Bounds, MapViewport);
            int count = features.Features.Count(f => f.Category == category);
            ShowInfo("Bir öğeye dokun", count + " öğe", "Haritadaki " + count + " öğeden birine dokun: adı, türü ve KPSS bilgi notu burada görünür. Yakınlaştırmak için iki parmak ya da fare tekerleğini kullan.");
        }

        // ================== Harita girişi ==================

        public void OnProvinceClicked(ProvinceView p)
        {
            if (!AcceptsProvinceInput || p == null) return;
            sfx.Play(Sfx.Click);
            p.PlayCorrect();
            if (current.kind == "attribute" && attribute != null)
            {
                int choice = ChoiceOf(p.Data.plate);
                if (choice >= 0)
                    ShowInfo(p.DisplayName, attribute.choices[choice],
                        attribute.infos != null && choice < attribute.infos.Length ? attribute.infos[choice] : "");
                else
                    ShowInfo(p.DisplayName, "Geçiş alanı", "Bu ilde birden fazla " + (attribute.id == "climate" ? "iklimin" : "bitki örtüsünün") +
                        " özellikleri görülür (ör. kıyı ile iç kesim farklıdır); bu yüzden tek bir gruba alınmadı.");
            }
            else if (current.kind == "topicset")
            {
                var hits = topicList.Where(t => t.plates != null && Array.IndexOf(t.plates, p.Data.plate) >= 0).Select(t => t.title).ToList();
                ShowInfo(p.DisplayName, p.Data.region + " Bölgesi",
                    hits.Count > 0 ? "Bu konuda öne çıktığı başlıklar:\n• " + string.Join("\n• ", hits)
                                   : "Bu ders kapsamındaki başlıkların hiçbirinde ilk sıralarda yer almıyor.");
            }
        }

        public void OnFeatureClicked(FeatureView f)
        {
            if (!AcceptsFeatureInput || f == null) return;
            sfx.Play(Sfx.Click);
            if (selectedFeature != null) selectedFeature.SetState(ProvinceState.Idle);
            selectedFeature = f;
            f.SetState(ProvinceState.Correct);
            f.PlayCorrect();
            Labels?.ShowReview(new (IMapTarget, string, bool, Action)[] { (f, f.DisplayName, true, null) });
            ShowInfo(f.DisplayName, FeatureSubtitle(f.Data), f.Data.info);
        }

        static string FeatureSubtitle(GeoFeature d)
        {
            string kind = d.category switch
            {
                GeoFeature.River => "Akarsu",
                GeoFeature.Mountain => "Dağ",
                GeoFeature.Volcano => "Volkanik dağ",
                GeoFeature.Plain => "Ova",
                GeoFeature.Plateau => "Plato",
                GeoFeature.Lake => "Göl",
                GeoFeature.Dam => "Baraj",
                GeoFeature.Gulf => d.subtype == "strait" ? "Boğaz" : "Körfez",
                GeoFeature.Peninsula => d.subtype == "island" ? "Ada" : d.subtype == "cape" ? "Burun" : "Yarımada",
                GeoFeature.Unesco => "UNESCO Dünya Mirası",
                GeoFeature.Park => "Milli park",
                GeoFeature.Fault => "Fay hattı",
                _ => ""
            };
            bool typed = d.category == GeoFeature.Plain || d.category == GeoFeature.Plateau ||
                         d.category == GeoFeature.Mountain || d.category == GeoFeature.Lake;
            return typed && !string.IsNullOrEmpty(d.subtype) ? kind + " • " + d.subtype : kind;
        }

        // ================== Yardımcılar ==================

        void ShowInfo(string title, string subtitle, string body)
        {
            infoTitle.text = title;
            infoSubtitle.text = subtitle;
            infoBody.text = body;
            infoSubtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            var rt = infoBody.rectTransform;
            rt.anchoredPosition = new Vector2(24, string.IsNullOrEmpty(subtitle) ? -70 : -104);
            PunchScale("info", infoCard.transform, 0.03f, 0.25f);
        }

        void AddChip(string label, Color swatch, int index, int columns, UnityEngine.Events.UnityAction onClick)
        {
            float w = NotesPanelWidth - 44f;
            float cw = (w - (columns - 1) * 10f) / columns;
            const float ch = 52f;
            int row = index / columns, col = index % columns;
            var b = UIFactory.Button("Chip" + index, chipArea, label, Theme.NavyCard, new Vector2(cw, ch), () =>
            {
                sfx.Play(Sfx.Click);
                onClick();
            }, null, 21);
            b.GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(col * (cw + 10f), -row * (ch + 8f)), new Vector2(cw, ch));
            var txt = b.GetComponentInChildren<Text>();
            txt.rectTransform.offsetMin = new Vector2(44, 0);
            txt.rectTransform.offsetMax = new Vector2(-10, 0);
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 14;
            txt.resizeTextMaxSize = 21;
            UIFactory.Icon("Swatch", b.transform, ShapeSprites.Circle, swatch).rectTransform
                .Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14, 0), new Vector2(22, 22));
            chipButtons.Add(b);
        }

        void ToggleChip(int index, Action apply)
        {
            chipSelected = chipSelected == index ? -1 : index;
            apply();
        }

        void HighlightChips()
        {
            for (int i = 0; i < chipButtons.Count; i++)
                UIFactory.SetButtonColor(chipButtons[i], i == chipSelected ? Theme.Teal : Theme.NavyCard);
        }

        void ClearChips()
        {
            foreach (var b in chipButtons) Destroy(b.gameObject);
            chipButtons.Clear();
        }
    }
}
