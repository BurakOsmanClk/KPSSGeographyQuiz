using System;
using System.Collections.Generic;
using UnityEngine;

namespace KPSSGeography.Data
{
    /// <summary>İl bazlı konu seti (ör. "Fındık": öne çıkan iller). Tools/content/build_topics.py ile üretilir.</summary>
    [Serializable]
    public class TopicSet
    {
        public string id;
        /// <summary>Bölümün kullandığı grup (levels.json "set").</summary>
        public string set;
        public string title;
        public string question;
        public int[] plates;
        public string info;
        public string source;
    }

    [Serializable]
    public class AttributeGroup
    {
        public int choice;
        public int[] plates;
    }

    /// <summary>İllere ya da yer şekillerine ait seçmeli özellik (iklim, bitki örtüsü, göl tipi).</summary>
    [Serializable]
    public class AttributeDef
    {
        public string id;
        public string prompt;
        public string[] choices;
        public string[] colors;
        public string[] infos;
        public AttributeGroup[] groups;

        public Color ChoiceColor(int i) =>
            colors != null && i < colors.Length && ColorUtility.TryParseHtmlString(colors[i], out var c) ? c : Color.white;
    }

    [Serializable]
    public class TopicDatabase
    {
        public const string ResourcePath = "Data/topics";
        public const string ExtraResourcePath = "Data/topics_ek";

        public TopicSet[] topics;
        public AttributeDef[] attributes;

        public IEnumerable<TopicSet> InSet(string set)
        {
            foreach (var t in topics)
                if (t.set == set) yield return t;
        }

        public AttributeDef Attribute(string id) => Array.Find(attributes, a => a.id == id);

        public static TopicDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[KPSS] Konu verisi bulunamadı: Resources/{ResourcePath}.json");
                return new TopicDatabase { topics = Array.Empty<TopicSet>(), attributes = Array.Empty<AttributeDef>() };
            }
            var db = JsonUtility.FromJson<TopicDatabase>(asset.text);
            // Ek konu setleri (Tools/content/build_topics_ek.py): ayrı dosyada tutulur ki topics.json ile çakışmasın
            var extraAsset = Resources.Load<TextAsset>(ExtraResourcePath);
            if (extraAsset != null)
            {
                var extra = JsonUtility.FromJson<TopicDatabase>(extraAsset.text);
                if (extra?.topics != null && extra.topics.Length > 0)
                {
                    var all = new List<TopicSet>(db.topics ?? Array.Empty<TopicSet>());
                    all.AddRange(extra.topics);
                    db.topics = all.ToArray();
                }
            }
            return db;
        }
    }

    /// <summary>
    /// A–E şıklı KPSS sorusu. highlight: "feature:&lt;id&gt;" ya da "plate:&lt;no&gt;" (boş olabilir).
    /// Numaralı harita sorusu: markers = haritada I, II, III… diye numaralanan yerler (aynı biçimde),
    /// answerMarkers = doğru olan numaraların sırası (0 = I); cevaptan sonra haritada yeşile boyanırlar.
    /// </summary>
    [Serializable]
    public class McqQuestion
    {
        public string id;
        public string set;
        public string text;
        public string[] options;
        public int answer;
        public string explanation;
        public string highlight;
        public string[] markers;
        public int[] answerMarkers;
        /// <summary>Sorudan önce okunan metin: giriş cümlesi ve madde işaretli bilgiler (\n ile ayrılır). Boş olabilir.</summary>
        public string body;
    }

    /// <summary>Bir il merkezinin aylık ortalama sıcaklık (°C) ve yağış (mm) değerleri (iklim grafiği soruları).</summary>
    [Serializable]
    public class ClimateCity
    {
        public int plate;
        public string name;
        /// <summary>İklim tipi (çeldiriciler farklı tiplerden seçilir).</summary>
        public string type;
        public float[] temp;
        public float[] rain;
        public string note;
    }

    [Serializable]
    public class ClimateDatabase
    {
        public const string ResourcePath = "Data/climate";

        public string source;
        public ClimateCity[] cities;

        public static ClimateDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[KPSS] İklim verisi bulunamadı: Resources/{ResourcePath}.json");
                return new ClimateDatabase { cities = Array.Empty<ClimateCity>() };
            }
            return JsonUtility.FromJson<ClimateDatabase>(asset.text);
        }
    }

    [Serializable]
    public class McqDatabase
    {
        public const string ResourcePath = "Data/questions";

        public McqQuestion[] questions;

        public static McqDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[KPSS] Soru bankası bulunamadı: Resources/{ResourcePath}.json");
                return new McqDatabase { questions = Array.Empty<McqQuestion>() };
            }
            return JsonUtility.FromJson<McqDatabase>(asset.text);
        }
    }
}
