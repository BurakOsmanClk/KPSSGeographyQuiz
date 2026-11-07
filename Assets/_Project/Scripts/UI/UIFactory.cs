using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KPSSGeography.UI
{
    /// <summary>
    /// Kodla uGUI elemanı oluşturma yardımcıları. Prototipte harici görsel gerekmemesi için
    /// yuvarlak köşeli panel ve kalp ikonları çalışma anında üretilir.
    /// </summary>
    public static class UIFactory
    {
        static Font font;
        static Sprite rounded;
        static Sprite heart;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Sprite Rounded
        {
            get
            {
                if (rounded == null) rounded = CreateRoundedSprite(64, 20);
                return rounded;
            }
        }

        public static Sprite Heart
        {
            get
            {
                if (heart == null) heart = CreateHeartSprite(96);
                return heart;
            }
        }

        // ---------- Eleman oluşturma ----------

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Solid(string name, Transform parent, Color color, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Icon(string name, Transform parent, Sprite sprite, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, bool shadow = false)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.35f);
                s.effectDistance = new Vector2(0f, -3f);
            }
            return t;
        }

        /// <summary>Yuvarlak köşeli, gölgeli, üzerine gelince büyüyen buton.</summary>
        public static Button Button(string name, Transform parent, string label, Color color, Vector2 size, UnityAction onClick,
            Color? textColor = null, int fontSize = 0)
        {
            var img = Panel(name, parent, Color.white, true);
            img.rectTransform.sizeDelta = size;

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            SetButtonColor(btn, color);
            if (onClick != null) btn.onClick.AddListener(onClick);

            var shadow = img.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -5f);

            int fs = fontSize > 0 ? fontSize : Mathf.RoundToInt(size.y * 0.4f);
            var text = Label("Label", img.transform, label, fs, textColor ?? Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.Stretch();

            img.gameObject.AddComponent<ButtonHoverScale>();
            return btn;
        }

        /// <summary>Daire biçiminde ikon butonu.</summary>
        public static Button IconButton(string name, Transform parent, Sprite icon, Color color, float size, UnityAction onClick,
            Color? iconColor = null)
        {
            var rt = Rect(name, parent);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ShapeSprites.Circle;
            img.raycastTarget = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            SetButtonColor(btn, color);
            if (onClick != null) btn.onClick.AddListener(onClick);

            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -4f);

            var ic = Icon("Icon", rt, icon, iconColor ?? Color.white);
            ic.rectTransform.Stretch(size * 0.26f, size * 0.26f, size * 0.26f, size * 0.26f);
            rt.gameObject.AddComponent<ButtonHoverScale>();
            return btn;
        }

        public static void SetButtonColor(Button btn, Color color)
        {
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = color;
            colors.disabledColor = new Color(0.36f, 0.42f, 0.5f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
        }

        public static void SetButtonLabel(Button btn, string text)
        {
            var t = btn.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        /// <summary>Belirli bir boyutta, gölgeli kart paneli.</summary>
        public static Image Card(string name, Transform parent, Color color, bool raycast = false)
        {
            var img = Panel(name, parent, color, raycast);
            var sh = img.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.3f);
            sh.effectDistance = new Vector2(0f, -8f);
            return img;
        }

        // ---------- Prosedürel sprite'lar ----------

        static Sprite CreateRoundedSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UI_Rounded",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        static Sprite CreateHeartSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UI_Heart",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            const int ss = 4; // kenar yumuşatma için alt örnekleme
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                {
                    float u = ((x + (sx + 0.5f) / ss) / size - 0.5f) * 2.6f;
                    float v = ((y + (sy + 0.5f) / ss) / size) * 2.6f - 1.25f;
                    float a = u * u + v * v - 1f;
                    if (a * a * a - u * u * v * v * v <= 0f) hits++;
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (ss * ss)));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
