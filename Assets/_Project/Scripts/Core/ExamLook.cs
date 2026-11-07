using UnityEngine;

namespace KPSSGeography.Core
{
    /// <summary>
    /// Sınav görünümünün anlık yoğunluğu (0 = renkli ve etiketli normal harita, 1 = soluk gri, boş KPSS haritası).
    /// <see cref="ExamModeController"/> bu değeri animasyonla değiştirir; harita katmanları her karede okuyup
    /// kendi renklerini ona göre karıştırır, böylece geçiş "buharlaşma" gibi yumuşak olur.
    /// </summary>
    public static class ExamLook
    {
        /// <summary>0..1 geçiş değeri.</summary>
        public static float Blend;

        /// <summary>Görünüm yarıdan fazla sınav haritasına döndü mü? (ipuçlarını gizlemek için)</summary>
        public static bool Active => Blend > 0.5f;

        /// <summary>
        /// Rengi gri tona çeker: koyu renkler orta griye, açıklar açık griye gider (kontrast sıkışır, "baskı" hissi verir).
        /// </summary>
        public static Color Pale(Color c, float t)
        {
            if (t <= 0f) return c;
            float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            float g = 0.88f - (0.88f - lum) * 0.65f;
            return Color.Lerp(c, new Color(g, g, g, c.a), t);
        }
    }
}
