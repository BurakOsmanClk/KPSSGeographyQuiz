using System;
using System.Collections.Generic;
using UnityEngine;

namespace KPSSGeography.Quiz
{
    public enum QuizMode
    {
        /// <summary>Hedef ili haritada tıklayarak bul.</summary>
        FindProvince,
        /// <summary>Haritada vurgulanan ilin coğrafi bölgesini seç.</summary>
        Region,
        /// <summary>Henüz hazır olmayan bölüm ("Yakında").</summary>
        ComingSoon
    }

    /// <summary>Bir bölümün tanımı. Veriler Resources/Data/levels.json dosyasından okunur.</summary>
    [Serializable]
    public class LevelDefinition
    {
        public string id;
        public string title;
        public string description;
        public string mode;
        public string[] regions;
        public int questionCount;
        public float timePerQuestion;
        public int maxMistakes = 3;

        [NonSerialized] public int index;

        public QuizMode Mode => mode switch
        {
            "find" => QuizMode.FindProvince,
            "region" => QuizMode.Region,
            _ => QuizMode.ComingSoon
        };

        public bool IsPlayable => Mode != QuizMode.ComingSoon;
        public bool HasRegionFilter => regions != null && regions.Length > 0;

        public string ModeLabel => Mode switch
        {
            QuizMode.FindProvince => "İLİ BUL",
            QuizMode.Region => "BÖLGE MODU",
            _ => "YAKINDA"
        };

        public bool IncludesRegion(string region)
        {
            if (!HasRegionFilter) return true;
            foreach (var r in regions)
                if (r == region) return true;
            return false;
        }
    }

    [Serializable]
    public class LevelCatalog
    {
        public const string ResourcePath = "Data/levels";

        public LevelDefinition[] levels;

        public IReadOnlyList<LevelDefinition> Levels => levels;

        public static LevelCatalog Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[KPSS] Bölüm verisi bulunamadı: Resources/{ResourcePath}.json");
                return new LevelCatalog { levels = Array.Empty<LevelDefinition>() };
            }
            var catalog = JsonUtility.FromJson<LevelCatalog>(asset.text);
            for (int i = 0; i < catalog.levels.Length; i++) catalog.levels[i].index = i;
            return catalog;
        }

        /// <summary>Verilen bölümden sonraki oynanabilir bölüm (yoksa null).</summary>
        public LevelDefinition NextPlayable(LevelDefinition current)
        {
            for (int i = current.index + 1; i < levels.Length; i++)
                if (levels[i].IsPlayable) return levels[i];
            return null;
        }
    }
}
