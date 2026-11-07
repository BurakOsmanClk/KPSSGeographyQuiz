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

        public string id;
        public string name;
        /// <summary>river | mountain | volcano | plain | plateau</summary>
        public string category;
        /// <summary>line | polygon | point</summary>
        public string kind;
        /// <summary>KPSS bilgi notu (cevaptan sonra gösterilir).</summary>
        public string info;
        public float[] label;
        public ProvinceRing[] lines;
        public float[] vertices;
        public int[] triangles;
        public ProvinceRing[] outline;

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
            _ => "YER ŞEKLİNİ"
        };
    }
}
