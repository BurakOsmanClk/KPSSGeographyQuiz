using System;

namespace KPSSGeography.Ads
{
    /// <summary>
    /// Oyunun reklam sağlayıcısından bağımsız arayüzü. Şu an <see cref="MockAdService"/> (test reklamı) kullanılıyor;
    /// Ads Mediation (LevelPlay) paketi kurulunca gerçek uygulama bu arayüzü uygulayacak, oyun kodu değişmeyecek.
    /// </summary>
    public interface IAdService
    {
        bool IsRewardedReady { get; }
        bool IsInterstitialReady { get; }

        /// <summary>Ödüllü reklam gösterir. <paramref name="onComplete"/>(true) = oyuncu ödülü hak etti.</summary>
        void ShowRewarded(string placement, Action<bool> onComplete);

        /// <summary>Geçiş reklamı gösterir; kapandığında (ya da gösterilemezse) <paramref name="onClosed"/> çağrılır.</summary>
        void ShowInterstitial(string placement, Action onClosed);
    }
}
