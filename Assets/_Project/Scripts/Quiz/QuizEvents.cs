using KPSSGeography.Map;

namespace KPSSGeography.Quiz
{
    public enum QuizState
    {
        Idle,
        Countdown,
        Playing,
        Feedback,
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
        public readonly ProvinceView Target;
        public readonly int Index;
        public readonly int Total;

        public QuestionInfo(ProvinceView target, int index, int total)
        {
            Target = target;
            Index = index;
            Total = total;
        }
    }

    public readonly struct AnswerResult
    {
        public readonly AnswerOutcome Outcome;
        public readonly ProvinceView Target;
        public readonly ProvinceView Clicked;
        public readonly int Points;
        public readonly int Streak;
        public readonly float Multiplier;
        public readonly int Mistakes;

        public bool IsCorrect => Outcome == AnswerOutcome.Correct;

        public AnswerResult(AnswerOutcome outcome, ProvinceView target, ProvinceView clicked, int points, int streak, float multiplier, int mistakes)
        {
            Outcome = outcome;
            Target = target;
            Clicked = clicked;
            Points = points;
            Streak = streak;
            Multiplier = multiplier;
            Mistakes = mistakes;
        }
    }

    public readonly struct GameResult
    {
        public readonly bool Completed;
        public readonly int Score;
        public readonly int BestScore;
        public readonly bool IsNewBest;
        public readonly int Correct;
        public readonly int Answered;
        public readonly int Total;
        public readonly int BestStreak;

        public GameResult(bool completed, int score, int bestScore, bool isNewBest, int correct, int answered, int total, int bestStreak)
        {
            Completed = completed;
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
