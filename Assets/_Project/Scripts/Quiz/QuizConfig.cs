using System;
using UnityEngine;

namespace KPSSGeography.Quiz
{
    /// <summary>Oyun kuralları. GameBootstrap üzerinden Inspector'da değiştirilebilir.</summary>
    [Serializable]
    public class QuizConfig
    {
        [Header("Kurallar")]
        [Min(1)] public int questionCount = 20;
        [Min(1f)] public float timePerQuestion = 10f;
        [Min(1)] public int maxMistakes = 3;

        [Header("Puanlama")]
        public int basePoints = 100;
        [Tooltip("Kalan süreye göre eklenen en yüksek bonus.")]
        public int maxTimeBonus = 100;
        [Tooltip("Her art arda doğru cevapta çarpana eklenen değer.")]
        public float streakStep = 0.1f;
        [Tooltip("Seri çarpanının en fazla kaç adım artabileceği (5 => en fazla x1.5).")]
        public int maxStreakSteps = 5;

        [Header("Zamanlama (sn)")]
        public float countdownStep = 0.7f;
        public float correctFeedback = 0.7f;
        public float wrongFeedback = 1.8f;
        public float endDelay = 0.6f;
    }
}
