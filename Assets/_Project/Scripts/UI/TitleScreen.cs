using System;
using KPSSGeography.Core;
using KPSSGeography.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Giriş ekranı: arkada süzülen harita, oyun logosu, birincil eylemler (OYNA, EĞİTİM),
    /// ikincil eylemler (Nasıl Oynanır, Ayarlar), toplam yıldız ve sürüm bilgisi.
    /// </summary>
    public class TitleScreen : UIScreen
    {

        public event Action PlayRequested;
        public event Action StudyRequested;
        public event Action ProgressReset;

        ProgressStore progress;
        SfxPlayer sfx;

        RectTransform logo;
        CanvasGroup logoGroup;
        RectTransform playRect;
        CanvasGroup playGroup;
        RectTransform secondary;
        CanvasGroup secondaryGroup;
        Text starText;

        Modal howTo;
        Modal settings;
        Button soundButton;
        Button unlockButton;
        Button resetButton;
        Button colorblindButton;
        Button bigTextButton;
        Button effectsButton;
        bool resetArmed;

        public void Initialize(Transform parent, ProgressStore progressStore, SfxPlayer sfxPlayer)
        {
            progress = progressStore;
            sfx = sfxPlayer;
            CreateRoot(parent, "TitleScreen");
            Build();
            howTo = BuildHowTo(parent);
            settings = BuildSettings(parent);
        }

        void Build()
        {
            // Arka plan katmanları: genel karartma + alttan gelen koyu gradyan
            UIFactory.Solid("Dim", Root, Theme.Hex(0x071827, 0.35f), true).rectTransform.Stretch();
            var grad = UIFactory.Icon("Gradient", Root, ShapeSprites.GradientUp, Theme.Hex(0x071827, 0.95f));
            grad.preserveAspect = false;
            grad.rectTransform.anchorMin = Vector2.zero;
            grad.rectTransform.anchorMax = new Vector2(1f, 0.85f);
            grad.rectTransform.offsetMin = Vector2.zero;
            grad.rectTransform.offsetMax = Vector2.zero;

            // --- Logo ---
            logo = UIFactory.Rect("Logo", Root).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1200, 340));
            logoGroup = logo.gameObject.AddComponent<CanvasGroup>();

            var badge = UIFactory.Panel("Badge", logo, Theme.Amber);
            badge.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(170, 56));
            UIFactory.Label("BadgeText", badge.transform, "KPSS", 34, Theme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Stretch();

            var title = UIFactory.Label("Title", logo, "COĞRAFYA", 168, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1200, 190));
            var tShadow = title.gameObject.AddComponent<Shadow>();
            tShadow.effectColor = Theme.Hex(0x000000, 0.45f);
            tShadow.effectDistance = new Vector2(0f, -8f);
            var tOutline = title.gameObject.AddComponent<Outline>();
            tOutline.effectColor = Theme.Hex(0x0B2239, 0.6f);
            tOutline.effectDistance = new Vector2(3f, -3f);

            UIFactory.Panel("Accent", logo, Theme.Amber)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -262), new Vector2(120, 8));
            UIFactory.Label("Subtitle", logo, "Türkiye haritasıyla öğren, oynayarak hatırla", 34, Theme.Sky, TextAnchor.MiddleCenter, FontStyle.Bold, true)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -288), new Vector2(1200, 46));

            // --- Birincil eylemler: OYNA ve yanında EĞİTİM ---
            playRect = UIFactory.Rect("Primary", Root).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(900, 124));
            playGroup = playRect.gameObject.AddComponent<CanvasGroup>();
            var play = UIFactory.Button("Play", playRect, "OYNA", Theme.Amber, new Vector2(430, 124), OnPlay, Theme.Ink, 58);
            play.GetComponent<RectTransform>().Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(430, 124));
            play.gameObject.AddComponent<PulseScale>();
            var study = UIFactory.Button("Study", playRect, "EĞİTİM", Theme.Teal, new Vector2(430, 124), () =>
            {
                sfx.Play(Sfx.Click);
                StudyRequested?.Invoke();
            }, Color.white, 52);
            study.GetComponent<RectTransform>().Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(430, 124));

            // --- İkincil eylemler ---
            secondary = UIFactory.Rect("Secondary", Root).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(660, 84));
            secondaryGroup = secondary.gameObject.AddComponent<CanvasGroup>();
            UIFactory.Button("HowTo", secondary, "NASIL OYNANIR", Theme.NavyPanel, new Vector2(320, 84), () =>
                {
                    sfx.Play(Sfx.Click);
                    howTo.Show();
                }, null, 28)
                .GetComponent<RectTransform>().Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(320, 84));
            UIFactory.Button("Settings", secondary, "AYARLAR", Theme.NavyPanel, new Vector2(320, 84), () =>
                {
                    sfx.Play(Sfx.Click);
                    RefreshSettings();
                    settings.Show();
                }, null, 28)
                .GetComponent<RectTransform>().Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(320, 84));

            // --- Alt bilgi ---
            var starPill = UIFactory.Card("Stars", Root, Theme.NavyPanel);
            starPill.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32, 32), new Vector2(250, 76));
            UIFactory.Icon("Star", starPill.transform, ShapeSprites.Star, Theme.StarOn).rectTransform
                .Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20, 0), new Vector2(46, 46));
            starText = UIFactory.Label("Text", starPill.transform, "", 30, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            starText.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78, 0), new Vector2(160, 46));

            UIFactory.Label("Version", Root, AppInfo.ShortLabel, 22, Theme.TextFaint, TextAnchor.LowerRight)
                .rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32, 32), new Vector2(500, 30));
        }

        Modal BuildHowTo(Transform parent)
        {
            var m = Modal.Create(parent, "HowTo", "NASIL OYNANIR?", new Vector2(980, 700));
            string text =
                "<b><color=#8ECAE6>İLİ BUL</color></b>\n" +
                "Üstte yazan ili haritada bul ve tıkla. Hızlı cevap daha çok puan getirir.\n\n" +
                "<b><color=#FFB703>BÖLGE MODU</color></b>\n" +
                "Haritada parlayan ilin hangi coğrafi bölgede olduğunu alttaki butonlardan seç.\n\n" +
                "<b>KURALLAR</b>\n" +
                "•  Her sorunun bir süresi var. Doğru cevap verince süre yenilenir.\n" +
                "•  Yanlış cevap ya da süre bitimi bir can götürür. 3 canın biterse elenirsin.\n" +
                "•  Art arda doğru cevaplar puan çarpanını artırır.\n" +
                "•  Bölümü tamamla: hatasız 3, bir hatayla 2, iki hatayla 1 yıldız.\n\n" +
                "<b>KONTROLLER</b>   Sol tık: seç  •  Tekerlek: yakınlaştır  •  Sağ tık + sürükle: kaydır";
            var label = UIFactory.Label("Text", m.Content, text, 26, Theme.Text, TextAnchor.UpperLeft);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.lineSpacing = 1.2f;
            label.rectTransform.Stretch(0, 0, 0, 110);

            UIFactory.Button("Ok", m.Content, "ANLADIM", Theme.Amber, new Vector2(320, 84), () =>
                {
                    sfx.Play(Sfx.Click);
                    m.Hide();
                }, Theme.Ink, 32)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(320, 84));
            return m;
        }

        Modal BuildSettings(Transform parent)
        {
            var m = Modal.Create(parent, "Settings", "AYARLAR", new Vector2(680, 900));
            soundButton = UIFactory.Button("Sound", m.Content, "", Theme.NavyLight, new Vector2(540, 84), () =>
            {
                sfx.Muted = !sfx.Muted;
                sfx.Play(Sfx.Click);
                RefreshSettings();
            }, null, 30);
            soundButton.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(540, 84));

            unlockButton = UIFactory.Button("Unlock", m.Content, "", Theme.NavyLight, new Vector2(540, 84), () =>
            {
                progress.UnlockAll = !progress.UnlockAll;
                sfx.Play(Sfx.Click);
                RefreshSettings();
                RefreshStars();
            }, null, 26);
            unlockButton.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -114), new Vector2(540, 84));

            resetButton = UIFactory.Button("Reset", m.Content, "", Theme.NavyLight, new Vector2(540, 84), () =>
            {
                sfx.Play(Sfx.Click);
                if (!resetArmed)
                {
                    resetArmed = true;
                    RefreshSettings();
                    return;
                }
                progress.ResetAll();
                resetArmed = false;
                RefreshSettings();
                RefreshStars();
                ProgressReset?.Invoke();
            }, null, 26);
            resetButton.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -218), new Vector2(540, 84));

            colorblindButton = SettingButton(m.Content, "Colorblind", -322, () => GameSettings.Colorblind = !GameSettings.Colorblind);
            bigTextButton = SettingButton(m.Content, "BigText", -426, () => GameSettings.BigText = !GameSettings.BigText);
            effectsButton = SettingButton(m.Content, "Effects", -530, () => GameSettings.Effects = !GameSettings.Effects);

            var about = UIFactory.Label("About", m.Content, AppInfo.FullLabel, 19, Theme.TextFaint, TextAnchor.MiddleCenter);
            about.horizontalOverflow = HorizontalWrapMode.Wrap;
            about.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 88), new Vector2(560, 26));

            UIFactory.Button("Close", m.Content, "KAPAT", Theme.Amber, new Vector2(320, 84), () =>
                {
                    sfx.Play(Sfx.Click);
                    m.Hide();
                }, Theme.Ink, 32)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(320, 84));
            return m;
        }

        Button SettingButton(Transform parent, string name, float y, System.Action toggle)
        {
            var b = UIFactory.Button(name, parent, "", Theme.NavyLight, new Vector2(540, 84), () =>
            {
                toggle();
                sfx.Play(Sfx.Click);
                RefreshSettings();
            }, null, 26);
            b.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(540, 84));
            return b;
        }

        void RefreshSettings()
        {
            UIFactory.SetButtonLabel(soundButton, sfx.Muted ? "SES: KAPALI" : "SES: AÇIK");
            UIFactory.SetButtonLabel(unlockButton, progress.UnlockAll ? "TÜM BÖLÜMLER AÇIK (TEST)" : "TÜM BÖLÜMLERİ AÇ (TEST)");
            UIFactory.SetButtonLabel(resetButton, resetArmed ? "EMİN MİSİN? TEKRAR DOKUN" : "İLERLEMEYİ SIFIRLA");
            UIFactory.SetButtonColor(resetButton, resetArmed ? Theme.Error : Theme.NavyLight);
            UIFactory.SetButtonLabel(colorblindButton, GameSettings.Colorblind ? "RENK KÖRÜ DOSTU: AÇIK" : "RENK KÖRÜ DOSTU: KAPALI");
            UIFactory.SetButtonLabel(bigTextButton, GameSettings.BigText ? "BÜYÜK YAZI: AÇIK" : "BÜYÜK YAZI: KAPALI");
            UIFactory.SetButtonLabel(effectsButton, GameSettings.Effects ? "EFEKTLER: AÇIK" : "EFEKTLER: KAPALI");
        }

        void RefreshStars() => starText.text = $"{progress.TotalStars} / {progress.MaxStars}";

        void OnPlay()
        {
            sfx.Play(Sfx.Click);
            PlayRequested?.Invoke();
        }

        public override void Show()
        {
            base.Show();
            resetArmed = false;
            RefreshStars();
            SlideIn("logo", logo, logoGroup, 0.05f, 50f);
            SlideIn("play", playRect, playGroup, 0.25f, 40f);
            SlideIn("secondary", secondary, secondaryGroup, 0.38f, 40f);
        }

        public override void Hide()
        {
            base.Hide();
            if (howTo.IsVisible) howTo.Hide();
            if (settings.IsVisible) settings.Hide();
        }
    }
}
