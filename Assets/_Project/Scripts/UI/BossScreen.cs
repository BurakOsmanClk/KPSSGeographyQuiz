using System;
using KPSSGeography.Core;
using KPSSGeography.Map;
using KPSSGeography.Meta;
using KPSSGeography.Quiz;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Boss savaşı ekranı: üstte boss (süzülür, vurulunca titrer ve kızarır, yenilince yanıp sönerek kaybolur),
    /// can barı ve adı; ortada harf harf yazılan diyalog kutusu; altta harita (il tıklanır), canlar, raund ve
    /// kalıcı olarak kapatılan il sayısı. Oyun akışı CityQuizGame'de (QuizMode.Boss); bu ekran yalnızca olayları dinler.
    /// </summary>
    public class BossScreen : UIScreen
    {
        class BossDef
        {
            public string name;
            public string texture;
            public string intro;
            public string defeat;
        }

        static readonly BossDef[] Bosses =
        {
            new BossDef
            {
                name = "FAY HATTI CANAVARI", texture = "Textures/Bosses/boss_fault",
                intro = "* Yerin altından bir gürültü geliyor...\n* FAY HATTI CANAVARI yolunu kesti!",
                defeat = "* Fay Hattı Canavarı çatırdayarak dağıldı!"
            },
            new BossDef
            {
                name = "İLLER DEVİ", texture = "Textures/Bosses/boss_provinces",
                intro = "* Haritanın parçaları bir araya geldi...\n* İLLER DEVİ ortaya çıktı!",
                defeat = "* İller Devi yamalarına ayrıldı!"
            }
        };

        const int BossHp = 3;
        const float CharsPerSecond = 48f;

        /// <summary>Boss savaşının bölüm tanımı (levels.json'da değil; ana ekrandaki BOSS butonuyla açılır).</summary>
        public static readonly LevelDefinition Level = new LevelDefinition
        {
            id = "boss-1", group = "", title = "Boss Savaşı", description = "", mode = "boss",
            regions = Array.Empty<string>(), questionCount = 6, timePerQuestion = 20, maxMistakes = 3
        };

        public event Action ExitRequested;
        public event Action RetryRequested;

        CityQuizGame game;
        TurkeyMap map;
        SfxPlayer sfx;

        RectTransform bossRect;
        Image bossImage;
        Text bossName;
        Image[] hpSegments;
        Text dialog;
        Image timerFill;
        Image[] hearts;
        Text roundText;
        Text closedText;
        RectTransform endCard;
        Text endTitle;
        Text endBody;

        // Boss durumu
        int bossIndex = -1;
        int hp;
        int round;
        float time;
        float hitTime = -1f;
        float lungeTime = -1f;
        float defeatTime = -1f;
        float introTime = -1f;
        bool pendingNext;
        float endCardAt = -1f;

        // Daktilo
        string fullText = "";
        float shownChars;

        static readonly Vector2 BossAnchor = new Vector2(0f, -36f);
        const float BossHeight = 300f;

        public void Initialize(Transform parent, CityQuizGame quizGame, TurkeyMap turkeyMap, SfxPlayer sfxPlayer)
        {
            game = quizGame;
            map = turkeyMap;
            sfx = sfxPlayer;
            CreateRoot(parent, "BossScreen");
            Build();
            game.GameStarted += OnGameStarted;
            game.QuestionStarted += OnQuestion;
            game.AnswerResolved += OnAnswer;
            game.GameEnded += OnGameEnded;
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.GameStarted -= OnGameStarted;
            game.QuestionStarted -= OnQuestion;
            game.AnswerResolved -= OnAnswer;
            game.GameEnded -= OnGameEnded;
        }

        // ================== Arayüz ==================

        void Build()
        {
            // Üst yarıyı hafifçe karart: boss ve diyalog haritadan ayrışsın
            var top = UIFactory.Icon("TopShade", Root, ShapeSprites.GradientUp, Theme.Hex(0x071827, 0.55f));
            top.preserveAspect = false;
            top.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.offsetMin = Vector2.zero;
            top.rectTransform.offsetMax = Vector2.zero;
            top.rectTransform.localScale = new Vector3(1f, -1f, 1f); // koyu taraf yukarıda

            // Boss
            bossImage = UIFactory.Solid("Boss", Root, Color.white);
            bossImage.preserveAspect = true;
            bossRect = bossImage.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), BossAnchor, new Vector2(BossHeight, BossHeight));

            // Ad ve can barı
            bossName = UIFactory.Label("Name", Root, "", 30, Theme.Amber, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            bossName.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -348), new Vector2(900, 40));
            var hpBack = UIFactory.Panel("HpBack", Root, Theme.Hex(0x0B2239, 0.9f));
            hpBack.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -392), new Vector2(480, 30));
            hpSegments = new Image[BossHp];
            for (int i = 0; i < BossHp; i++)
            {
                var seg = UIFactory.Panel("Hp" + i, hpBack.transform, Theme.HeartOn);
                float w = (480f - 12f - (BossHp - 1) * 6f) / BossHp;
                seg.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6 + i * (w + 6f), 0), new Vector2(w, 18));
                hpSegments[i] = seg;
            }

            // Diyalog kutusu: beyaz çerçeveli koyu kutu, harf harf yazı
            var frame = UIFactory.Solid("DialogFrame", Root, Color.white);
            frame.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -432), new Vector2(1260, 128));
            var inner = UIFactory.Solid("DialogInner", frame.transform, Theme.Hex(0x0B2239, 0.97f));
            inner.rectTransform.Stretch(5, 5, 5, 5);
            dialog = UIFactory.Label("Dialog", inner.transform, "", 32, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            dialog.horizontalOverflow = HorizontalWrapMode.Wrap;
            dialog.lineSpacing = 1.1f;
            dialog.rectTransform.Stretch(26, 26, 16, 12);
            var timerBack = UIFactory.Solid("TimerBack", Root, Theme.Hex(0xFFFFFF, 0.18f));
            timerBack.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -566), new Vector2(1260, 8));
            timerFill = UIFactory.Solid("TimerFill", timerBack.transform, Theme.Amber);
            timerFill.rectTransform.anchorMin = Vector2.zero;
            timerFill.rectTransform.anchorMax = Vector2.one;
            timerFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            timerFill.rectTransform.offsetMin = Vector2.zero;
            timerFill.rectTransform.offsetMax = Vector2.zero;

            // Alt bilgi: canlar, raund, kapatılan iller
            var bar = UIFactory.Card("Bottom", Root, Theme.NavyPanel);
            bar.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28, 24), new Vector2(430, 72));
            hearts = new Image[3];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = UIFactory.Icon("Heart" + i, bar.transform, UIFactory.Heart, Theme.HeartOn);
                hearts[i].rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18 + i * 50, 0), new Vector2(42, 42));
            }
            roundText = UIFactory.Label("Round", bar.transform, "", 26, Theme.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            roundText.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(240, 40));

            var closed = UIFactory.Card("Closed", Root, Theme.NavyPanel);
            closed.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28, 24), new Vector2(380, 72));
            closedText = UIFactory.Label("Text", closed.transform, "", 26, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            closedText.rectTransform.Stretch();

            // Çıkış
            UIFactory.IconButton("Exit", Root, ShapeSprites.Cross, Theme.NavyPanel, 76, () =>
                {
                    sfx.Play(Sfx.Click);
                    ExitRequested?.Invoke();
                })
                .GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28, -28), new Vector2(76, 76));

            // Sonuç kartı
            var card = UIFactory.Card("EndCard", Root, Theme.NavyPanel, true);
            endCard = card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(760, 380));
            endTitle = UIFactory.Label("Title", endCard, "", 64, Theme.Amber, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            endTitle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(700, 80));
            endBody = UIFactory.Label("Body", endCard, "", 28, Theme.Text, TextAnchor.UpperCenter);
            endBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            endBody.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -126), new Vector2(680, 110));
            UIFactory.Button("Retry", endCard, "TEKRAR", Theme.Amber, new Vector2(300, 84), () =>
                {
                    sfx.Play(Sfx.Click);
                    RetryRequested?.Invoke();
                }, Theme.Ink, 32)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-165, 30), new Vector2(300, 84));
            UIFactory.Button("Home", endCard, "ANA EKRAN", Theme.NavyLight, new Vector2(300, 84), () =>
                {
                    sfx.Play(Sfx.Click);
                    ExitRequested?.Invoke();
                }, null, 30)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(165, 30), new Vector2(300, 84));
            endCard.gameObject.SetActive(false);
        }

        // ================== Oyun olayları ==================

        void OnGameStarted()
        {
            if (game.Mode != QuizMode.Boss) return;
            Show();
            endCard.gameObject.SetActive(false);
            endCardAt = -1f;
            round = 1;
            pendingNext = false;
            defeatTime = -1f;
            hitTime = -1f;
            lungeTime = -1f;

            // Daha önce kapatılmış iller haritada boyalı başlar
            foreach (int plate in BossProgress.Plates)
                map.GetByPlate(plate)?.SetState(ProvinceState.Correct);

            SetBoss(0);
            RefreshFooter();
        }

        void OnQuestion(QuestionInfo info)
        {
            if (game.Mode != QuizMode.Boss) return;
            var q = info.Question;
            if (q.Round != round)
            {
                round = q.Round;
                if (bossIndex != round - 1 && !pendingNext) SetBoss(round - 1);
            }
            Type("* " + q.Title);
            RefreshFooter();
        }

        void OnAnswer(AnswerResult r)
        {
            if (game.Mode != QuizMode.Boss) return;
            if (r.IsCorrect)
            {
                string closedName = "";
                if (r.Clicked is ProvinceView p)
                {
                    BossProgress.Close(p.Data.plate);
                    closedName = $" {p.DisplayName} kapandı.";
                }
                hp = Mathf.Max(0, hp - 1);
                RefreshHp();
                if (hp == 0)
                {
                    defeatTime = 0f;
                    pendingNext = bossIndex + 1 < Bosses.Length;
                    Type(Bosses[bossIndex].defeat + closedName);
                    sfx.Play(Sfx.Victory);
                }
                else
                {
                    hitTime = 0f;
                    Type($"* Doğru!{closedName} {Bosses[bossIndex].name} sarsıldı!");
                }
            }
            else
            {
                lungeTime = 0f;
                string msg = r.Outcome == AnswerOutcome.TimedOut ? "* Süre doldu!"
                    : r.Clicked != null ? $"* Yanlış! Burası {r.Clicked.DisplayName}." : "* Yanlış!";
                Type(msg + " Bir canın gitti.");
            }
            RefreshFooter();
        }

        void OnGameEnded(GameResult res)
        {
            if (game.Mode != QuizMode.Boss) return;
            RefreshFooter();
            endTitle.text = res.Completed ? "ZAFER!" : "YENİLDİN";
            endTitle.color = res.Completed ? Theme.Amber : Theme.Error;
            endBody.text = res.Completed
                ? $"İki boss'u da yendin.\nKapatılan il: {BossProgress.Count} / {BossProgress.TotalProvinces}"
                : $"{Bosses[Mathf.Max(0, bossIndex)].name} kazandı. Tekrar dene!\nKapatılan il: {BossProgress.Count} / {BossProgress.TotalProvinces}";
            if (!res.Completed) Type("* Canın bitti...");
            // Son boss'un kayboluşu bitsin, sonra kart gelsin
            endCardAt = time + (res.Completed ? 1.2f : 0.4f);
        }

        public override void Hide()
        {
            base.Hide();
            endCard.gameObject.SetActive(false);
            endCardAt = -1f;
        }

        // ================== Boss ==================

        void SetBoss(int index)
        {
            bossIndex = Mathf.Clamp(index, 0, Bosses.Length - 1);
            var def = Bosses[bossIndex];
            var tex = Resources.Load<Texture2D>(def.texture);
            if (tex != null)
            {
                bossImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                float w = BossHeight * tex.width / Mathf.Max(1f, tex.height);
                bossRect.sizeDelta = new Vector2(Mathf.Min(w, 460f), BossHeight);
            }
            else Debug.LogWarning($"[KPSS] Boss görseli bulunamadı: Resources/{def.texture}.png");
            bossName.text = def.name;
            bossImage.enabled = true;
            hp = BossHp;
            RefreshHp();
            defeatTime = -1f;
            hitTime = -1f;
            introTime = 0f;
            Type(def.intro);
        }

        void RefreshHp()
        {
            for (int i = 0; i < hpSegments.Length; i++)
                hpSegments[i].color = i < hp ? Theme.HeartOn : Theme.HeartOff;
        }

        void RefreshFooter()
        {
            int lives = Mathf.Max(0, game.MaxMistakes - game.Mistakes);
            for (int i = 0; i < hearts.Length; i++) hearts[i].color = i < lives ? Theme.HeartOn : Theme.HeartOff;
            roundText.text = $"RAUND {Mathf.Max(1, round)}/{Bosses.Length}";
            closedText.text = $"KAPATILAN İL: {BossProgress.Count} / {BossProgress.TotalProvinces}";
        }

        void Type(string text)
        {
            fullText = text ?? "";
            shownChars = 0f;
            dialog.text = "";
        }

        void Update()
        {
            if (!IsVisible) return;
            float dt = Time.deltaTime;
            time += dt;

            // Daktilo
            if (shownChars < fullText.Length)
            {
                shownChars = Mathf.Min(fullText.Length, shownChars + CharsPerSecond * dt);
                dialog.text = fullText.Substring(0, Mathf.FloorToInt(shownChars));
            }

            // Süre çubuğu
            bool playing = game.State == QuizState.Playing;
            timerFill.rectTransform.localScale = new Vector3(playing ? game.TimeRatio : 0f, 1f, 1f);
            timerFill.color = game.TimeRatio < 0.3f ? Theme.Error : Theme.Amber;

            AnimateBoss(dt);

            if (endCardAt > 0f && time >= endCardAt)
            {
                endCardAt = -1f;
                endCard.gameObject.SetActive(true);
                endCard.SetAsLastSibling();
                PopIn("endcard", endCard, 0.85f, 0.35f);
            }
        }

        /// <summary>Boss hareketleri: süzülme, vurulunca sağa sola titreme ve kızarma, yanlışta öne atılma,
        /// yenilince yanıp sönme ve kaybolma, yeni boss'un yukarıdan inişi.</summary>
        void AnimateBoss(float dt)
        {
            Vector2 pos = BossAnchor + new Vector2(0f, Mathf.Sin(time * 2.1f) * 8f);
            float scale = 1f + Mathf.Sin(time * 3.2f) * 0.015f;
            Color tint = Color.white;
            float alpha = 1f;

            if (introTime >= 0f)
            {
                introTime += dt;
                float t = Mathf.Clamp01(introTime / 0.75f);
                pos += new Vector2(0f, Mathf.LerpUnclamped(220f, 0f, Tween.EaseOutBack(t)));
                alpha *= t;
                if (t >= 1f) introTime = -1f;
            }

            if (hitTime >= 0f)
            {
                hitTime += dt;
                float t = Mathf.Clamp01(hitTime / 0.5f);
                pos.x += Mathf.Sin(hitTime * 60f) * 24f * (1f - t);
                tint = Color.Lerp(new Color(1f, 0.45f, 0.45f), Color.white, t);
                if (t >= 1f) hitTime = -1f;
            }

            if (lungeTime >= 0f)
            {
                lungeTime += dt;
                float t = Mathf.Clamp01(lungeTime / 0.4f);
                scale *= 1f + 0.16f * Mathf.Sin(t * Mathf.PI);
                pos.y -= 30f * Mathf.Sin(t * Mathf.PI);
                if (t >= 1f) lungeTime = -1f;
            }

            if (defeatTime >= 0f)
            {
                defeatTime += dt;
                const float flicker = 1.3f, fade = 0.6f;
                if (defeatTime < flicker)
                {
                    // Gidip gelme: hızlı yanıp sönme + titreme
                    alpha *= Mathf.FloorToInt(defeatTime * 12f) % 2 == 0 ? 1f : 0.15f;
                    pos.x += Mathf.Sin(defeatTime * 45f) * 12f;
                    tint = new Color(1f, 0.6f, 0.6f);
                }
                else
                {
                    float t = Mathf.Clamp01((defeatTime - flicker) / fade);
                    alpha *= 1f - t;
                    scale *= Mathf.Lerp(1f, 0.6f, t);
                    pos.y += 40f * t;
                    if (t >= 1f)
                    {
                        defeatTime = -1f;
                        alpha = 0f;
                        if (pendingNext)
                        {
                            pendingNext = false;
                            SetBoss(bossIndex + 1);
                        }
                        else bossImage.enabled = false;
                    }
                }
            }

            bossRect.anchoredPosition = pos;
            bossRect.localScale = new Vector3(scale, scale, 1f);
            tint.a = alpha;
            bossImage.color = tint;
        }
    }
}
