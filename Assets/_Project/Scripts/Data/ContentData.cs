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
            return JsonUtility.FromJson<TopicDatabase>(asset.text);
        }
    }

    /// <summary>A–E şıklı KPSS sorusu. highlight: "feature:&lt;id&gt;" ya da "plate:&lt;no&gt;" (boş olabilir).</summary>
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

    /// <summary>"Öğren" ekranındaki bir ders: harita görünümü + ders notu.</summary>
    [Serializable]
    public class Lesson
    {
        public string id;
        public string tab;
        public string title;
        /// <summary>attribute (illeri özelliğe göre boya) | feature (yer şekli kategorisi) | topicset (konu seti)</summary>
        public string kind;
        /// <summary>attribute id, yer şekli kategorisi ya da konu seti.</summary>
        public string reference;
        public string notes;
    }

    [Serializable]
    public class LessonTab
    {
        public string id;
        public string title;
    }

    [Serializable]
    public class LessonDatabase
    {
        public const string ResourcePath = "Data/lessons";

        public LessonTab[] tabs;
        public Lesson[] lessons;

        public List<Lesson> InTab(string tab)
        {
            var list = new List<Lesson>();
            foreach (var l in lessons)
                if (l.tab == tab) list.Add(l);
            return list;
        }

        public static LessonDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[KPSS] Ders verisi bulunamadı: Resources/{ResourcePath}.json");
                return new LessonDatabase { tabs = Array.Empty<LessonTab>(), lessons = Array.Empty<Lesson>() };
            }
            return JsonUtility.FromJson<LessonDatabase>(asset.text);
        }
    }
}
