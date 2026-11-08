using System;
using UnityEngine;

namespace KPSSGeography.Data
{
    /// <summary>
    /// Fiziki harita öğesi: akarsu, dağ, volkan, ova ya da plato.
    /// Veriler Tools/geo/build_features.py ile üretilir (harita koordinatlarında, illerle aynı projeksiyon).
    /// </summary>
    [Serializable]
    public class GeoFeature
    {
        public const string River = "river";
        public const string Mountain = "mountain";
        public const string Volcano = "volcano";
        public const string Plain = "plain";
        public const string Plateau = "plateau";
        public const string Lake = "lake";
        public const string Dam = "dam";
        public const string Gulf = "gulf";
        public const string Peninsula = "peninsula";
        public const string Unesco = "unesco";
        public const string Park = "park";
        public const string Fault = "fault";
        /// <summary>Kara sınırı olan komşu ülke (alan). subtype = ülke adı.</summary>
        public const string Country = "country";
        /// <summary>Kara hudut kapısı (nokta). subtype = açıldığı ülke.</summary>
        public const string Gate = "gate";
        /// <summary>Maden yatağı ya da madeni işleyen fabrika (nokta). subtype = "&lt;maden&gt;-site" | "&lt;maden&gt;-factory".</summary>
        public const string Mine = "mine";

        public string id;
        public string name;
        /// <summary>river | mountain | volcano | plain | plateau</summary>
        public string category;
        /// <summary>line | polygon | point</summary>
        public string kind;
        /// <summary>Alt tür: göl tipi, kıyı şekli türü (strait/gulf/peninsula/island/cape), unesco/park vb.</summary>
        public string subtype;
        /// <summary>Maden öğelerinde maden kimliği (demir, bakır...).</summary>
        public string mineral;
        public bool IsFactory => subtype != null && subtype.EndsWith("-factory");
        /// <summary>KPSS bilgi notu (cevaptan sonra gösterilir).</summary>
        public string info;
        public float[] label;
        public ProvinceRing[] lines;
        public float[] vertices;
        public int[] triangles;
        public ProvinceRing[] outline;
        /// <summary>Dağlar için koyu iç çekirdek alanı.</summary>
        public float[] innerVertices;
        public int[] innerTriangles;
        /// <summary>Komşu ülkeler: bu ülkeyle sınırı olan illerin plakaları.</summary>
        public int[] plates;

        public Vector2 LabelPoint => new Vector2(label[0], label[1]);
        public bool IsLine => kind == "line";
        public bool IsPolygon => kind == "polygon";
        public bool IsPoint => kind == "point";
    }

    [Serializable]
    public class LakeData
    {
        public string name;
        public float[] vertices;
        public int[] triangles;
        public ProvinceRing[] outline;
    }

    [Serializable]
    public class GeoFeatureDatabase
    {
        public const string ResourcePath = "Data/turkey_features";

        public string source;
        public GeoFeature[] features;
        public LakeData[] lakes;
        /// <summary>Komşu ülkeler (dekor). Alan yapısı göllerle aynıdır.</summary>
        public LakeData[] countries;

        public static GeoFeatureDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[KPSS] Yer şekli verisi bulunamadı: Resources/{ResourcePath}.json");
                return new GeoFeatureDatabase { features = Array.Empty<GeoFeature>(), lakes = Array.Empty<LakeData>() };
            }
            return JsonUtility.FromJson<GeoFeatureDatabase>(asset.text);
        }

        /// <summary>Kategori adının Türkçe karşılığı (HUD'da "BU ___ HARİTADA BUL").</summary>
        public static string CategoryNoun(string category) => category switch
        {
            GeoFeature.River => "AKARSUYU",
            GeoFeature.Mountain => "DAĞI",
            GeoFeature.Volcano => "VOLKANİK DAĞI",
            GeoFeature.Plain => "OVAYI",
            GeoFeature.Plateau => "PLATOYU",
            GeoFeature.Lake => "GÖLÜ",
            GeoFeature.Dam => "BARAJI",
            GeoFeature.Gulf => "BOĞAZI / KÖRFEZİ",
            GeoFeature.Peninsula => "KIYI ŞEKLİNİ",
            GeoFeature.Unesco => "MİRAS ALANINI",
            GeoFeature.Park => "MİLLİ PARKI",
            GeoFeature.Fault => "FAY HATTINI",
            GeoFeature.Country => "ÜLKEYİ",
            GeoFeature.Gate => "SINIR KAPISINI",
            GeoFeature.Mine => "MADEN YERİNİ",
            _ => "YER ŞEKLİNİ"
        };

        /// <summary>Maden kimliğinin Türkçe adı (soru metinleri için).</summary>
        public static string MineralName(string mineral) => mineral switch
        {
            "demir" => "Demir",
            _ => string.IsNullOrEmpty(mineral) ? "Maden" : char.ToUpper(mineral[0]) + mineral.Substring(1)
        };
    }
}
