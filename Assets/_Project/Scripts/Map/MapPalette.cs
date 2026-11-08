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
        public Color mountainLight = new Color32(0xC9, 0xA2, 0x7E, 0xEB);
        public Color country = new Color32(0xD5, 0xD9, 0xDD, 0xFF);
        public Color countryEdge = new Color32(0xB3, 0xBA, 0xC1, 0xFF);
        public Color mountainMark = new Color32(0x5E, 0x3A, 0x22, 0xFF);
        public Color volcano = new Color32(0x6D, 0x45, 0x2C, 0xFF);
        public Color lava = new Color32(0xF0, 0x55, 0x2E, 0xFF);
        /// <summary>Masif arazi işareti: eski, başkalaşmış kaya (koyu taş moru).</summary>
        public Color massif = new Color32(0x6E, 0x5A, 0x8C, 0xFF);
        public Color massifLight = new Color32(0xB9, 0xA8, 0xD4, 0xFF);
        public Color plain = new Color32(0x72, 0xC4, 0x72, 0xE0);
        public Color plainEdge = new Color32(0x3E, 0x8E, 0x41, 0xFF);
        public Color plateau = new Color32(0xD9, 0xA5, 0x5B, 0xE0);
        public Color plateauEdge = new Color32(0xA0, 0x6F, 0x2E, 0xFF);
        public Color dam = new Color32(0x1F, 0x5F, 0xA8, 0xFF);
        public Color strait = new Color32(0x0B, 0x4F, 0x8A, 0xFF);
        public Color gulf = new Color32(0x11, 0x9D, 0xA4, 0xFF);
        public Color peninsula = new Color32(0xE9, 0xA2, 0x3B, 0xFF);
        public Color island = new Color32(0x3E, 0xA6, 0x5A, 0xFF);
        public Color cape = new Color32(0xE0, 0x5A, 0x3A, 0xFF);
        public Color unesco = new Color32(0xF2, 0xB1, 0x1A, 0xFF);
        public Color park = new Color32(0x2E, 0x8B, 0x57, 0xFF);
        public Color fault = new Color32(0xD6, 0x28, 0x28, 0xFF);
        public Color markerEdge = new Color32(0x0B, 0x22, 0x39, 0xFF);
        public Color featureCorrect = new Color32(0x2E, 0xB8, 0x5C, 0xFF);
        public Color featureMissed = new Color32(0xE5, 0x48, 0x4D, 0xFF);

        [Header("Komşu ülkeler ve sınır kapıları")]
        public Color countryGreece = new Color32(0xA9, 0xC8, 0xEC, 0xFF);
        public Color countryBulgaria = new Color32(0xA8, 0xDC, 0xC8, 0xFF);
        public Color countryGeorgia = new Color32(0xF4, 0xB6, 0xB0, 0xFF);
        public Color countryArmenia = new Color32(0xD0, 0xC2, 0xEE, 0xFF);
        public Color countryAzerbaijan = new Color32(0x9F, 0xD5, 0xD9, 0xFF);
        public Color countryIran = new Color32(0xEB, 0xD3, 0x9C, 0xFF);
        public Color countryIraq = new Color32(0xF2, 0xC2, 0x9C, 0xFF);
        public Color countrySyria = new Color32(0xC5, 0xDD, 0xA0, 0xFF);
        public Color countryBorder = new Color(1f, 1f, 1f, 0.9f);
        /// <summary>Bariyerin kırmızı şeritleri (kırmızı-beyaz hudut bariyeri).</summary>
        public Color gateRed = new Color32(0xD6, 0x28, 0x28, 0xFF);
        public Color gatePost = new Color32(0x3A, 0x44, 0x50, 0xFF);

        [Header("Dış hat (il sınırları gizliyken)")]
        public Color outline = new Color32(0x6F, 0x78, 0x84, 0xE6);

        [Header("Sınav modu (soluk, boş KPSS haritası)")]
        public Color examSea = new Color32(0xE6, 0xE9, 0xED, 0xFF);
        public Color examSeaTop = new Color32(0xF3, 0xF4, 0xF6, 0xFF);
        public Color examLand = new Color32(0xCF, 0xD3, 0xD8, 0xFF);
        public Color examDim = new Color32(0xE9, 0xEB, 0xEE, 0xFF);
        public Color examBorder = new Color32(0x69, 0x70, 0x7A, 0xFF);
        public Color examShadow = new Color(0.1f, 0.12f, 0.15f, 0.1f);
        public Color examCountry = new Color32(0xF7, 0xF8, 0xF9, 0xFF);
        public Color examCountryEdge = new Color32(0xC3, 0xC8, 0xCF, 0xFF);
        public Color examLake = new Color32(0xDC, 0xE3, 0xEA, 0xFF);
        public Color examLakeEdge = new Color32(0x86, 0x90, 0x9B, 0xFF);

        [Header("Durumlar")]
        public Color border = new Color(1f, 1f, 1f, 0.9f);
        public Color hover = Color.white;
        public Color missed = new Color32(0xE0, 0x6C, 0x75, 0xFF);
        public Color dim = new Color32(0xD9, 0xD4, 0xC7, 0xFF);
        public Color correctFlash = Color.white;
        public Color wrongFlash = new Color32(0xD9, 0x3B, 0x2F, 0xFF);
        public Color reveal = new Color32(0xFF, 0xB7, 0x03, 0xFF);
        public Color shadow = new Color(0.02f, 0.12f, 0.22f, 0.25f);

        Color[] defaults;

        /// <summary>Renk körü dostu durum renkleri (doğru = mavi, yanlış = turuncu).</summary>
        public void ApplyColorblind(bool on)
        {
            if (defaults == null) defaults = new[] { missed, featureCorrect, featureMissed, wrongFlash };
            missed = on ? new Color32(0xE6, 0x9F, 0x00, 0xFF) : defaults[0];
            featureCorrect = on ? new Color32(0x00, 0x72, 0xB2, 0xFF) : defaults[1];
            featureMissed = on ? new Color32(0xE6, 0x9F, 0x00, 0xFF) : defaults[2];
            wrongFlash = on ? new Color32(0xD5, 0x5E, 0x00, 0xFF) : defaults[3];
        }

        /// <summary>Komşu ülkenin pastel rengi (illerdeki bölge renkleri gibi).</summary>
        public Color CountryColor(string country) => country switch
        {
            "Yunanistan" => countryGreece,
            "Bulgaristan" => countryBulgaria,
            "Gürcistan" => countryGeorgia,
            "Ermenistan" => countryArmenia,
            "Azerbaycan" => countryAzerbaijan,
            "İran" => countryIran,
            "Irak" => countryIraq,
            "Suriye" => countrySyria,
            _ => this.country
        };

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
