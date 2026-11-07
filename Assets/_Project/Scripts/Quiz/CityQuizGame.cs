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
    /// Bölüm oyun akışı. Üç mod desteklenir:
    /// • İli Bul: hedef ili haritada tıkla.
    /// • Bölge Modu: haritada parlayan ilin coğrafi bölgesini seç.
    /// • Yer Şekli: adı verilen akarsu, dağ, volkan, ova ya da platoyu haritada bul.
    /// Her soru için süre akar. Doğru cevap puan + seri bonusu kazandırır ve sonraki soruda süre yenilenir.
    /// Yanlış cevap ya da süre bitimi bir hata sayılır, doğru cevap gösterilir ve sıradaki soruya geçilir.
    /// Hata sayısı sınıra ulaşınca (isteğe bağlı) reklamla devam teklifi gelir, kabul edilmezse oyuncu elenir.
    /// Yıldız: tamamlarsa 3 − hata (en az 1), elenirse 0.
    /// </summary>
    public class CityQuizGame : MonoBehaviour
    {
        /// <summary>Oyun alanının ekrandaki yeri (HUD'a yer bırakır).</summary>
        static readonly Rect FindViewport = new Rect(0.03f, 0.06f, 0.94f, 0.72f);
        static readonly Rect RegionViewport = new Rect(0.03f, 0.2f, 0.94f, 0.58f);

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

        readonly List<IMapTarget> questions = new List<IMapTarget>();
        Coroutine flow;
        bool continueUsed;

        public LevelDefinition Level { get; private set; }
        public QuizMode Mode => Level != null ? Level.Mode : QuizMode.FindProvince;
        public QuizState State { get; private set; } = QuizState.Idle;
        public bool IsPaused { get; private set; }
        public IMapTarget Current { get; private set; }
        public ProvinceView CurrentProvince => Current as ProvinceView;
        public int QuestionIndex { get; private set; }
        public int TotalQuestions => questions.Count;
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

        bool AcceptsAnswer => State == QuizState.Playing && !IsPaused;
        public bool AcceptsMapInput => AcceptsAnswer && Mode == QuizMode.FindProvince;
        public bool AcceptsRegionInput => AcceptsAnswer && Mode == QuizMode.Region;
        public bool AcceptsFeatureInput => AcceptsAnswer && Mode == QuizMode.Feature;
        public bool IsRunning => State != QuizState.Idle && State != QuizState.Finished;

        public float StreakMultiplier(int streak) =>
            1f + Mathf.Clamp(streak - 1, 0, config.maxStreakSteps) * config.streakStep;

        public void Initialize(QuizConfig quizConfig, TurkeyMap turkeyMap, FeatureLayer featureLayer, SfxPlayer sfxPlayer,
            MapCameraController camController, ProgressStore progressStore)
        {
            config = quizConfig;
            map = turkeyMap;
            features = featureLayer;
            sfx = sfxPlayer;
            cameraController = camController;
            progress = progressStore;
        }

        public void StartLevel(LevelDefinition level)
        {
            if (level == null || !level.IsPlayable) return;
            StopFlow();
            SetPaused(false);
            Level = level;
            flow = StartCoroutine(StartRoutine());
        }

        /// <summary>Oyunu yarıda bırakır ve haritayı menü görünümüne döndürür.</summary>
        public void Abort()
        {
            StopFlow();
            SetPaused(false);
            Current?.SetFocused(false);
            Current = null;
            State = QuizState.Idle;
            map.ResetAll();
            features.ShowDefault();
            cameraController.SetAttractMode();
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
            Rect focus;
            if (Mode == QuizMode.Feature)
            {
                map.SetFocus(_ => false); // iller sade zemin olsun, yer şekilleri öne çıksın
                features.ShowCategory(Level.category);
                focus = map.Bounds;
            }
            else
            {
                features.ShowDefault();
                focus = map.SetFocus(Level.HasRegionFilter ? p => Level.IncludesRegion(p.Data.region) : null);
            }
            cameraController.FocusOn(focus, Mode == QuizMode.Region ? RegionViewport : FindViewport);
            PickQuestions();

            TimeLimit = Mathf.Max(1f, Level.timePerQuestion);
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

            State = QuizState.Countdown;
            GameStarted?.Invoke();

            yield return new WaitForSeconds(0.45f); // kameranın yaklaşması için
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

        void PickQuestions()
        {
            questions.Clear();
            var pool = new List<IMapTarget>();
            if (Mode == QuizMode.Feature)
            {
                foreach (var f in features.Features)
                    if (FeatureLayer.Matches(Level.category, f.Category)) pool.Add(f);
            }
            else
            {
                foreach (var p in map.Provinces)
                    if (Level.IncludesRegion(p.Data.region)) pool.Add(p);
            }

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            int count = Mathf.Clamp(Level.questionCount, 1, Mathf.Max(1, pool.Count));
            for (int i = 0; i < count && i < pool.Count; i++) questions.Add(pool[i]);
        }

        void NextQuestion()
        {
            QuestionIndex++;
            if (QuestionIndex >= questions.Count)
            {
                flow = StartCoroutine(FinishRoutine(true));
                return;
            }

            Current = questions[QuestionIndex];
            if (Mode == QuizMode.Region) Current.SetFocused(true);
            TimeRemaining = TimeLimit;
            State = QuizState.Playing;
            QuestionStarted?.Invoke(new QuestionInfo(Current, Mode, QuestionIndex, questions.Count));
        }

        void Update()
        {
            if (State != QuizState.Playing || IsPaused) return;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                ResolveWrong(null, null, true);
            }
        }

        /// <summary>İli Bul: oyuncunun haritada tıkladığı il.</summary>
        public void SubmitProvince(ProvinceView clicked)
        {
            if (!AcceptsMapInput || clicked == null) return;
            SubmitTarget(clicked);
        }

        /// <summary>Yer Şekli: oyuncunun haritada tıkladığı öğe.</summary>
        public void SubmitFeature(FeatureView clicked)
        {
            if (!AcceptsFeatureInput || clicked == null) return;
            SubmitTarget(clicked);
        }

        void SubmitTarget(IMapTarget clicked)
        {
            if (ReferenceEquals(clicked, Current)) ResolveCorrect();
            else ResolveWrong(clicked, null, false);
        }

        /// <summary>Bölge Modu: oyuncunun seçtiği bölge.</summary>
        public void SubmitRegion(string region)
        {
            if (!AcceptsRegionInput || string.IsNullOrEmpty(region)) return;
            if (region == CurrentProvince.Data.region) ResolveCorrect(region);
            else ResolveWrong(null, region, false);
        }

        void ResolveCorrect(string chosenRegion = null)
        {
            Streak++;
            BestStreak = Mathf.Max(BestStreak, Streak);
            CorrectCount++;

            float multiplier = StreakMultiplier(Streak);
            int points = Mathf.RoundToInt((config.basePoints + config.maxTimeBonus * TimeRatio) * multiplier);
            Score += points;

            Current.SetFocused(false);
            Current.SetState(ProvinceState.Correct);
            Current.PlayCorrect();
            sfx.Play(Sfx.Correct);

            State = QuizState.Feedback;
            AnswerResolved?.Invoke(new AnswerResult(AnswerOutcome.Correct, Mode, Current, Current, chosenRegion,
                points, Streak, multiplier, Mistakes));
            float delay = config.correctFeedback + (Mode == QuizMode.Feature ? config.featureFeedbackBonus * 0.5f : 0f);
            flow = StartCoroutine(AfterFeedback(delay, false));
        }

        void ResolveWrong(IMapTarget clicked, string chosenRegion, bool timedOut)
        {
            Mistakes++;
            Streak = 0;

            clicked?.PlayWrongClick();
            float delay = config.wrongFeedback + (Mode == QuizMode.Feature ? config.featureFeedbackBonus : 0f);
            Current.SetFocused(false);
            Current.SetState(ProvinceState.Missed);
            Current.PlayReveal(delay);
            sfx.Play(Sfx.Wrong);
            cameraController.Shake();

            State = QuizState.Feedback;
            var outcome = timedOut ? AnswerOutcome.TimedOut : AnswerOutcome.Wrong;
            AnswerResolved?.Invoke(new AnswerResult(outcome, Mode, Current, clicked, chosenRegion, 0, 0, 1f, Mistakes));

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

            bool canOffer = config.offerContinue && !continueUsed && QuestionIndex < questions.Count - 1 &&
                            (CanOfferContinue == null || CanOfferContinue());
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

        /// <summary>Oyuncu reklamı izledi: bir can geri verilir ve sıradaki soruyla devam edilir.</summary>
        public void AcceptContinue()
        {
            if (State != QuizState.ContinueOffer) return;
            continueUsed = true;
            Mistakes = MaxMistakes - 1;
            ContinueGranted?.Invoke();
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
            Current = null;

            int stars = completed ? Mathf.Clamp(3 - Mistakes, 1, 3) : 0;
            int previousBest = progress.GetBest(Level.id);
            bool newBest = progress.Record(Level.id, stars, Score);

            sfx.Play(completed ? Sfx.Victory : Sfx.GameOver);
            yield return new WaitForSeconds(config.endDelay);

            int answered = Mathf.Min(QuestionIndex + (completed ? 0 : 1), questions.Count);
            GameEnded?.Invoke(new GameResult(Level, completed, stars, Score, Mathf.Max(previousBest, Score), newBest && previousBest > 0,
                CorrectCount, answered, questions.Count, BestStreak));
        }
    }
}
