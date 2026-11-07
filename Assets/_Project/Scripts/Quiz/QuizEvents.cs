using KPSSGeography.Map;

namespace KPSSGeography.Quiz
{
    public enum QuizState
    {
        Idle,
        Countdown,
        Playing,
        Feedback,
        /// <summary>Canlar bitti; oyuncuya reklam izleyip devam etme teklif ediliyor.</summary>
        ContinueOffer,
        Finished
    }

    public enum AnswerOutcome
    {
        Correct,
        Wrong,
        TimedOut
    }

    public readonly struct QuestionInfo
    {
        public readonly IMapTarget Target;
        public readonly QuizMode Mode;
        public readonly int Index;
        public readonly int Total;

        public QuestionInfo(IMapTarget target, QuizMode mode, int index, int total)
        {
            Target = target;
            Mode = mode;
            Index = index;
            Total = total;
        }
    }

    public readonly struct AnswerResult
    {
        public readonly AnswerOutcome Outcome;
        public readonly QuizMode Mode;
        public readonly IMapTarget Target;
        /// <summary>Haritada tıklanan il ya da yer şekli (süre dolduysa / bölge modunda null).</summary>
        public readonly IMapTarget Clicked;
        /// <summary>Bölge modunda seçilen bölge (süre dolduysa null).</summary>
        public readonly string ChosenRegion;
        public readonly int Points;
        public readonly int Streak;
        public readonly float Multiplier;
        public readonly int Mistakes;

        public bool IsCorrect => Outcome == AnswerOutcome.Correct;
        public string CorrectRegion => (Target as ProvinceView)?.Data.region;
        /// <summary>Yer şekli sorularında KPSS bilgi notu.</summary>
        public string Info => (Target as FeatureView)?.Data.info;

        public AnswerResult(AnswerOutcome outcome, QuizMode mode, IMapTarget target, IMapTarget clicked, string chosenRegion,
            int points, int streak, float multiplier, int mistakes)
        {
            Outcome = outcome;
            Mode = mode;
            Target = target;
            Clicked = clicked;
            ChosenRegion = chosenRegion;
            Points = points;
            Streak = streak;
            Multiplier = multiplier;
            Mistakes = mistakes;
        }
    }

    public readonly struct GameResult
    {
        public readonly LevelDefinition Level;
        public readonly bool Completed;
        public readonly int Stars;
        public readonly int Score;
        public readonly int BestScore;
        public readonly bool IsNewBest;
        public readonly int Correct;
        public readonly int Answered;
        public readonly int Total;
        public readonly int BestStreak;

        public GameResult(LevelDefinition level, bool completed, int stars, int score, int bestScore, bool isNewBest,
            int correct, int answered, int total, int bestStreak)
        {
            Level = level;
            Completed = completed;
            Stars = stars;
            Score = score;
            BestScore = bestScore;
            IsNewBest = isNewBest;
            Correct = correct;
            Answered = answered;
            Total = total;
            BestStreak = bestStreak;
        }
    }
}
