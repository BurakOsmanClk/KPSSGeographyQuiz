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
                    size = t.fontSize;
                    baseSizes[t] = size;
                }
                t.fontSize = Mathf.RoundToInt(size * k);
            }
        }
    }
}
