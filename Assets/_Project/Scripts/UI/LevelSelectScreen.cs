using System;
using System.Collections.Generic;
using KPSSGeography.Core;
using KPSSGeography.Meta;
using KPSSGeography.Quiz;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Bölüm seçimi: kartlar yatay bir karuselde dizilir. Ortadaki kart büyük ve parlak,
    /// uzaklaştıkça küçülür ve solar (yaklaşan kart hissi). Sürükle, tekerlek, oklar ya da karta tıklayarak gezilir.
    /// </summary>
    public class LevelSelectScreen : UIScreen
    {
        const float Spacing = 470f;

        public event Action BackRequested;
        public event Action<LevelDefinition> PlayRequested;

        LevelCatalog catalog;
        ProgressStore progress;
        SfxPlayer sfx;
        Canvas canvas;

        readonly List<LevelCard> allCards = new List<LevelCard>();
        /// <summary>Etkin sekmedeki kartlar (karusel bunları dizer).</summary>
        readonly List<LevelCard> cards = new List<LevelCard>();
        readonly List<Button> tabButtons = new List<Button>();
        string activeGroup;
        RectTransform track;
        RectTransform dotsRow;
        Image[] dots;
        Text starTotal;
        Button playButton;
        Button prevButton;
        Button nextButton;
        RectTransform header;
        CanvasGroup headerGroup;
        RectTransform footer;
        CanvasGroup footerGroup;

        float position;
        float target;
        float velocity;
        float dragVelocity;
        bool dragging;
        int selected = -1;

        public LevelDefinition Selected => selected >= 0 && selected < cards.Count ? cards[selected].Level : null;

        public void Initialize(Transform parent, Canvas rootCanvas, LevelCatalog levelCatalog, ProgressStore progressStore, SfxPlayer sfxPlayer)
        {
            catalog = levelCatalog;
            progress = progressStore;
            sfx = sfxPlayer;
            canvas = rootCanvas;

            CreateRoot(parent, "LevelSelect");
            Build();
        }

        void Build()
        {
            // Arka plan: harita görünsün ama kartlar öne çıksın
            var dim = UIFactory.Solid("Dim", Root, Theme.Hex(0x071827, 0.5f), true);
            dim.rectTransform.Stretch();
            var grad = UIFactory.Icon("Gradient", Root, ShapeSprites.GradientUp, Theme.Hex(0x071827, 0.85f));
            grad.preserveAspect = false;
            grad.rectTransform.anchorMin = Vector2.zero;
            grad.rectTransform.anchorMax = new Vector2(1f, 0.6f);
            grad.rectTransform.offsetMin = Vector2.zero;
            grad.rectTransform.offsetMax = Vector2.zero;

            // --- Üst bar ---
            header = UIFactory.Rect("Header", Root).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(1840, 100));
            headerGroup = header.gameObject.AddComponent<CanvasGroup>();

            var back = UIFactory.Button("Back", header, "GERİ", Theme.NavyPanel, new Vector2(200, 84), () =>
            {
                sfx.Play(Sfx.Click);
                BackRequested?.Invoke();
            }, null, 28);
            back.GetComponent<RectTransform>().Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(200, 84));
            var backText = back.GetComponentInChildren<Text>();
            backText.rectTransform.offsetMin = new Vector2(36, 0);
            UIFactory.Icon("Chevron", back.transform, ShapeSprites.ChevronLeft, Color.white).rectTransform
                .Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22, 0), new Vector2(34, 34));

            UIFactory.Label("Title", header, "BÖLÜMLER", 60, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true)
                .rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 80));

            var pill = UIFactory.Card("StarPill", header, Theme.NavyPanel);
            pill.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(230, 84));
            UIFactory.Icon("Star", pill.transform, ShapeSprites.Star, Theme.StarOn).rectTransform
                .Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22, 0), new Vector2(50, 50));
            starTotal = UIFactory.Label("Total", pill.transform, "0 / 0", 32, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            starTotal.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84, 0), new Vector2(140, 50));

            // --- Sekmeler (bölüm grupları) ---
            var tabs = UIFactory.Rect("Tabs", Root).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -148), new Vector2(1840, 60));
            int n = catalog.groups.Length;
            float tw = Mathf.Min(220f, (1840f - 10f * (n - 1)) / n);
            float ttotal = n * tw + (n - 1) * 10f;
            for (int i = 0; i < n; i++)
            {
                string gid = catalog.groups[i].id;
                var tb = UIFactory.Button("Tab_" + gid, tabs, catalog.groups[i].title, Theme.NavyPanel, new Vector2(tw, 60), () =>
                {
                    if (gid == activeGroup) return;
                    sfx.Play(Sfx.Click);
                    SetGroup(gid, null);
                }, null, n > 7 ? 19 : 22);
                tb.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-ttotal / 2f + tw / 2f + i * (tw + 10f), 0f), new Vector2(tw, 60));
                tabButtons.Add(tb);
            }

            // --- Karusel ---
            var area = UIFactory.Solid("CarouselArea", Root, new Color(0f, 0f, 0f, 0f), true);
            area.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(1920, 600));
            var input = area.gameObject.AddComponent<CarouselInput>();
            input.DragBegan += _ => { dragging = true; dragVelocity = 0f; };
            input.Dragged += OnDrag;
            input.DragEnded += OnDragEnd;
            input.Scrolled += OnScroll;
            input.Clicked += OnClick;

            track = UIFactory.Rect("Track", area.transform).Stretch();
            foreach (var level in catalog.Levels)
                allCards.Add(new LevelCard(track, level));

            prevButton = UIFactory.IconButton("Prev", Root, ShapeSprites.ChevronLeft, Theme.NavyPanel, 96, () => Step(-1));
            prevButton.GetComponent<RectTransform>().Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48, 10), new Vector2(96, 96));
            nextButton = UIFactory.IconButton("Next", Root, ShapeSprites.ChevronRight, Theme.NavyPanel, 96, () => Step(1));
            nextButton.GetComponent<RectTransform>().Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48, 10), new Vector2(96, 96));

            // --- Alt kısım: noktalar + OYNA ---
            footer = UIFactory.Rect("Footer", Root).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(900, 170));
            footerGroup = footer.gameObject.AddComponent<CanvasGroup>();

            dotsRow = UIFactory.Rect("Dots", footer).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(600, 24));
            int maxInGroup = 1;
            foreach (var g in catalog.groups) maxInGroup = Mathf.Max(maxInGroup, catalog.InGroup(g.id).Count);
            dots = new Image[maxInGroup];
            for (int i = 0; i < dots.Length; i++)
            {
                var d = UIFactory.Icon("Dot" + i, dotsRow, ShapeSprites.Circle, Theme.TextFaint);
                d.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
                dots[i] = d;
            }

            playButton = UIFactory.Button("Play", footer, "OYNA", Theme.Amber, new Vector2(440, 110), OnPlayClicked, Theme.Ink, 50);
            playButton.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(440, 110));
        }

        // ================== Gösterim ==================

        /// <summary>Ekranı açar. <paramref name="focus"/> verilirse o bölüme, yoksa oynanacak sıradaki bölüme odaklanır.</summary>
        public void Open(LevelDefinition focus = null)
        {
            RefreshCards();
            var focusLevel = focus ?? (allCards.Count > 0 ? allCards[SuggestedIndex()].Level : null);
            string group = focusLevel != null ? focusLevel.group : (catalog.groups.Length > 0 ? catalog.groups[0].id : "");
            SetGroup(group, focusLevel, false);
            velocity = 0f;
            selected = -1;
            introStart = Time.unscaledTime;
            Show();
            LayoutCards();
            UpdateSelection();

            SlideIn("header", header, headerGroup, 0.05f, -30f);
            SlideIn("footer", footer, footerGroup, 0.15f, 40f);
        }

        /// <summary>Sekmeyi değiştirir; karusel o gruptaki bölümleri gösterir.</summary>
        void SetGroup(string groupId, LevelDefinition focus, bool animate = true)
        {
            activeGroup = groupId;
            cards.Clear();
            foreach (var c in allCards)
            {
                bool inGroup = string.IsNullOrEmpty(groupId) || c.Level.group == groupId;
                if (inGroup)
                {
                    cards.Add(c);
                    c.SetNumber(cards.Count);
                }
                else c.Rect.gameObject.SetActive(false);
            }

            int index = 0;
            if (focus != null) index = Mathf.Max(0, cards.FindIndex(c => c.Level == focus));
            else
            {
                for (int i = 0; i < cards.Count; i++)
                    if (cards[i].State == LevelCardState.Unlocked && progress.GetStars(cards[i].Level.id) == 0) { index = i; break; }
            }
            position = target = index;
            velocity = 0f;
            selected = -1;
            introStart = Time.unscaledTime;

            for (int i = 0; i < tabButtons.Count; i++)
            {
                bool on = catalog.groups[i].id == groupId;
                UIFactory.SetButtonColor(tabButtons[i], on ? Theme.Amber : Theme.NavyPanel);
                tabButtons[i].GetComponentInChildren<Text>().color = on ? Theme.Ink : Theme.TextMuted;
            }
            for (int i = 0; i < dots.Length; i++)
            {
                bool on = i < cards.Count;
                dots[i].gameObject.SetActive(on);
                if (on) dots[i].rectTransform.anchoredPosition = new Vector2((i - (cards.Count - 1) / 2f) * 30f, 0f);
            }
            if (animate && IsVisible)
            {
                LayoutCards();
                UpdateSelection();
            }
        }

        int SuggestedIndex()
        {
            int last = 0;
            for (int i = 0; i < allCards.Count; i++)
            {
                var l = allCards[i].Level;
                if (!progress.IsUnlocked(l)) continue;
                last = i;
                if (progress.GetStars(l.id) == 0) return i;
            }
            return last;
        }

        void RefreshCards()
        {
            foreach (var c in allCards)
            {
                var l = c.Level;
                var state = !l.IsPlayable ? LevelCardState.ComingSoon
                    : progress.IsUnlocked(l) ? LevelCardState.Unlocked : LevelCardState.Locked;
                c.Refresh(state, progress.GetStars(l.id), progress.GetBest(l.id));
            }
            starTotal.text = $"{progress.TotalStars} / {progress.MaxStars}";
        }

        // ================== Giriş ==================

        void OnDrag(Vector2 delta)
        {
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            float d = -delta.x / scale / Spacing;
            bool outside = position < 0f || position > cards.Count - 1;
            if (outside) d *= 0.35f; // kenarlarda lastik etkisi
            position += d;
            target = position;
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            dragVelocity = Mathf.Lerp(dragVelocity, d / dt, 0.35f);
        }

        void OnDragEnd()
        {
            dragging = false;
            target = Mathf.Clamp(Mathf.Round(position + dragVelocity * 0.12f), 0, cards.Count - 1);
        }

        void OnScroll(float delta)
        {
            if (Mathf.Abs(delta) < 0.01f) return;
            Step(delta < 0 ? 1 : -1);
        }

        void Step(int dir)
        {
            target = Mathf.Clamp(Mathf.Round(target) + dir, 0, cards.Count - 1);
        }

        void OnClick(Vector2 screenPos)
        {
            // Önde olan (ortaya en yakın) karttan başlayarak tıklanan kartı bul
            int hit = -1;
            float best = float.MaxValue;
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].Rect.gameObject.activeSelf) continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(cards[i].Rect, screenPos, null)) continue;
                float d = Mathf.Abs(i - position);
                if (d < best) { best = d; hit = i; }
            }
            if (hit < 0) return;

            if (hit == selected && Mathf.Abs(position - hit) < 0.1f) OnPlayClicked();
            else target = hit;
        }

        void OnPlayClicked()
        {
            var level = Selected;
            if (level == null) return;
            var card = cards[selected];
            if (card.State != LevelCardState.Unlocked)
            {
                sfx.Play(Sfx.Wrong, 0.4f);
                PunchScale("deny", card.Rect, 0.06f, 0.25f);
                return;
            }
            sfx.Play(Sfx.Click);
            PlayRequested?.Invoke(level);
        }

        // ================== Her kare ==================

        void Update()
        {
            if (!dragging)
                position = Mathf.SmoothDamp(position, target, ref velocity, 0.16f, Mathf.Infinity, Time.unscaledDeltaTime);
            LayoutCards();
            UpdateSelection();
        }

        void LayoutCards()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                float d = i - position;
                float ad = Mathf.Abs(d);
                bool visible = ad < 3.4f;
                if (card.Rect.gameObject.activeSelf != visible) card.Rect.gameObject.SetActive(visible);
                if (!visible) continue;

                float x = Mathf.Sign(d) * Spacing * (ad <= 1f ? ad : 1f + (ad - 1f) * 0.7f);
                float y = -26f * Mathf.Min(ad, 2f);
                float scale = 1f - 0.2f * Mathf.Clamp01(ad) - 0.12f * Mathf.Clamp01(ad - 1f);
                float alpha = 1f - 0.25f * Mathf.Clamp01(ad) - 0.35f * Mathf.Clamp01(ad - 1f) - 0.4f * Mathf.Clamp01(ad - 2f);

                // Açılışta kartlar ortadan dışa doğru sırayla belirir
                float intro = Mathf.Clamp01((Time.unscaledTime - introStart - 0.05f - ad * 0.07f) / 0.45f);
                scale *= Mathf.LerpUnclamped(0.7f, 1f, Tween.EaseOutBack(intro));
                alpha *= Mathf.Clamp01(intro * 2f);

                card.Rect.anchoredPosition = new Vector2(x, y);
                card.Rect.localScale = new Vector3(scale, scale, 1f);
                card.Group.alpha = alpha;
                card.SetFocus(1f - Mathf.Clamp01(ad * 2f));
            }

            // Ortadaki kart en önde olsun
            // Ortadaki kart en önde: uzaktan yakına sırala (ekleme sıralaması, bellek ayırmaz)
            int count = Mathf.Min(cards.Count, order.Length);
            for (int k = 0; k < count; k++) order[k] = k;
            for (int a = 1; a < count; a++)
            {
                int key = order[a];
                float kd = Mathf.Abs(key - position);
                int b = a - 1;
                while (b >= 0 && Mathf.Abs(order[b] - position) < kd)
                {
                    order[b + 1] = order[b];
                    b--;
                }
                order[b + 1] = key;
            }
            for (int k = 0; k < count; k++) cards[order[k]].Rect.SetSiblingIndex(k);
        }

        readonly int[] order = new int[64];
        float introStart = -10f;

        void UpdateSelection()
        {
            if (cards.Count == 0) return;
            int idx = Mathf.Clamp(Mathf.RoundToInt(position), 0, cards.Count - 1);
            if (idx == selected) return;
            if (selected >= 0) sfx.Play(Sfx.Swipe);
            selected = idx;

            for (int i = 0; i < cards.Count && i < dots.Length; i++)
            {
                bool on = i == idx;
                dots[i].color = on ? Theme.Amber : Theme.TextFaint;
                dots[i].rectTransform.sizeDelta = on ? new Vector2(18, 18) : new Vector2(12, 12);
            }

            var card = cards[idx];
            bool can = card.State == LevelCardState.Unlocked;
            playButton.interactable = can;
            UIFactory.SetButtonLabel(playButton, card.State switch
            {
                LevelCardState.Unlocked => progress.GetStars(card.Level.id) > 0 ? "TEKRAR OYNA" : "OYNA",
                LevelCardState.Locked => "KİLİTLİ",
                _ => "YAKINDA"
            });
            playButton.GetComponentInChildren<Text>().color = can ? Theme.Ink : Theme.TextMuted;

            prevButton.interactable = idx > 0;
            nextButton.interactable = idx < cards.Count - 1;
        }
    }
}
