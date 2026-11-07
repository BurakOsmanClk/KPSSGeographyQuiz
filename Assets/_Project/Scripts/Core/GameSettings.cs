using System;
using UnityEngine;

namespace KPSSGeography.Core
{
    /// <summary>Oyuncu tercihleri (PlayerPrefs). Değişince <see cref="Changed"/> yayınlanır.</summary>
    public static class GameSettings
    {
        const string ColorblindKey = "KPSS_Colorblind";
        const string BigTextKey = "KPSS_BigText";
        const string EffectsKey = "KPSS_Effects";

        public static event Action Changed;

        /// <summary>Doğru/yanlış için yeşil-kırmızı yerine mavi-turuncu (Okabe–Ito) palet.</summary>
        public static bool Colorblind
        {
            get => PlayerPrefs.GetInt(ColorblindKey, 0) == 1;
            set => Set(ColorblindKey, value);
        }

        /// <summary>Arayüz yazıları %15 daha büyük.</summary>
        public static bool BigText
        {
            get => PlayerPrefs.GetInt(BigTextKey, 0) == 1;
            set => Set(BigTextKey, value);
        }

        /// <summary>Işıltı, bölge sınırı çizimi, kenar parlaması gibi görsel efektler.</summary>
        public static bool Effects
        {
            get => PlayerPrefs.GetInt(EffectsKey, 1) == 1;
            set => Set(EffectsKey, value);
        }

        public static float TextScale => BigText ? 1.15f : 1f;

        static void Set(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
