using System;
using System.Collections.Generic;
using KPSSGeography.Core;
using KPSSGeography.Data;
using KPSSGeography.Map;
using KPSSGeography.Quiz;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Oyun içi arayüz: hedef kartı, süre çubuğu, puan, seri, canlar, bölge butonları (Bölge Modu),
    /// geri sayım, geri bildirimler, duraklatma ve yıldızlı sonuç ekranı.
    /// </summary>
    public class QuizHud : UIScreen
    {
        public event Action LevelsRequested;
        public event Action RetryRequested;
        public event Action NextRequested;

        CityQuizGame game;
        MapPalette palette;
        Camera cam;
        SfxPlayer sfx;
        RectTransform canvasRect;

        // HUD
        CanvasGroup hudGroup;
        RectTransform targetCard;
        Text promptText;
        Text targetName;
        RectTransform timerFill;
        Image timerFillImage;
        Text timerText;
        Text scoreText;
        Text streakText;
        Text progressText;
        RectTransform heartsRow;
        Image[] hearts = Array.Empty<Image>();
        Text feedbackText;
        CanvasGroup feedbackGroup;
        Text countdownText;
        Text hintText;

        // Seçenek butonları (bölge, iklim, plaka, göl tipi, KPSS şıkları)
        const int MaxChoices = 8;
        RectTransform choiceBar;
        CanvasGroup choiceBarGroup;
        readonly Button[] choiceButtons = new Button[MaxChoices];
        readonly Text[] choiceLabels = new Text[MaxChoices];
        readonly Color[] choiceBase = new Color[MaxChoices];
        readonly Color[] choiceText = new Color[MaxChoices];
        int activeChoices;
        bool choiceBarVisible;
        Question currentQuestion;

        // İl adı etiketi
        RectTransform nameTag;
        Image nameTagImage;
        Text nameTagText;
        RectTransform examBadge;
        Image nameTagIcon;

        // Haritayı incele
        public MapLabelLayer Labels;
        RectTransform reviewBar;
        RectTransform reviewInfo;
        Text reviewInfoTitle;
        Text reviewInfoBody;
        Button reviewButton;
        bool reviewing;

        // Seri kenar parlaması
        Image glowTop;
        Image glowBottom;
        CanvasGroup nameTagGroup;
        IMapTarget nameTagTarget;

        // Bilgi notu (yer şekli soruları)
        Text infoText;
        CanvasGroup infoGroup;

        // Devam teklifi
        RectTransform continueScreen;
        CanvasGroup continueGroup;
        RectTransform continueCard;
        RectTransform continueFill;
        Button continueAdButton;
        float continueDeadline;
        bool continueWaiting;
        const float ContinueSeconds = 8f;

        /// <summary>Ödüllü reklam isteği: AppFlow reklam servisine yönlendirir.</summary>
        public Action<Action<bool>> RequestRewarded;

        // Duraklatma
        RectTransform pauseScreen;
        CanvasGroup pauseGroup;

        // Sonuç ekranı
        RectTransform endScreen;
        CanvasGroup endGroup;
        RectTransform endCard;
        Text endTitle;
        Text endLevel;
        Image[] endStars;
        Text endScore;
        Text endStats;
        Text endBest;
        Button endLevelsButton;
        Button endRetryButton;
        Button endNextButton;

        float displayedScore;
        int lastTickSecond;
        int heartsShown;

        public void Initialize(Transform parent, RectTransform rootCanvas, CityQuizGame quizGame, MapPalette mapPalette,
            Camera camera, SfxPlayer sfxPlayer)
        {
            game = quizGame;
            palette = mapPalette;
            cam = camera;
            sfx = sfxPlayer;
            canvasRect = rootCanvas;

            CreateRoot(parent, "QuizHUD");
            BuildHud();
            BuildChoiceBar();
            BuildNameTag();
            BuildPauseScreen();
            BuildContinueScreen();
            BuildEndScreen();
            BuildReview();
            BuildEdgeGlow();

            game.GameStarted += OnGameStarted;
            game.CountdownTick += OnCountdown;
            game.QuestionStarted += OnQuestion;
            game.AnswerResolved += OnAnswer;
            game.ContinueOffered += OnContinueOffered;
            game.ContinueGranted += OnContinueGranted;
            game.GameEnded += OnGameEnded;
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.GameStarted -= OnGameStarted;
            game.CountdownTick -= OnCountdown;
            game.QuestionStarted -= OnQuestion;
            game.AnswerResolved -= OnAnswer;
            game.ContinueOffered -= OnContinueOffered;
            game.ContinueGranted -= OnContinueGranted;
            game.GameEnded -= OnGameEnded;
        }

        public override void Hide()
        {
            ExitReview(false);
            Labels?.SetProvinceLabels(false);
            base.Hide();
            HideNameTag(true);
        }

        // ================== Kurulum ==================

        void BuildHud()
        {
            var hud = UIFactory.Rect("HUD", Root).Stretch();
            hudGroup = hud.gameObject.AddComponent<CanvasGroup>();

            // --- Hedef kartı (üst orta) ---
            var card = UIFactory.Card("TargetCard", hud, Theme.NavyPanel);
            targetCard = card.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(760, 160));

            promptText = UIFactory.Label("Prompt", targetCard, "", 22, Theme.Sky, TextAnchor.MiddleCenter, FontStyle.Bold);
            promptText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(720, 30));

            targetName = UIFactory.Label("TargetName", targetCard, "", 64, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            targetName.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -48), new Vector2(720, 68));

            var timerBg = UIFactory.Panel("TimerBg", targetCard, new Color(1f, 1f, 1f, 0.12f));
            timerBg.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-40, 22), new Vector2(580, 16));
            timerFillImage = UIFactory.Panel("TimerFill", timerBg.transform, Theme.Success);
            timerFill = timerFillImage.rectTransform;
            timerFill.anchorMin = Vector2.zero;
            timerFill.anchorMax = Vector2.one;
            timerFill.pivot = new Vector2(0f, 0.5f);
            timerFill.offsetMin = Vector2.zero;
            timerFill.offsetMax = Vector2.zero;

            timerText = UIFactory.Label("TimerText", targetCard, "", 26, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            timerText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(266, 12), new Vector2(90, 36));

            // --- Duraklat + puan (sol üst) ---
            var pause = UIFactory.IconButton("PauseButton", hud, ShapeSprites.Pause, Theme.NavyPanel, 88, OnPauseClicked);
            pause.GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -24), new Vector2(88, 88));

            var scorePanel = UIFactory.Card("ScorePanel", hud, Theme.NavyPanel);
            scorePanel.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(128, -24), new Vector2(270, 112));
            UIFactory.Label("ScoreTitle", scorePanel.transform, "PUAN", 20, Theme.TextMuted, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -14), new Vector2(200, 26));
            scoreText = UIFactory.Label("Score", scorePanel.transform, "0", 52, Theme.Amber, TextAnchor.MiddleLeft, FontStyle.Bold);
            scoreText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -42), new Vector2(230, 60));

            var badge = UIFactory.Panel("ExamBadge", hud, Theme.Hex(0xE5E7EB));
            examBadge = badge.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -214), new Vector2(214, 46));
            UIFactory.Label("Text", badge.transform, "SINAV MODU", 22, Theme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Stretch();
            examBadge.gameObject.SetActive(false);

            streakText = UIFactory.Label("Streak", hud, "", 30, Theme.Amber, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            streakText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(34, -166), new Vector2(460, 40));

            // --- İlerleme ve canlar (sağ üst) ---
            var rightPanel = UIFactory.Card("StatusPanel", hud, Theme.NavyPanel);
            rightPanel.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -24), new Vector2(300, 112));
            progressText = UIFactory.Label("Progress", rightPanel.transform, "", 22, Theme.TextMuted, TextAnchor.MiddleRight, FontStyle.Bold);
            progressText.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -14), new Vector2(260, 26));
            heartsRow = UIFactory.Rect("Hearts", rightPanel.transform)
                .Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24, 14), new Vector2(260, 50));

            // --- Geri bildirim ---
            feedbackText = UIFactory.Label("Feedback", hud, "", 40, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            feedbackText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -204), new Vector2(1200, 50));
            var outline = feedbackText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.5f);
            outline.effectDistance = new Vector2(2f, -2f);
            feedbackGroup = feedbackText.gameObject.AddComponent<CanvasGroup>();
            feedbackGroup.alpha = 0f;

            var infoBg = UIFactory.Card("Info", hud, Theme.NavyPanel);
            infoBg.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -258), new Vector2(1100, 84));
            infoText = UIFactory.Label("Text", infoBg.transform, "", 26, Theme.Text, TextAnchor.MiddleCenter);
            infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoText.rectTransform.Stretch(28, 28, 8, 8);
            infoGroup = infoBg.gameObject.AddComponent<CanvasGroup>();
            infoGroup.alpha = 0f;

            // --- Kontrol ipucu ---
            hintText = UIFactory.Label("Hint", hud,
                "Sol tık: il seç   •   Tekerlek: yakınlaştır   •   Sağ tık + sürükle: kaydır   •   Orta tık: sıfırla",
                20, Theme.Hex(0x0B2239, 0.7f), TextAnchor.MiddleCenter);
            hintText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(1600, 30));

            // --- Geri sayım ---
            countdownText = UIFactory.Label("Countdown", hud, "", 200, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            countdownText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(900, 260));
            var cdOutline = countdownText.gameObject.AddComponent<Outline>();
            cdOutline.effectColor = Theme.Hex(0x0B2239, 0.7f);
            cdOutline.effectDistance = new Vector2(5f, -5f);
            countdownText.gameObject.SetActive(false);
        }

        void BuildChoiceBar()
        {
            var bar = UIFactory.Rect("ChoiceBar", Root);
            choiceBar = bar.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(1840, 300));
            choiceBar.pivot = new Vector2(0.5f, 0f);
            choiceBarGroup = bar.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < MaxChoices; i++)
            {
                int idx = i;
                var btn = UIFactory.Button("Choice_" + i, bar, "", Theme.NavyPanel, new Vector2(240, 96), () => OnChoiceClicked(idx), Theme.Ink, 27);
                choiceLabels[i] = btn.GetComponentInChildren<Text>();
                choiceLabels[i].lineSpacing = 0.9f;
                choiceButtons[i] = btn;
            }
            bar.gameObject.SetActive(false);
        }

        /// <summary>Sorunun seçeneklerini yerleştirir: kısa seçenekler tek satırda, KPSS şıkları iki sütunlu ızgarada.</summary>
        void SetupChoices(Question q)
        {
            activeChoices = Mathf.Min(q.Choices.Length, MaxChoices);
            bool grid = q.LongTitle;
            for (int i = 0; i < MaxChoices; i++)
            {
                var btn = choiceButtons[i];
                bool on = i < activeChoices;
                btn.gameObject.SetActive(on);
                if (!on) continue;

                bool colored = q.ChoiceColors != null && i < q.ChoiceColors.Length;
                choiceBase[i] = colored ? q.ChoiceColors[i] : Theme.NavyPanel;
                choiceText[i] = colored ? Theme.Ink : Theme.Text;
                string label = q.Choices[i];
                if (!grid && label.Length > 10 && label.Contains(" ") && !label.Contains("\n"))
                {
                    int sp = label.IndexOf(' ');
                    label = label.Substring(0, sp) + "\n" + label.Substring(sp + 1);
                }
                choiceLabels[i].text = label;
                choiceLabels[i].alignment = grid ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                choiceLabels[i].fontSize = grid ? 26 : (activeChoices > 6 ? 24 : 27);
                choiceLabels[i].horizontalOverflow = grid ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
                choiceLabels[i].rectTransform.offsetMin = new Vector2(grid ? 26 : 6, 4);
                choiceLabels[i].rectTransform.offsetMax = new Vector2(grid ? -18 : -6, -4);

                var rt = btn.GetComponent<RectTransform>();
                if (grid)
                {
                    const float w = 880f, h = 78f, gx = 24f, gy = 14f;
                    int rows = Mathf.CeilToInt(activeChoices / 2f);
                    int row = i / 2, col = i % 2;
                    bool lastAlone = i == activeChoices - 1 && activeChoices % 2 == 1;
                    float x = lastAlone ? 0f : (col == 0 ? -(w + gx) / 2f : (w + gx) / 2f);
                    float y = (rows - 1 - row) * (h + gy) + h / 2f;
                    rt.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
                }
                else
                {
                    float spacing = 14f;
                    float w = Mathf.Min(260f, (1840f - spacing * (activeChoices - 1)) / activeChoices);
                    float total = activeChoices * w + (activeChoices - 1) * spacing;
                    rt.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                        new Vector2(-total / 2f + w / 2f + i * (w + spacing), 48f), new Vector2(w, 96f));
                }
                ResetChoice(i);
                btn.interactable = true;
            }
        }

        void ResetChoice(int i)
        {
            var b = choiceButtons[i];
            UIFactory.SetButtonColor(b, choiceBase[i]);
            var c = b.colors;
            c.disabledColor = choiceBase[i] * new Color(1f, 1f, 1f, 0.7f);
            b.colors = c;
            choiceLabels[i].color = choiceText[i];
            b.transform.localScale = Vector3.one;
        }

        void HighlightChoice(int i, Color color)
        {
            if (i < 0 || i >= activeChoices) return;
            var b = choiceButtons[i];
            var c = b.colors;
            c.disabledColor = color;
            b.colors = c;
            choiceLabels[i].color = Color.white;
            PunchScale("choice" + i, b.transform, 0.12f, 0.3f);
        }

        void SetChoicesInteractable(bool value)
        {
            for (int i = 0; i < activeChoices; i++) choiceButtons[i].interactable = value;
        }

        void ShowChoiceBar(bool show)
        {
            if (show == choiceBarVisible && choiceBar.gameObject.activeSelf == show) return;
            choiceBarVisible = show;
            choiceBar.gameObject.SetActive(show);
            if (show) SlideIn("choicebar", choiceBar, choiceBarGroup, 0.05f, 50f);
        }

        void OnChoiceClicked(int index)
        {
            if (!game.AcceptsChoiceInput) return;
            game.SubmitChoice(index);
        }

        /// <summary>Hedef kartını soru türüne göre boyutlandırır (KPSS sorusunda geniş ve çok satırlı).</summary>
        void LayoutTargetCard(bool longText)
        {
            targetCard.sizeDelta = longText ? new Vector2(1240, 236) : new Vector2(760, 160);
            targetName.fontSize = longText ? 34 : 64;
            targetName.horizontalOverflow = longText ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            targetName.lineSpacing = longText ? 1.1f : 1f;
            targetName.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, longText ? -50 : -48),
                new Vector2(longText ? 1160 : 720, longText ? 130 : 68));
        }

        void BuildNameTag()
        {
            nameTagImage = UIFactory.Panel("NameTag", Root, Theme.NavyPanel);
            nameTag = nameTagImage.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(200, 52));
            nameTagText = UIFactory.Label("Text", nameTag, "", 30, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            nameTagText.rectTransform.Stretch(40, 0, 0, 0);
            nameTagIcon = UIFactory.Icon("Icon", nameTag, ShapeSprites.Check, Color.white);
            nameTagIcon.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14, 0), new Vector2(30, 30));
            nameTagGroup = nameTag.gameObject.AddComponent<CanvasGroup>();
            nameTagGroup.blocksRaycasts = false;
            nameTagGroup.alpha = 0f;
        }

        void BuildPauseScreen()
        {
            var dim = UIFactory.Solid("PauseScreen", Root, Theme.Overlay, true);
            pauseScreen = dim.rectTransform.Stretch();
            pauseGroup = dim.gameObject.AddComponent<CanvasGroup>();

            var card = UIFactory.Card("Card", pauseScreen, Theme.NavyPanel, true);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 520));
            UIFactory.Label("Title", card.transform, "DURAKLATILDI", 52, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(520, 64));
            UIFactory.Panel("Accent", card.transform, Theme.Amber)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -118), new Vector2(90, 6));

            UIFactory.Button("Resume", card.transform, "DEVAM ET", Theme.Amber, new Vector2(400, 88), OnResumeClicked, Theme.Ink)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(400, 88));
            UIFactory.Button("Restart", card.transform, "YENİDEN BAŞLA", Theme.NavyLight, new Vector2(400, 80), () =>
                {
                    sfx.Play(Sfx.Click);
                    ClosePause();
                    RetryRequested?.Invoke();
                }, null, 30)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -282), new Vector2(400, 80));
            UIFactory.Button("Levels", card.transform, "BÖLÜMLER", Theme.NavyLight, new Vector2(400, 80), () =>
                {
                    sfx.Play(Sfx.Click);
                    ClosePause();
                    LevelsRequested?.Invoke();
                }, null, 30)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -384), new Vector2(400, 80));

            pauseScreen.gameObject.SetActive(false);
        }

        void BuildContinueScreen()
        {
            var dim = UIFactory.Solid("ContinueScreen", Root, Theme.Overlay, true);
            continueScreen = dim.rectTransform.Stretch();
            continueGroup = dim.gameObject.AddComponent<CanvasGroup>();

            var card = UIFactory.Card("Card", continueScreen, Theme.NavyPanel, true);
            continueCard = card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 600));

            var heart = UIFactory.Icon("Heart", continueCard, UIFactory.Heart, Theme.HeartOn);
            heart.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(120, 120));
            heart.gameObject.AddComponent<PulseScale>().amount = 0.08f;

            UIFactory.Label("Title", continueCard, "CANIN BİTTİ!", 60, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -176), new Vector2(680, 70));
            var body = UIFactory.Label("Body", continueCard, "Kısa bir reklam izle, <b>1 can</b> kazan ve kaldığın yerden devam et.",
                28, Theme.TextMuted, TextAnchor.MiddleCenter);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -256), new Vector2(600, 80));

            var bar = UIFactory.Panel("Bar", continueCard, new Color(1f, 1f, 1f, 0.12f));
            bar.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 222), new Vector2(560, 12));
            continueFill = UIFactory.Panel("Fill", bar.transform, Theme.Amber).rectTransform;
            continueFill.anchorMin = Vector2.zero;
            continueFill.anchorMax = Vector2.one;
            continueFill.offsetMin = Vector2.zero;
            continueFill.offsetMax = Vector2.zero;

            continueAdButton = UIFactory.Button("Watch", continueCard, "REKLAM İZLE  +1 CAN", Theme.Amber, new Vector2(520, 96), OnContinueWatch, Theme.Ink, 34);
            continueAdButton.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(520, 96));
            UIFactory.Button("Decline", continueCard, "HAYIR, BİTİR", Theme.NavyLight, new Vector2(320, 70), OnContinueDecline, null, 26)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(320, 70));

            continueScreen.gameObject.SetActive(false);
        }

        void BuildEndScreen()
        {
            var dim = UIFactory.Solid("EndScreen", Root, Theme.Overlay, true);
            endScreen = dim.rectTransform.Stretch();
            endGroup = dim.gameObject.AddComponent<CanvasGroup>();

            var card = UIFactory.Card("Card", endScreen, Theme.NavyPanel, true);
            endCard = card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 840));

            endLevel = UIFactory.Label("Level", endCard, "", 24, Theme.Sky, TextAnchor.MiddleCenter, FontStyle.Bold);
            endLevel.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(760, 30));

            endTitle = UIFactory.Label("Title", endCard, "", 72, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            endTitle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -64), new Vector2(760, 84));

            endStars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float size = i == 1 ? 150f : 116f;
                var star = UIFactory.Icon("Star" + i, endCard, ShapeSprites.Star, Theme.StarOff);
                star.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2((i - 1) * 150f, i == 1 ? -240f : -258f), new Vector2(size, size));
                endStars[i] = star;
            }

            UIFactory.Label("ScoreTitle", endCard, "PUAN", 22, Theme.TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -336), new Vector2(760, 30));
            endScore = UIFactory.Label("Score", endCard, "0", 88, Theme.Amber, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            endScore.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -366), new Vector2(760, 100));

            endStats = UIFactory.Label("Stats", endCard, "", 28, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter);
            endStats.lineSpacing = 1.3f;
            endStats.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -482), new Vector2(760, 80));

            endBest = UIFactory.Label("Best", endCard, "", 26, Theme.Amber, TextAnchor.MiddleCenter, FontStyle.Bold);
            endBest.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(760, 34));

            endLevelsButton = UIFactory.Button("Levels", endCard, "BÖLÜMLER", Theme.NavyLight, new Vector2(230, 88), () =>
            {
                sfx.Play(Sfx.Click);
                LevelsRequested?.Invoke();
            }, null, 28);
            endRetryButton = UIFactory.Button("Retry", endCard, "TEKRAR", Theme.NavyLight, new Vector2(230, 88), () =>
            {
                sfx.Play(Sfx.Click);
                RetryRequested?.Invoke();
            }, null, 28);
            endNextButton = UIFactory.Button("Next", endCard, "SONRAKİ  ›", Theme.Amber, new Vector2(230, 88), () =>
            {
                sfx.Play(Sfx.Click);
                NextRequested?.Invoke();
            }, Theme.Ink, 30);

            reviewButton = UIFactory.Button("Review", endCard, "HARİTAYI İNCELE", Theme.NavyCard, new Vector2(340, 60), () =>
            {
                sfx.Play(Sfx.Click);
                EnterReview();
            }, Theme.Sky, 24);
            reviewButton.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(340, 60));

            endScreen.gameObject.SetActive(false);
        }

        void BuildReview()
        {
            var bar = UIFactory.Card("ReviewBar", Root, Theme.NavyPanel, true);
            reviewBar = bar.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(980, 110));
            UIFactory.Label("Title", reviewBar, "HARİTAYI İNCELE", 34, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -18), new Vector2(600, 42));
            UIFactory.Label("Hint", reviewBar, "Etiketlere dokun • Tekerlekle yakınlaş • Sağ tıkla kaydır", 20, Theme.TextMuted, TextAnchor.MiddleLeft)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -64), new Vector2(640, 30));
            UIFactory.Button("Back", reviewBar, "SONUÇLARA DÖN", Theme.Amber, new Vector2(270, 70), () =>
                {
                    sfx.Play(Sfx.Click);
                    ExitReview(true);
                }, Theme.Ink, 24)
                .GetComponent<RectTransform>().Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(270, 70));
            reviewBar.gameObject.SetActive(false);

            var info = UIFactory.Card("ReviewInfo", Root, Theme.NavyPanel, true);
            reviewInfo = info.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1100, 150));
            reviewInfoTitle = UIFactory.Label("Title", reviewInfo, "", 32, Theme.Amber, TextAnchor.MiddleLeft, FontStyle.Bold);
            reviewInfoTitle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -14), new Vector2(1040, 40));
            reviewInfoBody = UIFactory.Label("Body", reviewInfo, "", 24, Theme.Text, TextAnchor.UpperLeft);
            reviewInfoBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            reviewInfoBody.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -60), new Vector2(1040, 80));
            reviewInfo.gameObject.SetActive(false);
        }

        void EnterReview()
        {
            if (reviewing || Labels == null) return;
            reviewing = true;
            endScreen.gameObject.SetActive(false);
            reviewBar.gameObject.SetActive(true);
            reviewBar.SetAsLastSibling();
            reviewInfo.gameObject.SetActive(false);
            var items = new List<(IMapTarget, string, bool, Action)>();
            foreach (var h in game.History)
            {
                var item = h;
                items.Add((h.Target, h.Label, h.Correct, () => ShowReviewInfo(item)));
            }
            Labels.ShowReview(items);
            Labels.SetProvinceLabels(!game.HidesProvinces);
            PopIn("reviewbar", reviewBar, 0.9f, 0.3f);
        }

        void ShowReviewInfo(ReviewItem item)
        {
            sfx.Play(Sfx.Click);
            reviewInfo.gameObject.SetActive(true);
            reviewInfo.SetAsLastSibling();
            reviewInfoTitle.text = (item.Correct ? "DOĞRU  •  " : "YANLIŞ  •  ") + item.Label;
            reviewInfoTitle.color = item.Correct ? Theme.Success : Theme.Error;
            reviewInfoBody.text = string.IsNullOrEmpty(item.Info) ? "Bu soru için ek bilgi notu yok." : item.Info;
            PopIn("reviewinfo", reviewInfo, 0.92f, 0.25f);
        }

        void ExitReview(bool backToResults)
        {
            if (!reviewing) return;
            reviewing = false;
            Labels?.ClearReview();
            reviewBar.gameObject.SetActive(false);
            reviewInfo.gameObject.SetActive(false);
            if (game.Level != null) Labels?.SetProvinceLabels(ProvinceNamesAllowed());
            if (backToResults)
            {
                endScreen.gameObject.SetActive(true);
                PopIn("endcard", endCard, 0.92f, 0.3f);
            }
        }

        void BuildEdgeGlow()
        {
            glowBottom = UIFactory.Icon("GlowBottom", Root, ShapeSprites.GradientUp, new Color(1f, 0.72f, 0.1f, 0f));
            glowBottom.preserveAspect = false;
            var b = glowBottom.rectTransform;
            b.anchorMin = Vector2.zero; b.anchorMax = new Vector2(1f, 0f); b.pivot = new Vector2(0.5f, 0.5f);
            b.sizeDelta = new Vector2(0f, 180f); b.anchoredPosition = new Vector2(0f, 90f);

            glowTop = UIFactory.Icon("GlowTop", Root, ShapeSprites.GradientUp, new Color(1f, 0.72f, 0.1f, 0f));
            glowTop.preserveAspect = false;
            var t = glowTop.rectTransform;
            t.anchorMin = new Vector2(0f, 1f); t.anchorMax = Vector2.one; t.pivot = new Vector2(0.5f, 0.5f);
            t.sizeDelta = new Vector2(0f, 180f); t.anchoredPosition = new Vector2(0f, -90f);
            t.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        void PulseEdgeGlow()
        {
            if (!GameSettings.Effects) return;
            Animate("glow", Tween.Run(0.9f, x =>
            {
                float a = Mathf.Sin(x * Mathf.PI) * 0.55f;
                glowTop.color = new Color(1f, 0.72f, 0.1f, a);
                glowBottom.color = new Color(1f, 0.72f, 0.1f, a);
            }));
        }

        void LayoutEndButtons(bool showNext, bool retryPrimary)
        {
            endNextButton.gameObject.SetActive(showNext);
            UIFactory.SetButtonColor(endRetryButton, retryPrimary ? Theme.Amber : Theme.NavyLight);
            endRetryButton.GetComponentInChildren<Text>().color = retryPrimary ? Theme.Ink : Theme.Text;
            UIFactory.SetButtonLabel(endRetryButton, retryPrimary ? "TEKRAR DENE" : "TEKRAR");

            Button[] order = showNext
                ? new[] { endLevelsButton, endRetryButton, endNextButton }
                : new[] { endLevelsButton, endRetryButton };
            float w = showNext ? 230f : 300f, spacing = 22f;
            float total = order.Length * w + (order.Length - 1) * spacing;
            for (int i = 0; i < order.Length; i++)
                order[i].GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-total / 2f + w / 2f + i * (w + spacing), 40f), new Vector2(w, 88));
        }

        void RebuildHearts(int count)
        {
            foreach (var h in hearts) if (h != null) Destroy(h.gameObject);
            hearts = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var h = UIFactory.Icon("Heart" + i, heartsRow, UIFactory.Heart, Theme.HeartOn);
                int fromRight = count - 1 - i;
                h.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-fromRight * 58, 0), new Vector2(50, 50));
                hearts[i] = h;
            }
            heartsShown = count;
        }

        // ================== Oyun olayları ==================

        /// <summary>İl adları cevabı ele vermiyorsa (yer şekli, konu haritası, KPSS sorusu) yakınlaşınca gösterilir.</summary>
        bool ProvinceNamesAllowed()
        {
            switch (game.Mode)
            {
                // Yer şekli bölümlerinde şehir adı yok (harita boş silüet); konu ve KPSS sorularında yakınlaşınca görünür
                case QuizMode.Topic:
                case QuizMode.Mcq:
                    return true;
                default:
                    return false;
            }
        }

        void OnGameStarted()
        {
            ExitReview(false);
            Labels?.SetProvinceLabels(ProvinceNamesAllowed());
            Show();
            endScreen.gameObject.SetActive(false);
            pauseScreen.gameObject.SetActive(false);
            continueScreen.gameObject.SetActive(false);
            HideNameTag(true);

            displayedScore = 0;
            scoreText.text = "0";
            streakText.text = "";
            infoGroup.alpha = 0f;
            currentQuestion = null;
            LayoutTargetCard(false);
            promptText.text = game.Level.ModeLabel;
            targetName.text = game.Level.title.ToUpper(TR);
            progressText.text = $"SORU 0 / {game.TotalQuestions}";
            SetTimerVisual(1f, game.TimeLimit);
            RebuildHearts(game.MaxMistakes);
            hintText.gameObject.SetActive(!game.UsesChoices && !game.IsExam);
            examBadge.gameObject.SetActive(game.IsExam);
            choiceBarVisible = false;
            choiceBar.gameObject.SetActive(false);

            Animate("hud", Tween.Run(0.3f, t => hudGroup.alpha = t));
            PopIn("card", targetCard, 0.9f);
        }

        void OnCountdown(int n)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = n > 0 ? n.ToString() : "BAŞLA!";
            countdownText.fontSize = n > 0 ? 200 : 150;
            var rt = countdownText.rectTransform;
            Animate("countdown", Tween.Run(n > 0 ? 0.55f : 0.45f, t =>
            {
                float s = Mathf.Lerp(1.6f, 1f, Tween.EaseOutBack(Mathf.Clamp01(t * 2.2f)));
                rt.localScale = new Vector3(s, s, 1f);
                countdownText.color = new Color(1f, 1f, 1f, t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f);
                if (t >= 1f && n == 0) countdownText.gameObject.SetActive(false);
            }));
        }

        void OnQuestion(QuestionInfo info)
        {
            var q = info.Question;
            currentQuestion = q;
            HideNameTag(false);
            LayoutTargetCard(q.LongTitle);
            promptText.text = q.Prompt;
            targetName.text = q.Title;
            progressText.text = q.RequireAll ? $"BULUNAN 0 / {q.Correct.Count}" : $"SORU {info.Index + 1} / {info.Total}";
            lastTickSecond = Mathf.CeilToInt(game.TimeLimit) + 1;
            PunchScale("card", targetCard, q.LongTitle ? 0.03f : 0.08f, 0.3f);

            bool choice = q.Type == AnswerType.Choice;
            if (choice) SetupChoices(q);
            ShowChoiceBar(choice);
            hintText.gameObject.SetActive(!choice && !game.IsExam);
        }

        void OnAnswer(AnswerResult r)
        {
            var q = r.Question;

            if (q.Type == AnswerType.Choice)
            {
                SetChoicesInteractable(false);
                HighlightChoice(q.CorrectChoice, Theme.Success);
                if (!r.IsCorrect && r.ChosenIndex >= 0) HighlightChoice(r.ChosenIndex, Theme.Error);
            }

            // "Hepsini bul": ara cevaplar
            if (r.Partial)
            {
                progressText.text = $"BULUNAN {r.Found} / {q.Correct.Count}";
                if (r.IsCorrect)
                {
                    SpawnFloatingText($"+{r.Points}", r.Clicked, Theme.Amber);
                    ShowNameTag(r.Clicked, Theme.Success);
                    if (r.Streak >= 3)
                    {
                        streakText.text = $"SERİ x{r.Streak}";
                        PunchScale("streak", streakText.rectTransform, 0.25f, 0.3f);
                    }
                }
                else
                {
                    ShowFeedback($"YANLIŞ! {r.Clicked.DisplayName} bu bölgede değil", Theme.Error);
                    streakText.text = "";
                    LoseHeart();
                }
                return;
            }

            // Sınavda bilgi notu cevaptan hemen sonra verilmez; "Haritayı incele" ekranında okunur
            // Haritadan seçmeli soruda yanlışta not da gösterilmez: cevabı ele verir
            bool hideAnswer = !r.IsCorrect && q.HidesAnswerOnMiss;
            if (!game.IsExam && !hideAnswer && !string.IsNullOrEmpty(r.Info)) ShowInfo(r.Info, r.IsCorrect ? 2.0f : 3.2f);
            IMapTarget tag = r.IsCorrect && r.Clicked != null ? r.Clicked : q.TagTarget;

            if (r.IsCorrect)
            {
                if (q.RequireAll)
                {
                    progressText.text = $"BULUNAN {r.Found} / {q.Correct.Count}";
                    ShowFeedback("BÖLGE TAMAMLANDI!", Theme.Success);
                    return;
                }
                ShowFeedback(r.Streak >= 3 ? "HARİKA!" : "DOĞRU!", Theme.Success);
                if (tag != null)
                {
                    SpawnFloatingText($"+{r.Points}", tag, Theme.Amber);
                    ShowNameTag(tag, Theme.Success);
                }
                if (r.Streak >= 5 && r.Streak % 5 == 0) PulseEdgeGlow();
                if (r.Streak >= 2)
                {
                    streakText.text = $"SERİ x{r.Streak}   (puan ×{r.Multiplier:0.0})";
                    PunchScale("streak", streakText.rectTransform, 0.25f, 0.3f);
                }
            }
            else
            {
                string answer = q.AnswerLabel;
                string msg;
                if (hideAnswer)
                    msg = r.Outcome == AnswerOutcome.TimedOut ? "SÜRE DOLDU!" : "YANLIŞ!";
                else if (r.Outcome == AnswerOutcome.TimedOut)
                    msg = q.RequireAll ? $"SÜRE DOLDU!  {r.Found} / {q.Correct.Count} il bulundu" : $"SÜRE DOLDU!  Doğru cevap: {answer}";
                else if (r.Clicked != null)
                    msg = $"YANLIŞ! ({r.Clicked.DisplayName})  Doğru cevap: {answer}";
                else
                    msg = $"YANLIŞ!  Doğru cevap: {answer}";
                ShowFeedback(msg, Theme.Error);
                if (tag != null && !hideAnswer) ShowNameTag(tag, Theme.Error);
                streakText.text = "";
                if (!q.RequireAll) LoseHeart();
            }
        }

        void OnGameEnded(GameResult res)
        {
            HideNameTag(false);
            continueScreen.gameObject.SetActive(false);
            continueWaiting = false;
            Animate("hud", Tween.Run(0.25f, t => hudGroup.alpha = 1f - t));

            endLevel.text = $"{res.Level.ModeLabel}  •  {res.Level.title.ToUpper(TR)}{(game.IsExam ? "  •  SINAV" : "")}";
            endTitle.text = res.Completed ? (res.Stars == 3 ? "MÜKEMMEL!" : "TEBRİKLER!") : "ELENDİN!";
            endTitle.color = res.Completed ? Theme.Text : Theme.Error;
            endScore.text = res.Score.ToString();
            int accuracy = res.Answered > 0 ? Mathf.RoundToInt(100f * res.Correct / res.Answered) : 0;
            endStats.text = $"Doğru: <b>{res.Correct} / {res.Total}</b>      İsabet: <b>%{accuracy}</b>      En uzun seri: <b>{res.BestStreak}</b>";
            endBest.text = res.IsNewBest ? "★  YENİ REKOR!  ★" : $"En yüksek puan: {res.BestScore}";

            bool nextAvailable = NextAvailable?.Invoke(res.Level) ?? false;
            LayoutEndButtons(nextAvailable, !res.Completed);

            reviewButton.gameObject.SetActive(game.History.Count > 0 && Labels != null);
            endScreen.gameObject.SetActive(true);
            endScreen.SetAsLastSibling();
            Animate("endfade", Tween.Run(0.25f, t => endGroup.alpha = t));
            PopIn("endcard", endCard, 0.85f, 0.4f);

            for (int i = 0; i < 3; i++) endStars[i].color = Theme.StarOff;
            for (int i = 0; i < res.Stars; i++)
            {
                int idx = i;
                var star = endStars[idx];
                Animate("star" + idx, Delayed(0.45f + idx * 0.28f, Tween.Run(0.4f, t =>
                {
                    if (t == 0f) sfx.Play(Sfx.Star);
                    star.color = Color.Lerp(Theme.StarOff, Theme.StarOn, Mathf.Clamp01(t * 3f));
                    float s = Mathf.LerpUnclamped(1.6f, 1f, Tween.EaseOutBack(t));
                    star.rectTransform.localScale = new Vector3(s, s, 1f);
                })));
            }
        }

        /// <summary>Sonraki bölümün açık olup olmadığını AppFlow söyler.</summary>
        public Func<LevelDefinition, bool> NextAvailable;

        // ================== Devam teklifi (ödüllü reklam) ==================

        void OnContinueOffered()
        {
            HideNameTag(false);
            continueScreen.gameObject.SetActive(true);
            continueScreen.SetAsLastSibling();
            continueAdButton.interactable = true;
            continueDeadline = Time.unscaledTime + ContinueSeconds;
            continueWaiting = true;
            Animate("continue", Tween.Run(0.25f, t => continueGroup.alpha = t));
            PopIn("continuecard", continueCard, 0.85f, 0.4f);
        }

        void OnContinueWatch()
        {
            if (!continueWaiting) return;
            continueWaiting = false;
            continueAdButton.interactable = false;
            sfx.Play(Sfx.Click);
            if (RequestRewarded == null)
            {
                game.DeclineContinue();
                return;
            }
            RequestRewarded(granted =>
            {
                continueScreen.gameObject.SetActive(false);
                if (granted) game.AcceptContinue();
                else game.DeclineContinue();
            });
        }

        void OnContinueDecline()
        {
            if (!continueWaiting) return;
            continueWaiting = false;
            sfx.Play(Sfx.Click);
            continueScreen.gameObject.SetActive(false);
            game.DeclineContinue();
        }

        void OnContinueGranted()
        {
            if (heartsShown >= hearts.Length) return;
            var h = hearts[heartsShown];
            heartsShown++;
            var rt = h.rectTransform;
            Animate("heartback", Tween.Run(0.5f, t =>
            {
                h.color = Color.Lerp(Theme.HeartOff, Theme.HeartOn, Mathf.Clamp01(t * 3f));
                float s = Mathf.LerpUnclamped(1.8f, 1f, Tween.EaseOutBack(t));
                rt.localScale = new Vector3(s, s, 1f);
            }));
            ShowFeedback("DEVAM!", Theme.Success);
        }

        // ================== Duraklatma ==================

        void OnPauseClicked()
        {
            if (!game.IsRunning || game.State == QuizState.ContinueOffer) return;
            sfx.Play(Sfx.Click);
            game.SetPaused(true);
            pauseScreen.gameObject.SetActive(true);
            pauseScreen.SetAsLastSibling();
            Animate("pause", Tween.Run(0.2f, t => pauseGroup.alpha = t));
        }

        void OnResumeClicked()
        {
            sfx.Play(Sfx.Click);
            ClosePause();
        }

        void ClosePause()
        {
            game.SetPaused(false);
            pauseScreen.gameObject.SetActive(false);
        }

        // ================== Her kare ==================

        void Update()
        {
            if (game == null) return;

            if (continueWaiting)
            {
                float left = continueDeadline - Time.unscaledTime;
                continueFill.anchorMax = new Vector2(Mathf.Clamp01(left / ContinueSeconds), 1f);
                if (left <= 0f) OnContinueDecline();
            }

            if (game.State == QuizState.Playing && !game.IsPaused)
            {
                SetTimerVisual(game.TimeRatio, game.TimeRemaining);

                int sec = Mathf.CeilToInt(game.TimeRemaining);
                if (sec < lastTickSecond)
                {
                    lastTickSecond = sec;
                    if (sec <= 3 && sec > 0)
                    {
                        sfx.Play(Sfx.Tick);
                        PunchScale("timer", timerText.rectTransform, 0.35f, 0.25f);
                    }
                }
            }

            if (Mathf.Abs(displayedScore - game.Score) > 0.5f)
            {
                displayedScore = Mathf.Lerp(displayedScore, game.Score, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
                if (Mathf.Abs(displayedScore - game.Score) < 1f) displayedScore = game.Score;
                scoreText.text = Mathf.RoundToInt(displayedScore).ToString();
            }
        }

        void LateUpdate()
        {
            if (nameTagTarget != null)
                nameTag.anchoredPosition = WorldToCanvas(nameTagTarget.LabelWorldPosition) + new Vector2(0f, 18f);
        }

        void SetTimerVisual(float ratio, float seconds)
        {
            timerFill.anchorMax = new Vector2(Mathf.Max(ratio, 0.001f), 1f);
            timerFillImage.color = ratio > 0.5f ? Theme.Success : ratio > 0.25f ? Theme.Amber : Theme.Error;
            timerText.text = seconds.ToString("0.0");
            timerText.color = ratio > 0.25f ? Theme.Text : Theme.Error;
        }

        // ================== Yardımcılar ==================

        static readonly System.Globalization.CultureInfo TR = new System.Globalization.CultureInfo("tr-TR");

        void LoseHeart()
        {
            if (heartsShown <= 0) return;
            heartsShown--;
            var h = hearts[heartsShown];
            var rt = h.rectTransform;
            Animate("heart" + heartsShown, Tween.Run(0.45f, t =>
            {
                float s = 1f + 0.6f * Tween.Punch(t);
                rt.localScale = new Vector3(s, s, 1f);
                h.color = Color.Lerp(Theme.HeartOn, Theme.HeartOff, t);
            }));
        }

        void ShowFeedback(string text, Color color)
        {
            feedbackText.text = text;
            feedbackText.color = color;
            var rt = feedbackText.rectTransform;
            Animate("feedback", Tween.Run(1.5f, t =>
            {
                float s = Mathf.Lerp(0.7f, 1f, Tween.EaseOutBack(Mathf.Clamp01(t * 5f)));
                rt.localScale = new Vector3(s, s, 1f);
                feedbackGroup.alpha = t < 0.75f ? Mathf.Clamp01(t * 8f) : 1f - (t - 0.75f) / 0.25f;
            }));
        }

        void ShowInfo(string text, float seconds)
        {
            infoText.text = text;
            Animate("info", Tween.Run(seconds, t =>
            {
                float fadeIn = Mathf.Clamp01(t * seconds / 0.2f);
                float fadeOut = Mathf.Clamp01((1f - t) * seconds / 0.3f);
                infoGroup.alpha = Mathf.Min(fadeIn, fadeOut);
            }));
        }

        void ShowNameTag(IMapTarget target, Color accent)
        {
            nameTagTarget = target;
            nameTagText.text = target.DisplayName;
            nameTagIcon.sprite = accent == Theme.Success ? ShapeSprites.Check : ShapeSprites.Cross;
            nameTag.sizeDelta = new Vector2(nameTagText.preferredWidth + 84f, 54f);
            nameTagImage.color = Color.Lerp(Theme.NavyPanel, accent, 0.55f);
            nameTag.SetAsLastSibling();
            Animate("nametag", Tween.Run(0.25f, t =>
            {
                nameTagGroup.alpha = t;
                float s = Mathf.Lerp(0.6f, 1f, Tween.EaseOutBack(t));
                nameTag.localScale = new Vector3(s, s, 1f);
            }));
        }

        void HideNameTag(bool instant)
        {
            if (instant || nameTagGroup.alpha <= 0f)
            {
                StopAnim("nametag");
                nameTagGroup.alpha = 0f;
                nameTagTarget = null;
                return;
            }
            Animate("nametag", Tween.Run(0.15f, t =>
            {
                nameTagGroup.alpha = 1f - t;
                if (t >= 1f) nameTagTarget = null;
            }));
        }

        void SpawnFloatingText(string text, IMapTarget at, Color color)
        {
            var label = UIFactory.Label("Floating", Root, text, 50, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            var o = label.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.55f);
            o.effectDistance = new Vector2(2f, -2f);
            var rt = label.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 60));
            Vector2 start = WorldToCanvas(at.LabelWorldPosition) + new Vector2(0f, 96f);
            StartCoroutine(Tween.Run(0.9f, t =>
            {
                rt.anchoredPosition = start + new Vector2(0f, 70f * Tween.EaseOutCubic(t));
                float s = Mathf.Lerp(0.5f, 1f, Tween.EaseOutBack(Mathf.Clamp01(t * 4f)));
                rt.localScale = new Vector3(s, s, 1f);
                label.color = new Color(color.r, color.g, color.b, t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f);
                if (t >= 1f) Destroy(label.gameObject);
            }));
        }

        Vector2 WorldToCanvas(Vector3 world)
        {
            Vector2 screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            return local + canvasRect.rect.size * 0.5f;
        }
    }
}
