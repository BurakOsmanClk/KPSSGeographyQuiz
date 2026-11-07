using System.Collections;
using System.Collections.Generic;
using KPSSGeography.Core;
using KPSSGeography.Map;
using KPSSGeography.Quiz;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Oyun arayüzü: hedef il kartı, süre çubuğu, puan, seri, canlar, geri bildirimler,
    /// başlangıç ve oyun sonu ekranları. Tamamen kodla kurulur (prefab gerekmez).
    /// </summary>
    public class QuizHud : MonoBehaviour
    {
        // Renkler
        static readonly Color CardColor = new Color(0.11f, 0.15f, 0.22f, 0.94f);
        static readonly Color TextMuted = new Color(1f, 1f, 1f, 0.65f);
        static readonly Color Green = new Color32(0x4C, 0xC0, 0x63, 0xFF);
        static readonly Color Yellow = new Color32(0xF2, 0xC1, 0x4E, 0xFF);
        static readonly Color Red = new Color32(0xE5, 0x53, 0x4B, 0xFF);
        static readonly Color Orange = new Color32(0xFF, 0xA9, 0x3A, 0xFF);
        static readonly Color HeartOn = new Color32(0xF0, 0x4E, 0x5E, 0xFF);
        static readonly Color HeartOff = new Color(1f, 1f, 1f, 0.18f);
        static readonly Color ButtonGreen = new Color32(0x3E, 0xA8, 0x5A, 0xFF);

        CityQuizGame game;
        QuizConfig config;
        Camera cam;
        SfxPlayer sfx;

        RectTransform canvasRect;
        readonly Dictionary<string, Coroutine> running = new Dictionary<string, Coroutine>();

        // HUD
        CanvasGroup hudGroup;
        RectTransform targetCard;
        Text targetName;
        RectTransform timerFill;
        Image timerFillImage;
        Text timerText;
        Text scoreText;
        Text streakText;
        Text progressText;
        Image[] hearts;
        Text feedbackText;
        CanvasGroup feedbackGroup;
        Text countdownText;

        // İl adı etiketi
        RectTransform nameTag;
        Text nameTagText;
        CanvasGroup nameTagGroup;
        ProvinceView nameTagTarget;

        // Ekranlar
        GameObject startScreen;
        Text startBestText;
        GameObject endScreen;
        Text endTitle;
        Text endScore;
        Text endStats;
        Text endBest;

        float displayedScore;
        int lastTickSecond;
        int heartsShown;

        public void Initialize(CityQuizGame quizGame, QuizConfig quizConfig, Camera camera, SfxPlayer sfxPlayer)
        {
            game = quizGame;
            config = quizConfig;
            cam = camera;
            sfx = sfxPlayer;

            BuildCanvas();
            BuildHud();
            BuildNameTag();
            BuildStartScreen();
            BuildEndScreen();

            game.GameStarted += OnGameStarted;
            game.CountdownTick += OnCountdown;
            game.QuestionStarted += OnQuestion;
            game.AnswerResolved += OnAnswer;
            game.GameEnded += OnGameEnded;

            ShowStartScreen();
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

        // ================== Kurulum ==================

        void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            canvasRect = (RectTransform)transform;
        }

        void BuildHud()
        {
            var hud = UIFactory.Rect("HUD", transform).Stretch();
            hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
            hudGroup.blocksRaycasts = false;
            hudGroup.interactable = false;

            // --- Hedef il kartı (üst orta) ---
            var card = UIFactory.Panel("TargetCard", hud, CardColor);
            targetCard = card.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(720, 156));

            UIFactory.Label("Prompt", targetCard, "BU İLİ HARİTADA BUL", 22, TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(680, 30));

            targetName = UIFactory.Label("TargetName", targetCard, "—", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            targetName.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -48), new Vector2(680, 66));

            var timerBg = UIFactory.Panel("TimerBg", targetCard, new Color(1f, 1f, 1f, 0.12f));
            timerBg.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-36, 20), new Vector2(560, 16));
            timerFillImage = UIFactory.Panel("TimerFill", timerBg.transform, Green);
            timerFill = timerFillImage.rectTransform;
            timerFill.anchorMin = Vector2.zero;
            timerFill.anchorMax = Vector2.one;
            timerFill.pivot = new Vector2(0f, 0.5f);
            timerFill.offsetMin = Vector2.zero;
            timerFill.offsetMax = Vector2.zero;

            timerText = UIFactory.Label("TimerText", targetCard, "10.0", 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            timerText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(256, 10), new Vector2(90, 36));

            // --- Puan (sol üst) ---
            var scorePanel = UIFactory.Panel("ScorePanel", hud, CardColor);
            scorePanel.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -24), new Vector2(300, 112));
            UIFactory.Label("ScoreTitle", scorePanel.transform, "PUAN", 20, TextMuted, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -14), new Vector2(200, 26));
            scoreText = UIFactory.Label("Score", scorePanel.transform, "0", 52, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            scoreText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24, -42), new Vector2(260, 60));

            streakText = UIFactory.Label("Streak", hud, "", 30, Orange, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            streakText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(34, -166), new Vector2(400, 40));

            // --- İlerleme ve canlar (sağ üst) ---
            var rightPanel = UIFactory.Panel("StatusPanel", hud, CardColor);
            rightPanel.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -24), new Vector2(300, 112));
            progressText = UIFactory.Label("Progress", rightPanel.transform, "SORU 0 / 20", 22, TextMuted, TextAnchor.MiddleRight, FontStyle.Bold);
            progressText.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -14), new Vector2(260, 26));

            hearts = new Image[config.maxMistakes];
            for (int i = 0; i < hearts.Length; i++)
            {
                var h = UIFactory.Icon("Heart" + i, rightPanel.transform, UIFactory.Heart, HeartOn);
                int fromRight = hearts.Length - 1 - i;
                h.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24 - fromRight * 58, 14), new Vector2(50, 50));
                hearts[i] = h;
            }

            // --- Geri bildirim yazısı ---
            feedbackText = UIFactory.Label("Feedback", hud, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            feedbackText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -196), new Vector2(1000, 50));
            var outline = feedbackText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            outline.effectDistance = new Vector2(2f, -2f);
            feedbackGroup = feedbackText.gameObject.AddComponent<CanvasGroup>();
            feedbackGroup.alpha = 0f;

            // --- Kontrol ipucu (alt) ---
            UIFactory.Label("Hint", hud,
                "Sol tık: il seç   •   Tekerlek: yakınlaştır   •   Sağ tık + sürükle: kaydır   •   Orta tık: görünümü sıfırla",
                20, new Color(0.08f, 0.18f, 0.28f, 0.75f), TextAnchor.MiddleCenter)
                .rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(1600, 30));

            // --- Geri sayım ---
            countdownText = UIFactory.Label("Countdown", transform, "", 200, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            countdownText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(800, 260));
            var cdOutline = countdownText.gameObject.AddComponent<Outline>();
            cdOutline.effectColor = new Color(0.05f, 0.1f, 0.18f, 0.6f);
            cdOutline.effectDistance = new Vector2(4f, -4f);
            countdownText.gameObject.SetActive(false);

            hudGroup.alpha = 0f;
        }

        void BuildNameTag()
        {
            var tag = UIFactory.Panel("NameTag", transform, new Color(0.11f, 0.15f, 0.22f, 0.92f));
            nameTag = tag.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(200, 52));
            nameTagText = UIFactory.Label("Text", nameTag, "", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            nameTagText.rectTransform.Stretch();
            nameTagGroup = tag.gameObject.AddComponent<CanvasGroup>();
            nameTagGroup.blocksRaycasts = false;
            nameTagGroup.alpha = 0f;
        }

        void BuildStartScreen()
        {
            var overlay = UIFactory.Solid("StartScreen", transform, new Color(0.03f, 0.07f, 0.12f, 0.55f), true);
            overlay.rectTransform.Stretch();
            startScreen = overlay.gameObject;

            var card = UIFactory.Panel("Card", overlay.transform, CardColor, true);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 640));

            UIFactory.Label("Title", card.transform, "TÜRKİYE İLLER", 70, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(760, 80));
            UIFactory.Label("Subtitle", card.transform, "KPSS Coğrafya  •  İli Bul", 28, Orange, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -128), new Vector2(760, 36));

            string rules =
                $"•  Rastgele <b>{config.questionCount}</b> il sorulur, her biri için <b>{config.timePerQuestion:0}</b> saniyen var.\n" +
                "•  Doğru ili haritada tıkla, süre yenilensin.\n" +
                "•  Yanlış tıklama ya da süre bitimi <b>1 hata</b> sayılır.\n" +
                $"•  <b>{config.maxMistakes}</b> hatada elenirsin.\n" +
                "•  Hızlı ve art arda doğru cevaplar daha çok puan getirir.";
            var rulesText = UIFactory.Label("Rules", card.transform, rules, 27, new Color(1f, 1f, 1f, 0.9f), TextAnchor.UpperLeft);
            rulesText.lineSpacing = 1.35f;
            rulesText.horizontalOverflow = HorizontalWrapMode.Wrap;
            rulesText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -196), new Vector2(700, 260));

            startBestText = UIFactory.Label("Best", card.transform, "", 24, TextMuted, TextAnchor.MiddleCenter);
            startBestText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(700, 30));

            UIFactory.Button("StartButton", card.transform, "BAŞLA", ButtonGreen, new Vector2(340, 88), OnStartClicked)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 44), new Vector2(340, 88));
        }

        void BuildEndScreen()
        {
            var overlay = UIFactory.Solid("EndScreen", transform, new Color(0.03f, 0.07f, 0.12f, 0.55f), true);
            overlay.rectTransform.Stretch();
            endScreen = overlay.gameObject;

            var card = UIFactory.Panel("Card", overlay.transform, CardColor, true);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 600));

            endTitle = UIFactory.Label("Title", card.transform, "", 68, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            endTitle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(680, 80));

            UIFactory.Label("ScoreTitle", card.transform, "PUAN", 22, TextMuted, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(680, 30));
            endScore = UIFactory.Label("Score", card.transform, "0", 96, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            endScore.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(680, 110));

            endStats = UIFactory.Label("Stats", card.transform, "", 28, new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter);
            endStats.lineSpacing = 1.3f;
            endStats.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -300), new Vector2(680, 90));

            endBest = UIFactory.Label("Best", card.transform, "", 28, Orange, TextAnchor.MiddleCenter, FontStyle.Bold);
            endBest.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(680, 36));

            UIFactory.Button("RestartButton", card.transform, "TEKRAR OYNA", ButtonGreen, new Vector2(380, 88), OnStartClicked)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 44), new Vector2(380, 88));

            endScreen.SetActive(false);
        }

        // ================== Ekran geçişleri ==================

        void ShowStartScreen()
        {
            int best = game.BestScore;
            startBestText.text = best > 0 ? $"En yüksek puan: {best}" : "";
            startScreen.SetActive(true);
            endScreen.SetActive(false);
            PopIn(startScreen.transform.GetChild(0));
        }

        void OnStartClicked()
        {
            sfx.Play(Sfx.Click);
            game.StartGame();
        }

        void OnGameStarted()
        {
            startScreen.SetActive(false);
            endScreen.SetActive(false);
            HideNameTag(true);

            displayedScore = 0;
            scoreText.text = "0";
            streakText.text = "";
            targetName.text = "Hazır ol...";
            progressText.text = $"SORU 0 / {game.TotalQuestions}";
            SetTimerVisual(1f, game.TimeLimit);
            heartsShown = hearts.Length;
            foreach (var h in hearts)
            {
                h.color = HeartOn;
                h.rectTransform.localScale = Vector3.one;
            }
            Animate("hud", Tween.Run(0.3f, t => hudGroup.alpha = t));
        }

        void OnCountdown(int n)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = n > 0 ? n.ToString() : "BAŞLA!";
            countdownText.fontSize = n > 0 ? 200 : 150;
            var rt = countdownText.rectTransform;
            Animate("countdown", Tween.Run(n > 0 ? 0.65f : 0.45f, t =>
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
            targetName.text = q.Target.DisplayName.ToUpper(new System.Globalization.CultureInfo("tr-TR"));
            progressText.text = $"SORU {q.Index + 1} / {q.Total}";
            lastTickSecond = Mathf.CeilToInt(game.TimeLimit) + 1;
            PunchScale("card", targetCard, 0.08f, 0.3f);
        }

        void OnAnswer(AnswerResult r)
        {
            if (r.IsCorrect)
            {
                ShowFeedback(r.Streak >= 3 ? "HARİKA!" : "DOĞRU!", Green);
                SpawnFloatingText($"+{r.Points}", r.Target, Green);
                ShowNameTag(r.Target, Green);
                if (r.Streak >= 2)
                {
                    streakText.text = $"SERİ x{r.Streak}   (puan ×{r.Multiplier:0.0})";
                    PunchScale("streak", streakText.rectTransform, 0.25f, 0.3f);
                }
            }
            else
            {
                string msg = r.Outcome == AnswerOutcome.TimedOut
                    ? $"SÜRE DOLDU!  Doğru cevap: {r.Target.DisplayName}"
                    : $"YANLIŞ! ({r.Clicked.DisplayName})  Doğru cevap: {r.Target.DisplayName}";
                ShowFeedback(msg, Red);
                ShowNameTag(r.Target, Red);
                streakText.text = "";
                LoseHeart();
            }
        }

        void OnGameEnded(GameResult res)
        {
            HideNameTag(false);
            Animate("hud", Tween.Run(0.25f, t => hudGroup.alpha = 1f - t));

            endTitle.text = res.Completed ? "TEBRİKLER!" : "ELENDİN!";
            endTitle.color = res.Completed ? Green : Red;
            endScore.text = res.Score.ToString();
            int accuracy = res.Answered > 0 ? Mathf.RoundToInt(100f * res.Correct / res.Answered) : 0;
            endStats.text = $"Doğru: <b>{res.Correct} / {res.Total}</b>     İsabet: <b>%{accuracy}</b>\nEn uzun seri: <b>{res.BestStreak}</b>";
            endBest.text = res.IsNewBest ? "★ YENİ REKOR! ★" : $"En yüksek puan: {res.BestScore}";

            endScreen.SetActive(true);
            PopIn(endScreen.transform.GetChild(0));
        }

        // ================== Her kare ==================

        void Update()
        {
            if (game == null) return;

            if (game.State == QuizState.Playing)
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

            // Puanı yumuşakça say
            if (Mathf.Abs(displayedScore - game.Score) > 0.5f)
            {
                displayedScore = Mathf.Lerp(displayedScore, game.Score, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
                if (Mathf.Abs(displayedScore - game.Score) < 1f) displayedScore = game.Score;
                scoreText.text = Mathf.RoundToInt(displayedScore).ToString();
            }
            else if (scoreText.text != game.Score.ToString())
            {
                displayedScore = game.Score;
                scoreText.text = game.Score.ToString();
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
            timerFillImage.color = ratio > 0.5f ? Green : ratio > 0.25f ? Yellow : Red;
            timerText.text = seconds.ToString("0.0");
            timerText.color = ratio > 0.25f ? Color.white : Red;
        }

        // ================== Yardımcılar ==================

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
                h.color = Color.Lerp(HeartOn, HeartOff, t);
            }));
        }

        void ShowFeedback(string text, Color color)
        {
            feedbackText.text = text;
            feedbackText.color = color;
            var rt = feedbackText.rectTransform;
            Animate("feedback", Tween.Run(1.4f, t =>
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
            nameTagText.color = Color.white;
            nameTag.sizeDelta = new Vector2(nameTagText.preferredWidth + 40f, 52f);
            nameTag.GetComponent<Image>().color = Color.Lerp(new Color(0.11f, 0.15f, 0.22f, 0.94f), accent, 0.35f);
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
            var label = UIFactory.Label("Floating", transform, text, 46, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            var o = label.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.55f);
            o.effectDistance = new Vector2(2f, -2f);
            var rt = label.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 60));
            Vector2 start = WorldToCanvas(at.transform.position) + new Vector2(0f, 90f);
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
            // Yer tutucu anchor'ı sol alt (0,0) olduğu için canvas'ın sol alt köşesine göre konum
            return local + canvasRect.rect.size * 0.5f;
        }

        void PunchScale(string key, RectTransform rt, float amount, float duration)
        {
            Animate(key, Tween.Run(duration, t =>
            {
                float s = 1f + amount * Tween.Punch(t);
                rt.localScale = new Vector3(s, s, 1f);
            }));
        }

        void PopIn(Transform t)
        {
            var rt = (RectTransform)t;
            Animate("pop_" + t.parent.name, Tween.Run(0.35f, x =>
            {
                float s = Mathf.Lerp(0.85f, 1f, Tween.EaseOutBack(x));
                rt.localScale = new Vector3(s, s, 1f);
            }));
        }

        void Animate(string key, IEnumerator routine)
        {
            StopAnim(key);
            running[key] = StartCoroutine(routine);
        }

        void StopAnim(string key)
        {
            if (running.TryGetValue(key, out var c) && c != null) StopCoroutine(c);
            running.Remove(key);
        }
    }
}
