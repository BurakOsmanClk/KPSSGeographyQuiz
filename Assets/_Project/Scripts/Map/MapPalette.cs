using System;
using KPSSGeography.Data;
using UnityEngine;

namespace KPSSGeography.Map
{
    /// <summary>Coğrafi bölgelerin adları (levels.json ve il verisiyle aynı yazım).</summary>
    public static class Regions
    {
        public const string Marmara = "Marmara";
        public const string Ege = "Ege";
        public const string Akdeniz = "Akdeniz";
        public const string IcAnadolu = "İç Anadolu";
        public const string Karadeniz = "Karadeniz";
        public const string DoguAnadolu = "Doğu Anadolu";
        public const string GuneydoguAnadolu = "Güneydoğu Anadolu";

        /// <summary>Arayüzde gösterim sırası (batıdan doğuya).</summary>
        public static readonly string[] All =
            { Marmara, Ege, Akdeniz, IcAnadolu, Karadeniz, DoguAnadolu, GuneydoguAnadolu };
    }

    /// <summary>Harita renkleri. GameBootstrap üzerinden Inspector'da değiştirilebilir.</summary>
    [Serializable]
    public class MapPalette
    {
        [Header("Deniz (gradyan)")]
        public Color seaTop = new Color32(0xBF, 0xE6, 0xF5, 0xFF);
        public Color seaBottom = new Color32(0x6C, 0xB4, 0xD8, 0xFF);

        [Header("Bölgeler (pastel)")]
        public Color marmara = new Color32(0xF6, 0xB8, 0xD1, 0xFF);
        public Color ege = new Color32(0xFF, 0xD3, 0xA0, 0xFF);
        public Color akdeniz = new Color32(0xFF, 0xA9, 0x9E, 0xFF);
        public Color icAnadolu = new Color32(0xFC, 0xE9, 0x9E, 0xFF);
        public Color karadeniz = new Color32(0xA8, 0xE2, 0x9E, 0xFF);
        public Color doguAnadolu = new Color32(0xC8, 0xB6, 0xF2, 0xFF);
        public Color guneydoguAnadolu = new Color32(0xF5, 0xBE, 0x6E, 0xFF);

        [Header("Fiziki harita")]
        public Color river = new Color32(0x1F, 0x78, 0xC8, 0xFF);
        public Color lake = new Color32(0x4D, 0x9F, 0xD6, 0xFF);
        public Color lakeShore = new Color32(0x2F, 0x7F, 0xB8, 0xFF);
        public Color mountain = new Color32(0x8B, 0x5A, 0x3C, 0xFF);
        public Color mountainMark = new Color32(0x5E, 0x3A, 0x22, 0xFF);
        public Color volcano = new Color32(0x6D, 0x45, 0x2C, 0xFF);
        public Color lava = new Color32(0xF0, 0x55, 0x2E, 0xFF);
        public Color plain = new Color32(0x72, 0xC4, 0x72, 0xE0);
        public Color plainEdge = new Color32(0x3E, 0x8E, 0x41, 0xFF);
        public Color plateau = new Color32(0xD9, 0xA5, 0x5B, 0xE0);
        public Color plateauEdge = new Color32(0xA0, 0x6F, 0x2E, 0xFF);
        public Color featureCorrect = new Color32(0x2E, 0xB8, 0x5C, 0xFF);
        public Color featureMissed = new Color32(0xE5, 0x48, 0x4D, 0xFF);

        [Header("Durumlar")]
        public Color border = new Color(1f, 1f, 1f, 0.9f);
        public Color hover = Color.white;
        public Color missed = new Color32(0xE0, 0x6C, 0x75, 0xFF);
        public Color dim = new Color32(0xD9, 0xD4, 0xC7, 0xFF);
        public Color correctFlash = Color.white;
        public Color wrongFlash = new Color32(0xD9, 0x3B, 0x2F, 0xFF);
        public Color reveal = new Color32(0xFF, 0xB7, 0x03, 0xFF);
        public Color shadow = new Color(0.02f, 0.12f, 0.22f, 0.25f);

        public Color RegionColor(string region) => region switch
        {
            Regions.Marmara => marmara,
            Regions.Ege => ege,
            Regions.Akdeniz => akdeniz,
            Regions.IcAnadolu => icAnadolu,
            Regions.Karadeniz => karadeniz,
            Regions.DoguAnadolu => doguAnadolu,
            Regions.GuneydoguAnadolu => guneydoguAnadolu,
            _ => dim
        };

        /// <summary>İlin temel rengi: bölge rengi + komşuların ayırt edilmesi için küçük ton farkı.</summary>
        public Color ProvinceBase(ProvinceData data)
        {
            Color c = RegionColor(data.region);
            float v = ((data.plate * 37) % 7) / 6f - 0.5f; // -0.5..0.5
            return v >= 0f ? Color.Lerp(c, Color.white, v * 0.28f) : Color.Lerp(c, Color.black, -v * 0.1f);
        }

        /// <summary>Doğru bilinen il: bölge renginin doygun ve koyu hali ("haritayı boyama" hissi).</summary>
        public static Color Saturated(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp01(s * 2.1f + 0.15f), v * 0.86f);
        }
    }
}
