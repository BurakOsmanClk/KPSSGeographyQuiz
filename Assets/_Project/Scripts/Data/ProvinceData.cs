using System;
using UnityEngine;

namespace KPSSGeography.Data
{
    /// <summary>Bir ilin tek bir sınır halkası (dış sınır ya da delik). Noktalar x0,y0,x1,y1... şeklinde.</summary>
    [Serializable]
    public class ProvinceRing
    {
        public float[] points;
    }

    /// <summary>JSON'dan okunan il verisi. Geometri önceden üçgenlenmiştir (Tools/geo/build_provinces.py).</summary>
    [Serializable]
    public class ProvinceData
    {
        public int plate;
        public string name;
        public string region;
        public float[] label;
        /// <summary>Sınır komşusu illerin plaka kodları.</summary>
        public int[] neighbors;
        public float[] vertices;
        public int[] triangles;
        public ProvinceRing[] rings;

        public Vector2 LabelPoint => new Vector2(label[0], label[1]);
    }

    [Serializable]
    public class RegionOutline
    {
        public string region;
        public ProvinceRing[] rings;
    }

    [Serializable]
    public class ProvinceDatabase
    {
        public const string ResourcePath = "Data/turkey_provinces";

        public string source;
        public float[] bounds;
        public ProvinceData[] provinces;
        public RegionOutline[] regionOutlines;
        /// <summary>[lon0, lat0, xScale, yScale]: x = (lon − lon0)·xScale, y = (lat − lat0)·yScale.</summary>
        public float[] projection;

        public Rect Bounds => Rect.MinMaxRect(bounds[0], bounds[1], bounds[2], bounds[3]);

        public static ProvinceDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[KPSS] İl verisi bulunamadı: Resources/{ResourcePath}.json");
                return null;
            }

            var db = JsonUtility.FromJson<ProvinceDatabase>(asset.text);
            if (db == null || db.provinces == null || db.provinces.Length == 0)
            {
                Debug.LogError("[KPSS] İl verisi okunamadı ya da boş.");
                return null;
            }
            return db;
        }
    }
}
