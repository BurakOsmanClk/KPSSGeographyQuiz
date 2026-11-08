using System.Collections.Generic;
using KPSSGeography.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>"Büyük yazı" ayarı: kök altındaki tüm Text'lerin boyutunu ilk görüldükleri boyuta göre ölçekler.</summary>
    public static class TextScaler
    {
        static readonly Dictionary<Text, int> baseSizes = new Dictionary<Text, int>();

        public static void Apply(Transform root)
        {
            float k = GameSettings.TextScale;
            foreach (var t in root.GetComponentsInChildren<Text>(true))
            {
                if (!baseSizes.TryGetValue(t, out int size))
                {
                    size = t.resizeTextForBestFit ? t.resizeTextMaxSize : t.fontSize;
                    baseSizes[t] = size;
                }
                int s = Mathf.RoundToInt(size * k);
                t.fontSize = s;
                if (t.resizeTextForBestFit)
                {
                    // Sığdırma açık yazılarda üst sınır büyür; kutuya sığmazsa yine küçülür (taşmaz)
                    float minScale = size > 0 ? Mathf.Clamp01(t.resizeTextMinSize / (float)Mathf.Max(1, t.resizeTextMaxSize)) : 0.55f;
                    t.resizeTextMaxSize = s;
                    t.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(s * minScale));
                }
            }
        }
    }
}
