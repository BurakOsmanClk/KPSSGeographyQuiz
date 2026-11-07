using System;
using UnityEngine;

namespace KPSSGeography.UI
{
    /// <summary>
    /// İkonları işaretli mesafe fonksiyonlarıyla (SDF) çalışma anında üretir: yıldız, kilit, ok, tik, dişli,
    /// duraklat, daire ve dikey gradyan. Harici görsel gerekmez, kenarlar yumuşatılmıştır.
    /// </summary>
    public static class ShapeSprites
    {
        static Sprite star, lockIcon, chevronRight, chevronLeft, check, gear, pause, circle, gradient;

        public static Sprite Star => star != null ? star : star = Render("Star", 128, StarSdf);
        public static Sprite Lock => lockIcon != null ? lockIcon : lockIcon = Render("Lock", 128, LockSdf);
        public static Sprite ChevronRight => chevronRight != null ? chevronRight : chevronRight = Render("ChevronR", 96, p => ChevronSdf(p));
        public static Sprite ChevronLeft => chevronLeft != null ? chevronLeft : chevronLeft = Render("ChevronL", 96, p => ChevronSdf(new Vector2(-p.x, p.y)));
        public static Sprite Check => check != null ? check : check = Render("Check", 96, CheckSdf);
        public static Sprite Gear => gear != null ? gear : gear = Render("Gear", 128, GearSdf);
        public static Sprite Pause => pause != null ? pause : pause = Render("Pause", 96, PauseSdf);
        public static Sprite Circle => circle != null ? circle : circle = Render("Circle", 128, p => p.magnitude - 0.94f);

        /// <summary>Altta opak, üstte saydam beyaz gradyan (Image.color ile renklendirilir).</summary>
        public static Sprite GradientUp
        {
            get
            {
                if (gradient != null) return gradient;
                const int h = 256;
                var tex = new Texture2D(4, h, TextureFormat.RGBA32, false)
                {
                    name = "UI_GradientUp", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
                };
                var px = new Color32[4 * h];
                for (int y = 0; y < h; y++)
                {
                    float t = y / (h - 1f);
                    byte a = (byte)(Mathf.Clamp01(1f - t * t * (3f - 2f * t)) * 255);
                    for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                return gradient = Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        // ---------------- Üretim ----------------

        static Sprite Render(string name, int size, Func<Vector2, float> sdf)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UI_" + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var px = new Color32[size * size];
            float pixelsPerUnit = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                float a = Mathf.Clamp01(0.5f - sdf(p) * pixelsPerUnit);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------------- SDF'ler ----------------

        static readonly Vector2[] StarPoints = BuildStar();

        static Vector2[] BuildStar()
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? 0.95f : 0.42f;
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r - 0.05f);
            }
            return pts;
        }

        static float StarSdf(Vector2 p) => PolygonSdf(p, StarPoints) - 0.04f;

        static float LockSdf(Vector2 p)
        {
            float body = BoxSdf(p, new Vector2(0f, -0.3f), new Vector2(0.62f, 0.46f), 0.14f);
            float hole = (p - new Vector2(0f, -0.26f)).magnitude - 0.12f;
            body = Mathf.Max(body, -hole);
            var c = new Vector2(0f, 0.25f);
            float shackle = p.y >= c.y
                ? Mathf.Abs((p - c).magnitude - 0.38f) - 0.1f
                : Mathf.Min(SegmentSdf(p, new Vector2(-0.38f, c.y), new Vector2(-0.38f, 0f)),
                            SegmentSdf(p, new Vector2(0.38f, c.y), new Vector2(0.38f, 0f))) - 0.1f;
            return Mathf.Min(body, shackle);
        }

        static float ChevronSdf(Vector2 p) =>
            Mathf.Min(SegmentSdf(p, new Vector2(-0.22f, 0.55f), new Vector2(0.3f, 0f)),
                      SegmentSdf(p, new Vector2(0.3f, 0f), new Vector2(-0.22f, -0.55f))) - 0.13f;

        static float CheckSdf(Vector2 p) =>
            Mathf.Min(SegmentSdf(p, new Vector2(-0.55f, 0.02f), new Vector2(-0.15f, -0.4f)),
                      SegmentSdf(p, new Vector2(-0.15f, -0.4f), new Vector2(0.6f, 0.45f))) - 0.14f;

        static float PauseSdf(Vector2 p) =>
            Mathf.Min(BoxSdf(p, new Vector2(-0.3f, 0f), new Vector2(0.15f, 0.6f), 0.06f),
                      BoxSdf(p, new Vector2(0.3f, 0f), new Vector2(0.15f, 0.6f), 0.06f));

        static float GearSdf(Vector2 p)
        {
            float r = p.magnitude;
            float ang = Mathf.Atan2(p.y, p.x);
            float teeth = Mathf.Clamp01((Mathf.Cos(ang * 8f) + 0.15f) * 2.5f);
            float outer = r - (0.66f + 0.2f * teeth);
            float inner = -(r - 0.27f);
            return Mathf.Max(outer * 0.8f, inner);
        }

        static float SegmentSdf(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        static float BoxSdf(Vector2 p, Vector2 center, Vector2 half, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half + new Vector2(radius, radius);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        static float PolygonSdf(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }
    }
}
