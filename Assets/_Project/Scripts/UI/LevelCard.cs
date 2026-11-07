using KPSSGeography.Quiz;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    public enum LevelCardState
    {
        Unlocked,
        Locked,
        ComingSoon
    }

    /// <summary>Bölüm seçim karuselindeki tek kart.</summary>
    public class LevelCard
    {
        public const float Width = 440f;
        public const float Height = 560f;

        public RectTransform Rect { get; }
        public CanvasGroup Group { get; }
        public LevelDefinition Level { get; }
        public LevelCardState State { get; private set; }

        readonly Image glow;
        readonly Image background;
        readonly Image band;
        readonly Text number;
        readonly Text modeLabel;
        readonly Image modePill;
        readonly Text title;
        readonly Text description;
        readonly Image[] stars = new Image[3];
        readonly Text best;
        readonly RectTransform lockOverlay;
        readonly Text lockText;
        readonly RectTransform soonRibbon;
        readonly RectTransform doneBadge;

        public LevelCard(Transform parent, LevelDefinition level)
        {
            Level = level;
            Rect = UIFactory.Rect("Card_" + level.id, parent)
                .Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, Height));
            Group = Rect.gameObject.AddComponent<CanvasGroup>();
            Group.blocksRaycasts = false;

            // Ortadaki kartın etrafındaki kehribar çerçeve
            glow = UIFactory.Panel("Glow", Rect, Theme.Amber);
            glow.rectTransform.Stretch(-8, -8, -8, -8);

            background = UIFactory.Card("Background", Rect, Theme.NavyCard);
            background.rectTransform.Stretch();

            var content = UIFactory.Rect("Content", Rect).Stretch();
            content.gameObject.AddComponent<RectMask2D>();

            // Üst renkli bant
            band = UIFactory.Panel("Band", content, Theme.ModeColor(level.Mode));
            band.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(Width - 32, 200));

            number = UIFactory.Label("Number", band.transform, (level.index + 1).ToString(), 120, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold, true);
            number.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(300, 150));

            modePill = UIFactory.Panel("ModePill", band.transform, new Color(0f, 0f, 0f, 0.22f));
            modePill.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16, -16), new Vector2(170, 38));
            modeLabel = UIFactory.Label("Mode", modePill.transform, level.ModeLabel, 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            modeLabel.rectTransform.Stretch();
            modePill.rectTransform.sizeDelta = new Vector2(modeLabel.preferredWidth + 32f, 38f);

            doneBadge = UIFactory.Icon("Done", band.transform, ShapeSprites.Circle, Theme.Success).rectTransform
                .Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14, -14), new Vector2(48, 48));
            UIFactory.Icon("Check", doneBadge, ShapeSprites.Check, Color.white).rectTransform.Stretch(10, 10, 10, 10);

            title = UIFactory.Label("Title", content, level.title, 42, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -236), new Vector2(Width - 40, 52));

            description = UIFactory.Label("Description", content, level.description, 24, Theme.TextMuted, TextAnchor.UpperCenter);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.lineSpacing = 1.15f;
            description.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -298), new Vector2(Width - 70, 90));

            for (int i = 0; i < 3; i++)
            {
                var s = UIFactory.Icon("Star" + i, content, ShapeSprites.Star, Theme.StarOff);
                s.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((i - 1) * 74f, i == 1 ? 78f : 70f),
                    new Vector2(i == 1 ? 70 : 60, i == 1 ? 70 : 60));
                stars[i] = s;
            }

            best = UIFactory.Label("Best", content, "", 22, Theme.TextFaint, TextAnchor.MiddleCenter);
            best.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(Width - 40, 30));

            // Kilit katmanı
            var lockImg = UIFactory.Panel("Lock", Rect, Theme.Hex(0x061524, 0.62f));
            lockOverlay = lockImg.rectTransform.Stretch();
            UIFactory.Icon("LockIcon", lockOverlay, ShapeSprites.Lock, new Color(1f, 1f, 1f, 0.9f)).rectTransform
                .Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(120, 120));
            lockText = UIFactory.Label("LockText", lockOverlay, "", 24, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            lockText.horizontalOverflow = HorizontalWrapMode.Wrap;
            lockText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -64), new Vector2(Width - 80, 70));

            // "YAKINDA" kurdelesi
            var ribbon = UIFactory.Solid("SoonRibbon", content, Theme.Amber);
            soonRibbon = ribbon.rectTransform.Place(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-66, -66), new Vector2(300, 46));
            soonRibbon.localRotation = Quaternion.Euler(0f, 0f, -45f);
            UIFactory.Label("Text", soonRibbon, "YAKINDA", 22, Theme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Stretch();
        }

        public void Refresh(LevelCardState state, int starCount, int bestScore)
        {
            State = state;
            bool soon = state == LevelCardState.ComingSoon;
            bool locked = state == LevelCardState.Locked;

            background.color = soon ? Theme.Soon : Theme.NavyCard;
            band.color = soon ? Theme.Locked : Theme.ModeColor(Level.Mode);
            number.color = soon ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            title.color = soon ? Theme.TextMuted : Theme.Text;

            lockOverlay.gameObject.SetActive(locked || soon);
            lockText.text = soon ? "Bu bölüm hazırlanıyor." : "Önceki bölümü tamamla.";
            lockOverlay.GetComponent<Image>().color = soon ? Theme.Hex(0x061524, 0.72f) : Theme.Hex(0x061524, 0.6f);
            soonRibbon.gameObject.SetActive(soon);
            doneBadge.gameObject.SetActive(starCount > 0);

            for (int i = 0; i < 3; i++)
            {
                stars[i].gameObject.SetActive(!soon);
                stars[i].color = i < starCount ? Theme.StarOn : Theme.StarOff;
            }
            best.text = !soon && bestScore > 0 ? $"En iyi: {bestScore}" : "";
        }

        /// <summary>Karuseldeki konuma göre (0 = ortada) çerçeve parlaklığı.</summary>
        public void SetFocus(float focus)
        {
            var c = State == LevelCardState.Unlocked ? Theme.Amber : new Color(1f, 1f, 1f, 0.5f);
            c.a *= focus;
            glow.color = c;
        }
    }
}
