using System;
using KPSSGeography.Core;
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

        // Bölge butonları
        RectTransform regionBar;
        CanvasGroup regionBarGroup;
        Button[] regionButtons;

        // İl adı etiketi
        RectTransform nameTag;
        Image nameTagImage;
        Text nameTagText;
        CanvasGroup nameTagGroup;
        ProvinceView nameTagTarget;

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
            BuildRegionBar();
            BuildNameTag();
            BuildPauseScreen();
            BuildEndScreen();

            game.GameStarted += OnGameStarted;
            game.CountdownTick += OnCountdown;
            game.QuestionStarted += OnQuestion;
            game.AnswerResolved += OnAnswer;
            game.GameEnded += OnGameEnded;
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.GameStarted -= OnGameStarted;
            game.CountdownTick -= OnCountdown;
            game.QuestionStarted -= OnQuestion;
            game.AnswerResolved -= OnAnswer;
            game.GameEnded -= OnGameEnded;
        }

        public override void Hide()
        {
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

        void BuildRegionBar()
        {
            var bar = UIFactory.Rect("RegionBar", Root);
            regionBar = bar.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(1800, 96));
            regionBarGroup = bar.gameObject.AddComponent<CanvasGroup>();

            var names = Regions.All;
            regionButtons = new Button[names.Length];
            const float w = 240f, spacing = 14f;
            float total = names.Length * w + (names.Length - 1) * spacing;
            for (int i = 0; i < names.Length; i++)
            {
                string region = names[i];
                string label = region == Regions.GuneydoguAnadolu ? "Güneydoğu\nAnadolu"
                    : region == Regions.DoguAnadolu ? "Doğu\nAnadolu"
                    : region == Regions.IcAnadolu ? "İç\nAnadolu" : region;
                var btn = UIFactory.Button("Region_" + i, bar, label, palette.RegionColor(region), new Vector2(w, 96),
                    () => OnRegionClicked(region), Theme.Ink, 27);
                btn.GetComponentInChildren<Text>().lineSpacing = 0.9f;
                btn.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-total / 2f + w / 2f + i * (w + spacing), 0f), new Vector2(w, 96));
                regionButtons[i] = btn;
            }
            bar.gameObject.SetActive(false);
        }

        void BuildNameTag()
        {
            nameTagImage = UIFactory.Panel("NameTag", Root, Theme.NavyPanel);
            nameTag = nameTagImage.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(200, 52));
            nameTagText = UIFactory.Label("Text", nameTag, "", 30, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            nameTagText.rectTransform.Stretch();
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

        void BuildEndScreen()
        {
            var dim = UIFactory.Solid("EndScreen", Root, Theme.Overlay, true);
            endScreen = dim.rectTransform.Stretch();
            endGroup = dim.gameObject.AddComponent<CanvasGroup>();

            var card = UIFactory.Card("Card", endScreen, Theme.NavyPanel, true);
            endCard = card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 760));

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

            endScreen.gameObject.SetActive(false);
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

        void OnGameStarted()
        {
            Show();
            endScreen.gameObject.SetActive(false);
            pauseScreen.gameObject.SetActive(false);
            HideNameTag(true);

            bool regionMode = game.Mode == QuizMode.Region;
            displayedScore = 0;
            scoreText.text = "0";
            streakText.text = "";
            promptText.text = regionMode ? "BU İL HANGİ BÖLGEDE?" : "BU İLİ HARİTADA BUL";
            targetName.text = game.Level.title.ToUpper(TR);
            progressText.text = $"SORU 0 / {game.TotalQuestions}";
            SetTimerVisual(1f, game.TimeLimit);
            RebuildHearts(game.MaxMistakes);
            hintText.gameObject.SetActive(!regionMode);
            regionBar.gameObject.SetActive(regionMode);
            ResetRegionButtons();
            SetRegionButtonsInteractable(false);
            if (regionMode) SlideIn("regionbar", regionBar, regionBarGroup, 0.2f, 60f);

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

        void OnQuestion(QuestionInfo q)
        {
            HideNameTag(false);
            targetName.text = q.Target.DisplayName.ToUpper(TR);
            progressText.text = $"SORU {q.Index + 1} / {q.Total}";
            lastTickSecond = Mathf.CeilToInt(game.TimeLimit) + 1;
            PunchScale("card", targetCard, 0.08f, 0.3f);

            if (q.Mode == QuizMode.Region)
            {
                ResetRegionButtons();
                SetRegionButtonsInteractable(true);
            }
        }

        void OnAnswer(AnswerResult r)
        {
            if (r.Mode == QuizMode.Region)
            {
                SetRegionButtonsInteractable(false);
                HighlightRegionButton(r.CorrectRegion, Theme.Success);
                if (!r.IsCorrect && r.ChosenRegion != null) HighlightRegionButton(r.ChosenRegion, Theme.Error);
            }

            if (r.IsCorrect)
            {
                ShowFeedback(r.Streak >= 3 ? "HARİKA!" : "DOĞRU!", Theme.Success);
                SpawnFloatingText($"+{r.Points}", r.Target, Theme.Amber);
                ShowNameTag(r.Target, Theme.Success);
                if (r.Streak >= 2)
                {
                    streakText.text = $"SERİ x{r.Streak}   (puan ×{r.Multiplier:0.0})";
                    PunchScale("streak", streakText.rectTransform, 0.25f, 0.3f);
                }
            }
            else
            {
                string answer = r.Mode == QuizMode.Region ? r.CorrectRegion : r.Target.DisplayName;
                string msg = r.Outcome == AnswerOutcome.TimedOut
                    ? $"SÜRE DOLDU!  Doğru cevap: {answer}"
                    : r.Mode == QuizMode.Region
                        ? $"YANLIŞ!  {r.Target.DisplayName} → {answer}"
                        : $"YANLIŞ! ({r.Clicked.DisplayName})  Doğru cevap: {answer}";
                ShowFeedback(msg, Theme.Error);
                ShowNameTag(r.Target, Theme.Error);
                streakText.text = "";
                LoseHeart();
            }
        }

        void OnGameEnded(GameResult res)
        {
            HideNameTag(false);
            Animate("hud", Tween.Run(0.25f, t => hudGroup.alpha = 1f - t));

            endLevel.text = $"{res.Level.index + 1}. BÖLÜM  •  {res.Level.title.ToUpper(TR)}";
            endTitle.text = res.Completed ? (res.Stars == 3 ? "MÜKEMMEL!" : "TEBRİKLER!") : "ELENDİN!";
            endTitle.color = res.Completed ? Theme.Text : Theme.Error;
            endScore.text = res.Score.ToString();
            int accuracy = res.Answered > 0 ? Mathf.RoundToInt(100f * res.Correct / res.Answered) : 0;
            endStats.text = $"Doğru: <b>{res.Correct} / {res.Total}</b>      İsabet: <b>%{accuracy}</b>      En uzun seri: <b>{res.BestStreak}</b>";
            endBest.text = res.IsNewBest ? "★  YENİ REKOR!  ★" : $"En yüksek puan: {res.BestScore}";

            bool nextAvailable = NextAvailable?.Invoke(res.Level) ?? false;
            LayoutEndButtons(nextAvailable, !res.Completed);

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

        // ================== Duraklatma ==================

        void OnPauseClicked()
        {
            if (!game.IsRunning || game.State == QuizState.Finished) return;
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

        // ================== Bölge butonları ==================

        void OnRegionClicked(string region)
        {
            if (!game.AcceptsRegionInput) return;
            game.SubmitRegion(region);
        }

        void SetRegionButtonsInteractable(bool value)
        {
            foreach (var b in regionButtons) b.interactable = value;
        }

        void ResetRegionButtons()
        {
            for (int i = 0; i < regionButtons.Length; i++)
            {
                UIFactory.SetButtonColor(regionButtons[i], palette.RegionColor(Regions.All[i]));
                var c = regionButtons[i].colors;
                c.disabledColor = palette.RegionColor(Regions.All[i]) * new Color(1f, 1f, 1f, 0.75f);
                regionButtons[i].colors = c;
                regionButtons[i].GetComponentInChildren<Text>().color = Theme.Ink;
                regionButtons[i].transform.localScale = Vector3.one;
            }
        }

        void HighlightRegionButton(string region, Color color)
        {
            int i = Array.IndexOf(Regions.All, region);
            if (i < 0) return;
            var b = regionButtons[i];
            var c = b.colors;
            c.disabledColor = color;
            b.colors = c;
            b.GetComponentInChildren<Text>().color = Color.white;
            PunchScale("regionbtn" + i, b.transform, 0.15f, 0.3f);
        }

        // ================== Her kare ==================

        void Update()
        {
            if (game == null) return;

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
                nameTag.anchoredPosition = WorldToCanvas(nameTagTarget.transform.position) + new Vector2(0f, 18f);
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

        void ShowNameTag(ProvinceView target, Color accent)
        {
            nameTagTarget = target;
            nameTagText.text = target.DisplayName;
            nameTag.sizeDelta = new Vector2(nameTagText.preferredWidth + 44f, 54f);
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

        void SpawnFloatingText(string text, ProvinceView at, Color color)
        {
            var label = UIFactory.Label("Floating", Root, text, 50, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            var o = label.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.55f);
            o.effectDistance = new Vector2(2f, -2f);
            var rt = label.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 60));
            Vector2 start = WorldToCanvas(at.transform.position) + new Vector2(0f, 96f);
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
