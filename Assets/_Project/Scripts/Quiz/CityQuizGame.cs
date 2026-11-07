using System;
using System.Collections;
using System.Collections.Generic;
using KPSSGeography.Core;
using KPSSGeography.Map;
using UnityEngine;

namespace KPSSGeography.Quiz
{
    /// <summary>
    /// "İli bul" oyun akışı:
    /// Her turda 81 il içinden rastgele N farklı il sorulur. Her soru için süre akar.
    /// Doğru cevapta puan + seri bonusu kazanılır ve süre yenilenir.
    /// Yanlış tıklama ya da süre bitimi bir hata sayılır; doğru il gösterilir ve sıradaki soruya geçilir.
    /// Hata sayısı sınıra ulaşınca oyuncu elenir.
    /// </summary>
    public class CityQuizGame : MonoBehaviour
    {
        const string BestScoreKey = "KPSS_CityQuiz_BestScore";

        public event Action GameStarted;
        public event Action<int> CountdownTick;
        public event Action<QuestionInfo> QuestionStarted;
        public event Action<AnswerResult> AnswerResolved;
        public event Action<GameResult> GameEnded;

        QuizConfig config;
        TurkeyMap map;
        SfxPlayer sfx;
        MapCameraController cameraController;

        readonly List<ProvinceView> questions = new List<ProvinceView>();
        Coroutine flow;

        public QuizState State { get; private set; } = QuizState.Idle;
        public ProvinceView Current { get; private set; }
        public int QuestionIndex { get; private set; }
        public int TotalQuestions => questions.Count;
        public float TimeRemaining { get; private set; }
        public float TimeLimit => config.timePerQuestion;
        public float TimeRatio => TimeLimit > 0f ? Mathf.Clamp01(TimeRemaining / TimeLimit) : 0f;
        public int Score { get; private set; }
        public int Mistakes { get; private set; }
        public int MaxMistakes => config.maxMistakes;
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int CorrectCount { get; private set; }
        public int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
        public bool AcceptsInput => State == QuizState.Playing;

        public float StreakMultiplier(int streak) =>
            1f + Mathf.Clamp(streak - 1, 0, config.maxStreakSteps) * config.streakStep;

        public void Initialize(QuizConfig quizConfig, TurkeyMap turkeyMap, SfxPlayer sfxPlayer, MapCameraController camController)
        {
            config = quizConfig;
            map = turkeyMap;
            sfx = sfxPlayer;
            cameraController = camController;
        }

        public void StartGame()
        {
            if (flow != null) StopCoroutine(flow);
            flow = StartCoroutine(StartRoutine());
        }

        IEnumerator StartRoutine()
        {
            map.ResetAll();
            cameraController.ResetView();
            PickQuestions();

            Score = 0;
            Mistakes = 0;
            Streak = 0;
            BestStreak = 0;
            CorrectCount = 0;
            QuestionIndex = -1;
            Current = null;
            TimeRemaining = TimeLimit;

            State = QuizState.Countdown;
            GameStarted?.Invoke();

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
            var pool = new List<ProvinceView>(map.Provinces);
            // Fisher–Yates karıştırma
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            int count = Mathf.Min(config.questionCount, pool.Count);
            for (int i = 0; i < count; i++) questions.Add(pool[i]);
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
            TimeRemaining = TimeLimit;
            State = QuizState.Playing;
            QuestionStarted?.Invoke(new QuestionInfo(Current, QuestionIndex, questions.Count));
        }

        void Update()
        {
            if (State != QuizState.Playing) return;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                ResolveWrong(null, true);
            }
        }

        /// <summary>Oyuncunun haritada tıkladığı il.</summary>
        public void SubmitAnswer(ProvinceView clicked)
        {
            if (State != QuizState.Playing || clicked == null) return;

            if (clicked == Current) ResolveCorrect();
            else ResolveWrong(clicked, false);
        }

        void ResolveCorrect()
        {
            Streak++;
            BestStreak = Mathf.Max(BestStreak, Streak);
            CorrectCount++;

            float multiplier = StreakMultiplier(Streak);
            int points = Mathf.RoundToInt((config.basePoints + config.maxTimeBonus * TimeRatio) * multiplier);
            Score += points;

            Current.SetState(ProvinceState.Correct);
            Current.PlayCorrect();
            sfx.Play(Sfx.Correct);

            State = QuizState.Feedback;
            AnswerResolved?.Invoke(new AnswerResult(AnswerOutcome.Correct, Current, Current, points, Streak, multiplier, Mistakes));
            flow = StartCoroutine(AfterFeedback(config.correctFeedback, false));
        }

        void ResolveWrong(ProvinceView clicked, bool timedOut)
        {
            Mistakes++;
            Streak = 0;

            if (clicked != null) clicked.PlayWrongClick();
            Current.SetState(ProvinceState.Missed);
            Current.PlayReveal(config.wrongFeedback);
            sfx.Play(Sfx.Wrong);
            cameraController.Shake();

            State = QuizState.Feedback;
            var outcome = timedOut ? AnswerOutcome.TimedOut : AnswerOutcome.Wrong;
            AnswerResolved?.Invoke(new AnswerResult(outcome, Current, clicked, 0, 0, 1f, Mistakes));

            bool eliminated = Mistakes >= config.maxMistakes;
            flow = StartCoroutine(AfterFeedback(config.wrongFeedback, eliminated));
        }

        IEnumerator AfterFeedback(float delay, bool eliminated)
        {
            yield return new WaitForSeconds(delay);
            if (eliminated) flow = StartCoroutine(FinishRoutine(false));
            else NextQuestion();
        }

        IEnumerator FinishRoutine(bool completed)
        {
            State = QuizState.Finished;
            Current = null;

            int previousBest = BestScore;
            bool newBest = Score > previousBest;
            if (newBest)
            {
                PlayerPrefs.SetInt(BestScoreKey, Score);
                PlayerPrefs.Save();
            }

            sfx.Play(completed ? Sfx.Victory : Sfx.GameOver);
            yield return new WaitForSeconds(config.endDelay);

            int answered = Mathf.Min(QuestionIndex + (completed ? 0 : 1), questions.Count);
            GameEnded?.Invoke(new GameResult(completed, Score, Mathf.Max(previousBest, Score), newBest,
                CorrectCount, answered, questions.Count, BestStreak));
        }
    }
}
