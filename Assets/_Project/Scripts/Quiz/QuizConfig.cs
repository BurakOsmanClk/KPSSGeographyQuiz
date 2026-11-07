using System;
using UnityEngine;

namespace KPSSGeography.Quiz
{
    /// <summary>
    /// Bölümlerden bağımsız genel oyun ayarları (puanlama, zamanlama).
    /// Soru sayısı, süre ve hata hakkı her bölüm için levels.json'da tanımlanır.
    /// </summary>
    [Serializable]
    public class QuizConfig
    {
        [Header("Puanlama")]
        public int basePoints = 100;
        [Tooltip("Kalan süreye göre eklenen en yüksek bonus.")]
        public int maxTimeBonus = 100;
        [Tooltip("Her art arda doğru cevapta çarpana eklenen değer.")]
        public float streakStep = 0.1f;
        [Tooltip("Seri çarpanının en fazla kaç adım artabileceği (5 => en fazla x1.5).")]
        public int maxStreakSteps = 5;

        [Header("Zamanlama (sn)")]
        public float countdownStep = 0.6f;
        public float correctFeedback = 0.7f;
        public float wrongFeedback = 1.8f;
        public float endDelay = 0.7f;
        [Tooltip("Yer şekli sorularında yanlış cevaptan sonra bilgi notunu okumak için ek süre.")]
        public float featureFeedbackBonus = 1.2f;

        [Header("Reklam")]
        public bool adsEnabled = true;
        [Tooltip("Elenince ödüllü reklamla +1 can ile devam teklifi (bölüm başına 1 kez).")]
        public bool offerContinue = true;
        [Tooltip("Oturumda kaç bölüm bittikten sonra geçiş reklamı gösterilmeye başlansın.")]
        public int interstitialAfterLevels = 2;
        [Tooltip("İki geçiş reklamı arasında en az kaç saniye olsun.")]
        public float interstitialMinInterval = 180f;

        [Header("Test")]
        [Tooltip("Açıkken tüm bölümler kilitsiz görünür (geliştirme için).")]
        public bool unlockAllLevels;
    }
}
