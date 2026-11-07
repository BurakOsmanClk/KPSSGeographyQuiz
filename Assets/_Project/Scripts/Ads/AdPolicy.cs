using System;
using KPSSGeography.Quiz;
using UnityEngine;

namespace KPSSGeography.Ads
{
    /// <summary>
    /// Reklam sıklığı kuralları: oturumun ilk bölümlerinde geçiş reklamı yok, iki geçiş reklamı arasında
    /// asgari süre var ve geçiş reklamı yalnızca sonuç ekranından ayrılırken (doğal mola anında) gösterilir.
    /// </summary>
    public class AdPolicy
    {
        readonly IAdService ads;
        readonly QuizConfig config;
        int levelsFinished;
        float lastInterstitialTime = float.NegativeInfinity;

        public IAdService Service => ads;

        public AdPolicy(IAdService adService, QuizConfig quizConfig)
        {
            ads = adService;
            config = quizConfig;
        }

        public bool CanOfferContinue => config.adsEnabled && config.offerContinue && ads != null && ads.IsRewardedReady;

        public void OnLevelFinished() => levelsFinished++;

        /// <summary>Sonuç ekranından ayrılırken uygunsa geçiş reklamı gösterir, sonra <paramref name="next"/>'i çalıştırır.</summary>
        public void BeforeLeavingResults(Action next)
        {
            bool show = config.adsEnabled && ads != null && ads.IsInterstitialReady &&
                        levelsFinished >= config.interstitialAfterLevels &&
                        Time.unscaledTime - lastInterstitialTime >= config.interstitialMinInterval;
            if (!show)
            {
                next?.Invoke();
                return;
            }
            lastInterstitialTime = Time.unscaledTime;
            ads.ShowInterstitial("level_end", next);
        }

        public void ShowRewarded(string placement, Action<bool> onComplete)
        {
            if (ads == null || !ads.IsRewardedReady)
            {
                onComplete?.Invoke(false);
                return;
            }
            ads.ShowRewarded(placement, onComplete);
        }
    }
}
