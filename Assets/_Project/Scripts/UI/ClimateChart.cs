using KPSSGeography.Data;
using UnityEngine;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// KPSS tipi iklim grafiği: aylık ortalama yağış sütunları (sol eksen, mm) ve sıcaklık çizgisi (sağ eksen, °C).
    /// Verilen RectTransform'un içine kodla çizilir; her gösterimde baştan kurulur.
    /// </summary>
    public class ClimateChart : MonoBehaviour
    {
        static readonly string[] Months = { "O", "Ş", "M", "N", "M", "H", "T", "A", "E", "E", "K", "A" };
        static readonly Color Ink = Theme.Hex(0x1B2A3A);
        static readonly Color Grid = Theme.Hex(0x1B2A3A, 0.12f);
        static readonly Color TempColor = Theme.Hex(0xD62828);

        const float PadLeft = 72f, PadRight = 72f, PadTop = 64f, PadBottom = 104f;

        RectTransform area;

        public void Show(RectTransform target, ClimateCity city)
        {
            if (area != null) Destroy(area.gameObject);
            area = UIFactory.Rect("Chart", target).Stretch();
            if (city == null || city.temp == null || city.rain == null || city.temp.Length < 12 || city.rain.Length < 12) return;

            Canvas.ForceUpdateCanvases();
            float w = target.rect.width, h = target.rect.height;
            float pw = w - PadLeft - PadRight, ph = h - PadTop - PadBottom;

            // Eksen aralıkları
            float maxRain = 0f;
            float minT = 0f, maxT = 0f;
            for (int i = 0; i < 12; i++)
            {
                maxRain = Mathf.Max(maxRain, city.rain[i]);
                minT = Mathf.Min(minT, city.temp[i]);
                maxT = Mathf.Max(maxT, city.temp[i]);
            }
            float rainTop = Mathf.Max(50f, Mathf.Ceil(maxRain / 50f) * 50f);
            float tLow = Mathf.Floor(minT / 10f) * 10f;
            float tHigh = Mathf.Max(tLow + 20f, Mathf.Ceil(maxT / 10f) * 10f);

            Vector2 Plot(float x01, float y01) => new Vector2(PadLeft + x01 * pw, PadBottom + y01 * ph);

            // Başlıklar
            Text("RainTitle", "Yağış (mm)", 20, TextAnchor.MiddleLeft, new Vector2(PadLeft - 40, h - 30), new Vector2(200, 30), Ink, FontStyle.Bold);
            Text("TempTitle", "Sıcaklık (°C)", 20, TextAnchor.MiddleRight, new Vector2(w - 212, h - 30), new Vector2(200, 30), TempColor, FontStyle.Bold);

            // Yağış ekseni ızgarası ve etiketleri
            int rainSteps = Mathf.RoundToInt(rainTop / 50f);
            float rainStep = 50f;
            if (rainSteps > 6) { rainStep = 100f; rainSteps = Mathf.CeilToInt(rainTop / 100f); rainTop = rainSteps * 100f; }
            for (int i = 0; i <= rainSteps; i++)
            {
                float y = i / (float)rainSteps;
                Rect("Grid", Plot(0f, y) - new Vector2(0f, 1f), new Vector2(pw, 2f), i == 0 ? Ink : Grid);
                Text("RainTick", (i * rainStep).ToString("0"), 18, TextAnchor.MiddleRight, Plot(0f, y) + new Vector2(-66, -12), new Vector2(58, 24), Ink);
            }
            // Sıcaklık ekseni etiketleri (sağda)
            for (float t = tLow; t <= tHigh + 0.01f; t += 10f)
            {
                float y = (t - tLow) / (tHigh - tLow);
                Text("TempTick", t.ToString("0"), 18, TextAnchor.MiddleLeft, Plot(1f, y) + new Vector2(8, -12), new Vector2(58, 24), TempColor);
                if (Mathf.Approximately(t, 0f) && tLow < 0f)
                    Rect("Zero", Plot(0f, y) - new Vector2(0f, 1f), new Vector2(pw, 2f), Theme.Hex(0xD62828, 0.35f));
            }
            // Eksen çizgileri
            Rect("AxisL", Plot(0f, 0f) - new Vector2(1f, 0f), new Vector2(2f, ph), Ink);
            Rect("AxisR", Plot(1f, 0f) - new Vector2(1f, 0f), new Vector2(2f, ph), Ink);

            // Yağış sütunları ve ay adları
            float slot = pw / 12f;
            for (int i = 0; i < 12; i++)
            {
                float bh = Mathf.Clamp01(city.rain[i] / rainTop) * ph;
                Rect("Bar", new Vector2(PadLeft + slot * i + slot * 0.2f, PadBottom), new Vector2(slot * 0.6f, Mathf.Max(1f, bh)), Ink);
                Text("Month", Months[i], 18, TextAnchor.MiddleCenter, new Vector2(PadLeft + slot * i, PadBottom - 30), new Vector2(slot, 26), Ink, FontStyle.Bold);
            }

            // Sıcaklık çizgisi
            Vector2 prev = Vector2.zero;
            for (int i = 0; i < 12; i++)
            {
                var p = Plot((i + 0.5f) / 12f, (city.temp[i] - tLow) / (tHigh - tLow));
                if (i > 0) Segment(prev, p, 4f, TempColor);
                prev = p;
            }
            for (int i = 0; i < 12; i++)
            {
                var p = Plot((i + 0.5f) / 12f, (city.temp[i] - tLow) / (tHigh - tLow));
                var dot = UIFactory.Icon("Dot", area, ShapeSprites.Circle, TempColor);
                dot.rectTransform.Place(Vector2.zero, new Vector2(0.5f, 0.5f), p, new Vector2(12, 12));
            }

            // Lejant
            float ly = 26f;
            Rect("LegendBar", new Vector2(PadLeft + 10, ly - 9), new Vector2(34, 18), Ink);
            Text("LegendRain", "Yağış", 19, TextAnchor.MiddleLeft, new Vector2(PadLeft + 52, ly - 14), new Vector2(120, 28), Ink);
            Segment(new Vector2(PadLeft + 200, ly), new Vector2(PadLeft + 240, ly), 4f, TempColor);
            Text("LegendTemp", "Sıcaklık", 19, TextAnchor.MiddleLeft, new Vector2(PadLeft + 250, ly - 14), new Vector2(140, 28), TempColor);
        }

        public void Clear()
        {
            if (area != null) Destroy(area.gameObject);
            area = null;
        }

        void Rect(string name, Vector2 pos, Vector2 size, Color color)
        {
            var img = UIFactory.Solid(name, area, color);
            img.rectTransform.Place(Vector2.zero, Vector2.zero, pos, size);
        }

        void Text(string name, string text, int size, TextAnchor anchor, Vector2 pos, Vector2 box, Color color, FontStyle style = FontStyle.Normal)
        {
            var t = UIFactory.Label(name, area, text, size, color, anchor, style);
            t.rectTransform.Place(Vector2.zero, Vector2.zero, pos, box);
        }

        void Segment(Vector2 a, Vector2 b, float thickness, Color color)
        {
            var img = UIFactory.Solid("Line", area, color);
            var rt = img.rectTransform;
            Vector2 d = b - a;
            rt.Place(Vector2.zero, new Vector2(0f, 0.5f), a, new Vector2(d.magnitude, thickness));
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
    }
}
