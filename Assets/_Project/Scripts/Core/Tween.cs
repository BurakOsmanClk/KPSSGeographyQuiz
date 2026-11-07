using System;
using System.Collections;
using UnityEngine;

namespace KPSSGeography.Core
{
    /// <summary>Küçük, bağımlılıksız tween yardımcıları (coroutine tabanlı).</summary>
    public static class Tween
    {
        public static IEnumerator Run(float duration, Action<float> step)
        {
            float t = 0f;
            while (t < duration)
            {
                step(Mathf.Clamp01(t / duration));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            step(1f);
        }

        public static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

        public static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// <summary>0→1→0 arası sönümlü "punch" eğrisi.</summary>
        public static float Punch(float x) => Mathf.Sin(x * Mathf.PI) * (1f - x * 0.6f);
    }
}
