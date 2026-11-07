using UnityEngine;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Arayüzün tek renk kaynağı. Palet: lacivert (zemin), turkuaz/gök mavisi (deniz, ikincil),
    /// kehribar/turuncu (vurgu, birincil eylem). Tüm ekranlar bu değerleri kullanır.
    /// </summary>
    public static class Theme
    {
        public static readonly Color Navy = Hex(0x0B2239);
        public static readonly Color NavyPanel = Hex(0x0F2D4A, 0.94f);
        public static readonly Color NavyCard = Hex(0x15395C);
        public static readonly Color NavyLight = Hex(0x1F4B73);
        public static readonly Color Teal = Hex(0x219EBC);
        public static readonly Color Sky = Hex(0x8ECAE6);
        public static readonly Color Amber = Hex(0xFFB703);
        public static readonly Color Orange = Hex(0xFB8500);
        public static readonly Color Cream = Hex(0xFFF8EC);
        public static readonly Color Success = Hex(0x3BB273);
        public static readonly Color Error = Hex(0xE5484D);
        public static readonly Color Locked = Hex(0x4A5D70);
        public static readonly Color Soon = Hex(0x2B3A4A);

        public static readonly Color Text = Color.white;
        public static readonly Color TextMuted = new Color(1f, 1f, 1f, 0.68f);
        public static readonly Color TextFaint = new Color(1f, 1f, 1f, 0.38f);
        public static readonly Color Ink = Hex(0x0B2239);

        public static readonly Color Overlay = Hex(0x071827, 0.82f);
        public static readonly Color StarOn = Hex(0xFFC93C);
        public static readonly Color StarOff = new Color(1f, 1f, 1f, 0.18f);
        public static readonly Color HeartOn = Hex(0xF2545B);
        public static readonly Color HeartOff = new Color(1f, 1f, 1f, 0.16f);

        public static Color ModeColor(Quiz.QuizMode mode) => mode switch
        {
            Quiz.QuizMode.FindProvince => Teal,
            Quiz.QuizMode.Region => Orange,
            _ => Locked
        };

        public static Color Hex(int rgb, float alpha = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
    }
}
