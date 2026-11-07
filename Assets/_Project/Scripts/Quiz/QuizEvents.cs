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
        public readonly Question Question;
        public readonly QuizMode Mode;
        public readonly int Index;
        public readonly int Total;

        public QuestionInfo(Question question, QuizMode mode, int index, int total)
        {
            Question = question;
            Mode = mode;
            Index = index;
            Total = total;
        }
    }

    public readonly struct AnswerResult
    {
        public readonly AnswerOutcome Outcome;
        public readonly QuizMode Mode;
        public readonly Question Question;
        /// <summary>Haritada tıklanan il ya da yer şekli (butonlu sorularda / süre dolduysa null).</summary>
        public readonly IMapTarget Clicked;
        /// <summary>Seçilen buton (haritadan seçmeli sorularda -1).</summary>
        public readonly int ChosenIndex;
        public readonly int Points;
        public readonly int Streak;
        public readonly float Multiplier;
        public readonly int Mistakes;
        /// <summary>"Hepsini bul" sorularında ara cevap mı (soru henüz bitmedi)?</summary>
        public readonly bool Partial;
        public readonly int Found;

        public bool IsCorrect => Outcome == AnswerOutcome.Correct;
        public string Info => Question.Info;

        public AnswerResult(AnswerOutcome outcome, QuizMode mode, Question question, IMapTarget clicked, int chosenIndex,
            int points, int streak, float multiplier, int mistakes, bool partial = false, int found = 0)
        {
            Outcome = outcome;
            Mode = mode;
            Question = question;
            Clicked = clicked;
            ChosenIndex = chosenIndex;
            Points = points;
            Streak = streak;
            Multiplier = multiplier;
            Mistakes = mistakes;
            Partial = partial;
            Found = found;
        }
    }

    /// <summary>"Haritayı incele" için turda sorulan bir yer ve sonucu.</summary>
    public readonly struct ReviewItem
    {
        public readonly IMapTarget Target;
        public readonly string Label;
        public readonly string Info;
        public readonly bool Correct;

        public ReviewItem(IMapTarget target, string label, string info, bool correct)
        {
            Target = target;
            Label = label;
            Info = info;
            Correct = correct;
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
