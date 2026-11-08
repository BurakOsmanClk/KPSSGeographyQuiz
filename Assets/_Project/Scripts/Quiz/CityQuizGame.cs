using System;
using System.Collections;
using System.Collections.Generic;
using KPSSGeography.Core;
using KPSSGeography.Map;
using KPSSGeography.Meta;
using UnityEngine;

namespace KPSSGeography.Quiz
{
    /// <summary>
    /// Bölüm oyun akışı. Bütün modlar ortak <see cref="Question"/> modeliyle çalışır:
    /// haritadan seçme (il / yer şekli, tek ya da çok doğru cevaplı), butonlardan seçme (bölge, iklim, plaka, göl tipi, KPSS sorusu)
    /// ve "hepsini bul" (bir bölgenin bütün illeri).
    /// Her soru için süre akar; doğru cevap puan + seri bonusu kazandırır. Yanlış cevap ya da süre bitimi bir hata sayılır.
    /// Hata sayısı sınıra ulaşınca (isteğe bağlı) reklamla devam teklifi gelir, kabul edilmezse oyuncu elenir.
    /// Yıldız: tamamlarsa 3 − hata (en az 1), elenirse 0.
    /// </summary>
    public class CityQuizGame : MonoBehaviour
    {
        static readonly Rect FindViewport = new Rect(0.03f, 0.06f, 0.94f, 0.72f);
        static readonly Rect ChoiceViewport = new Rect(0.03f, 0.2f, 0.94f, 0.58f);
        static readonly Rect McqViewport = new Rect(0.03f, 0.3f, 0.94f, 0.44f);

        public event Action GameStarted;
        public event Action<int> CountdownTick;
        public event Action<QuestionInfo> QuestionStarted;
        public event Action<AnswerResult> AnswerResolved;
        public event Action ContinueOffered;
        public event Action ContinueGranted;
        public event Action<GameResult> GameEnded;

        QuizConfig config;
        TurkeyMap map;
        FeatureLayer features;
        SfxPlayer sfx;
        MapCameraController cameraController;
        ProgressStore progress;
        QuestionBuilder builder;
        ReliefLayer relief;
        MapEffects effects;
        readonly List<ReviewItem> history = new List<ReviewItem>();

        /// <summary>Turda sorulan yerler ve sonuçları ("haritayı incele").</summary>
        public IReadOnlyList<ReviewItem> History => history;

        List<Question> questions = new List<Question>();
        readonly HashSet<IMapTarget> found = new HashSet<IMapTarget>();
        Coroutine flow;
        bool continueUsed;

        public LevelDefinition Level { get; private set; }

        /// <summary>
        /// Bu tur sınav modunda mı? Sınav modunda: soru süresi kısa, ipuçları kapalı (vurgulama, kalıcı boyama, ışıltı,
        /// anlık bilgi notu), puan çarpanlı ve yıldız/rekorlar ayrı kaydedilir.
        /// </summary>
        public bool IsExam { get; private set; }

        /// <summary>
        /// Yer şekli bölümlerinde (dağ, ova, plato, akarsu, göl, kıyı, fay...) il sınırları ve il adları gizlenir;
        /// harita yalnızca Türkiye'nin dış hattıyla boş silüet olarak görünür. UNESCO ve milli park bölümlerinde iller kalır.
        /// </summary>
        public bool HidesProvinces => Level != null && (Mode == QuizMode.Feature || Mode == QuizMode.FeatureChoice) &&
                                      Level.category != "unesco" && Level.category != "park";

        float ScoreScale => IsExam ? config.examScoreMultiplier : 1f;
        string ProgressId => IsExam ? ProgressStore.ExamKey(Level.id) : Level.id;
        float QuestionTime => Mathf.Max(1f, Level.timePerQuestion * (IsExam ? config.examTimeScale : 1f));
        public QuizMode Mode => Level != null ? Level.Mode : QuizMode.FindProvince;
        public QuizState State { get; private set; } = QuizState.Idle;
        public bool IsPaused { get; private set; }
        public Question Current { get; private set; }
        public int QuestionIndex { get; private set; }
        public int TotalQuestions => questions.Count;
        public int FoundCount => found.Count;
        public float TimeRemaining { get; private set; }
        public float TimeLimit { get; private set; } = 10f;
        public float TimeRatio => TimeLimit > 0f ? Mathf.Clamp01(TimeRemaining / TimeLimit) : 0f;
        public int Score { get; private set; }
        public int Mistakes { get; private set; }
        public int MaxMistakes { get; private set; } = 3;
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int CorrectCount { get; private set; }

        /// <summary>Devam teklifinin yapılıp yapılamayacağını (ör. ödüllü reklam hazır mı) dışarıdan söyler.</summary>
        public Func<bool> CanOfferContinue;

        /// <summary>Bölümde butonlu soru var mı? (HUD ve kamera yerleşimi için)</summary>
        public bool UsesChoices => Mode == QuizMode.Region || Mode == QuizMode.Plate || Mode == QuizMode.Attribute ||
                                   Mode == QuizMode.FeatureChoice || Mode == QuizMode.Mcq;

        bool Answering => State == QuizState.Playing && !IsPaused && Current != null;
        public bool AcceptsMapInput => Answering && Current.Type == AnswerType.Map && Current.Layer == MapLayer.Provinces;
        public bool AcceptsFeatureInput => Answering && Current.Type == AnswerType.Map && Current.Layer == MapLayer.Features;
        public bool AcceptsChoiceInput => Answering && Current.Type == AnswerType.Choice;
        public bool IsRunning => State != QuizState.Idle && State != QuizState.Finished;

        /// <summary>Doğru bilinen hedefler bölüm boyunca boyalı kalsın mı? ("haritayı doldurma" modları)</summary>
        /// Bölge modu bilerek yok: bölge rengine boyanan iller sonraki soruların cevabını ele verirdi.
        bool PersistentMarks => !IsExam && (Mode == QuizMode.FindProvince || Mode == QuizMode.Feature || Mode == QuizMode.RegionAll);

        public float StreakMultiplier(int streak) =>
            1f + Mathf.Clamp(streak - 1, 0, config.maxStreakSteps) * config.streakStep;

        public void Initialize(QuizConfig quizConfig, TurkeyMap turkeyMap, FeatureLayer featureLayer, SfxPlayer sfxPlayer,
            MapCameraController camController, ProgressStore progressStore, QuestionBuilder questionBuilder,
            ReliefLayer reliefLayer, MapEffects mapEffects)
        {
            relief = reliefLayer;
            effects = mapEffects;
            config = quizConfig;
            map = turkeyMap;
            features = featureLayer;
            sfx = sfxPlayer;
            cameraController = camController;
            progress = progressStore;
            builder = questionBuilder;
        }

        public void StartLevel(LevelDefinition level)
        {
            if (level == null || !level.IsPlayable) return;
            StopFlow();
            SetPaused(false);
            Level = level;
            IsExam = level.ExamActive;
            flow = StartCoroutine(StartRoutine());
        }

        /// <summary>Oyunu yarıda bırakır ve haritayı menü görünümüne döndürür.</summary>
        public void Abort()
        {
            StopFlow();
            SetPaused(false);
            Current?.Focus?.SetFocused(false);
            Current = null;
            State = QuizState.Idle;
            map.ResetAll();
            features.ShowDefault();
            cameraController.SetAttractMode();
            effects?.Clear();
            relief?.SetStrength(0.8f);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (cameraController != null) cameraController.InputEnabled = !paused;
        }

        void StopFlow()
        {
            if (flow != null) StopCoroutine(flow);
            flow = null;
        }

        IEnumerator StartRoutine()
        {
            map.ResetAll();
            effects?.Clear();
            history.Clear();
            Rect focus = SetupMap();
            bool physical = Mode == QuizMode.Feature || Mode == QuizMode.FeatureChoice;
            relief?.SetStrength(physical ? 1f : 0.55f);
            // Bölge sınırı yalnızca "ili bul" bölümlerinde çizilir; "Bölgeyi Tamamla"da çizilirse cevabın kendisi olur
            if (!IsExam && Level.HasRegionFilter && Mode == QuizMode.FindProvince)
            {
                var rings = new List<Vector2[]>();
                foreach (var region in Level.regions) rings.AddRange(map.GetRegionRings(region));
                effects?.DrawOutline(rings, new Color(1f, 0.72f, 0.1f, 1f));
            }
            Rect viewport = Mode == QuizMode.Mcq ? McqViewport : UsesChoices ? ChoiceViewport : FindViewport;
            cameraController.FocusOn(focus, viewport);

            questions = builder.Build(Level);
            if (questions.Count == 0) Debug.LogWarning($"[KPSS] '{Level.id}' bölümü için soru üretilemedi.");

            TimeLimit = QuestionTime;
            MaxMistakes = Mathf.Max(1, Level.maxMistakes);
            Score = 0;
            Mistakes = 0;
            Streak = 0;
            BestStreak = 0;
            CorrectCount = 0;
            QuestionIndex = -1;
            Current = null;
            TimeRemaining = TimeLimit;
            continueUsed = false;
            found.Clear();

            State = QuizState.Countdown;
            GameStarted?.Invoke();

            yield return new WaitForSeconds(0.45f);
            for (int n = 3; n >= 1; n--)
            {
                CountdownTick?.Invoke(n);
                sfx.Play(Sfx.Tick, 1.4f);
                yield return new WaitForSeconds(config.countdownStep);
            }
            CountdownTick?.Invoke(0);
            sfx.Play(Sfx.Go);
            yield return new WaitForSeconds(0.35f);

            NextQuestion();
        }

        /// <summary>Bölüm türüne göre il soluklaştırma ve yer şekli görünürlüğü. Dönüş: kameranın odaklanacağı alan.</summary>
        Rect SetupMap()
        {
            switch (Mode)
            {
                case QuizMode.Feature:
                    map.SetFocus(_ => false);
                    map.SetBordersHidden(HidesProvinces);
                    features.ShowCategory(Level.category);
                    return map.Bounds;
                case QuizMode.FeatureChoice:
                    map.SetFocus(_ => false);
                    map.SetBordersHidden(HidesProvinces);
                    features.ShowCategory(Level.category, false);
                    return map.Bounds;
                case QuizMode.FindProvince:
                    features.ShowDefault();
                    return map.SetFocus(Level.HasRegionFilter ? p => Level.IncludesRegion(p.Data.region) : null);
                case QuizMode.Region:
                case QuizMode.Attribute:
                    // Bölge renkleri cevabı ele vermesin
                    features.ShowDefault();
                    map.SetNeutral(true);
                    return map.SetFocus(Level.HasRegionFilter ? p => Level.IncludesRegion(p.Data.region) : null);
                case QuizMode.RegionAll:
                {
                    // Bütün Türkiye görünür, hiçbir il soluklaştırılmaz ve bölge sınırı çizilmez:
                    // kamera bölgeye yaklaşsaydı ya da sınır çizilseydi cevap haritada hazır olurdu. Bölge dışı il = yanlış.
                    features.ShowDefault();
                    map.ClearFocus();
                    map.SetNeutral(true);
                    return map.Bounds;
                }
                default:
                    features.ShowDefault();
                    map.SetFocus(null);
                    return map.Bounds;
            }
        }

        void NextQuestion()
        {
            QuestionIndex++;
            if (QuestionIndex >= questions.Count)
            {
                flow = StartCoroutine(FinishRoutine(true));
                return;
            }

            if (!PersistentMarks)
            {
                map.ClearStates();
                if (Mode != QuizMode.FeatureChoice) features.ClearStates();
            }

            Current = questions[QuestionIndex];
            found.Clear();
            if (Mode == QuizMode.Mcq)
            {
                if (Current.ShowFeature != null) features.ShowOnly(Current.ShowFeature);
                else features.ShowDefault();
            }
            if (!Current.FocusHidden) Current.Focus?.SetFocused(true);

            TimeLimit = QuestionTime;
            TimeRemaining = TimeLimit;
            State = QuizState.Playing;
            QuestionStarted?.Invoke(new QuestionInfo(Current, Mode, QuestionIndex, questions.Count));
        }

        void Update()
        {
            if (State != QuizState.Playing || IsPaused) return;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining > 0f) return;
            TimeRemaining = 0f;

            if (Current.RequireAll) RevealMissingAndFail();
            else ResolveWrong(null, -1, true);
        }

        // ================== Geçmiş ==================

        void Record(IMapTarget target, Question q, bool correct)
        {
            if (target == null) return;
            string label = q.Type == AnswerType.Choice && q.Focus == target && !string.IsNullOrEmpty(q.AnswerLabel)
                ? $"{target.DisplayName}: {q.AnswerLabel}"
                : target.DisplayName;
            history.RemoveAll(h => ReferenceEquals(h.Target, target));
            history.Add(new ReviewItem(target, label, q.Info, correct));
        }

        void Sparkle(IMapTarget target)
        {
            if (target != null && !IsExam) effects?.Sparkle(target.LabelWorldPosition, new Color(1f, 0.85f, 0.3f));
        }

        // ================== Cevaplar ==================

        public void SubmitProvince(ProvinceView clicked)
        {
            if (AcceptsMapInput && clicked != null) SubmitTarget(clicked);
        }

        public void SubmitFeature(FeatureView clicked)
        {
            if (AcceptsFeatureInput && clicked != null) SubmitTarget(clicked);
        }

        public void SubmitChoice(int index)
        {
            if (!AcceptsChoiceInput) return;
            if (index == Current.CorrectChoice) ResolveCorrect(null, index);
            else ResolveWrong(null, index, false);
        }

        void SubmitTarget(IMapTarget clicked)
        {
            if (Current.RequireAll)
            {
                SubmitPartial(clicked);
                return;
            }
            if (Current.Accepts(clicked)) ResolveCorrect(clicked, -1);
            else ResolveWrong(clicked, -1, false);
        }

        /// <summary>"Hepsini bul": her doğru il ayrı puanlanır; soru tüm iller bulununca biter.</summary>
        void SubmitPartial(IMapTarget clicked)
        {
            if (found.Contains(clicked)) return;

            if (Current.Accepts(clicked))
            {
                found.Add(clicked);
                Streak++;
                BestStreak = Mathf.Max(BestStreak, Streak);
                float multiplier = StreakMultiplier(Streak);
                int points = Mathf.RoundToInt(config.basePoints * 0.6f * multiplier * ScoreScale);
                Score += points;
                clicked.SetState(ProvinceState.Correct);
                clicked.PlayCorrect();
                sfx.Play(Sfx.Correct);
                Sparkle(clicked);
                Record(clicked, Current, true);

                bool done = found.Count >= Current.Correct.Count;
                if (done)
                {
                    CorrectCount++;
                    Score += Mathf.RoundToInt(config.maxTimeBonus * 3f * TimeRatio); // kalan süre bonusu
                    State = QuizState.Feedback;
                }
                AnswerResolved?.Invoke(new AnswerResult(AnswerOutcome.Correct, Mode, Current, clicked, -1, points, Streak,
                    multiplier, Mistakes, !done, found.Count));
                if (done) flow = StartCoroutine(AfterFeedback(config.correctFeedback, false));
                return;
            }

            Mistakes++;
            Streak = 0;
            clicked.PlayWrongClick();
            sfx.Play(Sfx.Wrong);
            cameraController.Shake();
            AnswerResolved?.Invoke(new AnswerResult(AnswerOutcome.Wrong, Mode, Current, clicked, -1, 0, 0, 1f, Mistakes, true, found.Count));
            if (Mistakes >= MaxMistakes)
            {
                State = QuizState.Feedback;
                flow = StartCoroutine(AfterFeedback(config.wrongFeedback, true));
            }
        }

        void RevealMissingAndFail()
        {
            foreach (var t in Current.Correct)
                if (!found.Contains(t))
                {
                    t.PlayReveal(config.wrongFeedback);
                    Record(t, Current, false);
                }
            Mistakes = MaxMistakes;
            State = QuizState.Feedback;
            sfx.Play(Sfx.Wrong);
            AnswerResolved?.Invoke(new AnswerResult(AnswerOutcome.TimedOut, Mode, Current, null, -1, 0, 0, 1f, Mistakes, false, found.Count));
            flow = StartCoroutine(FailAfter(config.wrongFeedback));
        }

        IEnumerator FailAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            flow = StartCoroutine(FinishRoutine(false));
        }

        float FeedbackBonus => string.IsNullOrEmpty(Current?.Info) ? 0f : config.featureFeedbackBonus;

        void ResolveCorrect(IMapTarget clicked, int chosenIndex)
        {
            Streak++;
            BestStreak = Mathf.Max(BestStreak, Streak);
            CorrectCount++;

            float multiplier = StreakMultiplier(Streak);
            int points = Mathf.RoundToInt((config.basePoints + config.maxTimeBonus * TimeRatio) * multiplier * ScoreScale);
            Score += points;

            Current.Focus?.SetFocused(false);
            IMapTarget mark = clicked ?? Current.Focus;
            if (mark != null)
            {
                mark.SetState(ProvinceState.Correct);
                mark.PlayCorrect();
                Sparkle(mark);
                Record(mark, Current, true);
            }
            sfx.Play(Sfx.Correct);

            State = QuizState.Feedback;
            AnswerResolved?.Invoke(new AnswerResult(AnswerOutcome.Correct, Mode, Current, clicked, chosenIndex, points, Streak, multiplier, Mistakes));
            flow = StartCoroutine(AfterFeedback(config.correctFeedback + FeedbackBonus * 0.5f, false));
        }

        void ResolveWrong(IMapTarget clicked, int chosenIndex, bool timedOut)
        {
            Mistakes++;
            Streak = 0;

            float delay = config.wrongFeedback + FeedbackBonus;
            clicked?.PlayWrongClick();
            Current.Focus?.SetFocused(false);

            if (Current.Reveal != null)
            {
                Current.Reveal.SetState(ProvinceState.Missed);
                Current.Reveal.PlayReveal(delay);
                Record(Current.Reveal, Current, false);
            }
            else if (Current.Type == AnswerType.Map)
            {
                foreach (var t in Current.Correct) t.PlayReveal(delay); // konu setleri, komşular: hepsini göster
            }
            if (Current.Focus != null && Current.Type == AnswerType.Choice)
            {
                Current.Focus.SetState(ProvinceState.Missed);
                Current.Focus.PlayReveal(delay);
                Record(Current.Focus, Current, false);
            }

            sfx.Play(Sfx.Wrong);
            cameraController.Shake();

            State = QuizState.Feedback;
            var outcome = timedOut ? AnswerOutcome.TimedOut : AnswerOutcome.Wrong;
            AnswerResolved?.Invoke(new AnswerResult(outcome, Mode, Current, clicked, chosenIndex, 0, 0, 1f, Mistakes));
            flow = StartCoroutine(AfterFeedback(delay, Mistakes >= MaxMistakes));
        }

        IEnumerator AfterFeedback(float delay, bool outOfLives)
        {
            yield return new WaitForSeconds(delay);
            if (!outOfLives)
            {
                NextQuestion();
                yield break;
            }

            bool questionsLeft = Current != null && Current.RequireAll
                ? found.Count < Current.Correct.Count
                : QuestionIndex < questions.Count - 1;
            bool canOffer = config.offerContinue && !continueUsed && questionsLeft && (CanOfferContinue == null || CanOfferContinue());
            if (canOffer)
            {
                State = QuizState.ContinueOffer;
                ContinueOffered?.Invoke();
            }
            else
            {
                flow = StartCoroutine(FinishRoutine(false));
            }
        }

        /// <summary>Oyuncu reklamı izledi: bir can geri verilir ve devam edilir.</summary>
        public void AcceptContinue()
        {
            if (State != QuizState.ContinueOffer) return;
            continueUsed = true;
            Mistakes = MaxMistakes - 1;
            ContinueGranted?.Invoke();
            if (Current != null && Current.RequireAll)
            {
                State = QuizState.Playing; // aynı "hepsini bul" sorusuna devam
                return;
            }
            NextQuestion();
        }

        /// <summary>Oyuncu devam etmek istemedi (ya da reklam tamamlanmadı).</summary>
        public void DeclineContinue()
        {
            if (State != QuizState.ContinueOffer) return;
            flow = StartCoroutine(FinishRoutine(false));
        }

        IEnumerator FinishRoutine(bool completed)
        {
            State = QuizState.Finished;
            Current?.Focus?.SetFocused(false);

            int stars = completed ? Mathf.Clamp(3 - Mistakes, 1, 3) : 0;
            int previousBest = progress.GetBest(ProgressId);
            bool newBest = progress.Record(ProgressId, stars, Score);

            sfx.Play(completed ? Sfx.Victory : Sfx.GameOver);
            yield return new WaitForSeconds(config.endDelay);

            bool regionAll = Mode == QuizMode.RegionAll && Current != null;
            if (regionAll)
                foreach (var t in Current.Correct)
                    if (!found.Contains(t) && !history.Exists(h => ReferenceEquals(h.Target, t))) Record(t, Current, false);
            int total = regionAll ? Current.Correct.Count : questions.Count;
            int correct = regionAll ? found.Count : CorrectCount;
            int answered = regionAll ? found.Count + Mistakes : Mathf.Min(QuestionIndex + (completed ? 0 : 1), questions.Count);
            Current = null;
            GameEnded?.Invoke(new GameResult(Level, completed, stars, Score, Mathf.Max(previousBest, Score), newBest && previousBest > 0,
                correct, Mathf.Max(answered, 1), total, BestStreak));
        }
    }
}
