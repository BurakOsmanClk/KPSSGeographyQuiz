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
        /// <summary>Yer şekli (akarsu, dağ, göl, kıyı, fay...) bul.</summary>
        Feature,
        /// <summary>Konu setindeki (tarım, maden, nüfus...) illerden birini seç.</summary>
        Topic,
        /// <summary>Plakası verilen ili bul / ilin plakasını seç.</summary>
        Plate,
        /// <summary>Vurgulanan ile komşu bir il seç.</summary>
        Neighbor,
        /// <summary>Vurgulanan ilin özelliğini (iklim, bitki örtüsü) seç.</summary>
        Attribute,
        /// <summary>Vurgulanan yer şeklinin türünü (göl tipi) seç.</summary>
        FeatureChoice,
        /// <summary>Bir bölgenin bütün illeri tek tek sorulur; bölge sınırı çizili.</summary>
        RegionAll,
        /// <summary>A–E şıklı KPSS sorusu.</summary>
        Mcq,
        /// <summary>Adı verilen sınır ilinin komşu olduğu ülkeyi haritada seç.</summary>
        BorderCountry,
        /// <summary>Henüz hazır olmayan bölüm ("Yakında").</summary>
        ComingSoon
    }

    /// <summary>Bir bölümün tanımı. Veriler Resources/Data/levels.json dosyasından okunur.</summary>
    [Serializable]
    public class LevelDefinition
    {
        public string id;
        public string group;
        public string title;
        public string description;
        public string mode;
        public string[] regions;
        public int questionCount;
        public float timePerQuestion;
        public int maxMistakes = 3;
        /// <summary>Yer şekli bölümleri: river | mountain | volcano | massif | plain | plateau | lake | dam | gulf | peninsula | unesco | park | fault | country | gate</summary>
        public string category;
        /// <summary>Konu / soru seti (topic ve mcq modları).</summary>
        public string set;
        /// <summary>Özellik adı (attribute ve featurechoice modları): climate | vegetation | laketype</summary>
        public string attribute;
        public int plateMin;
        public int plateMax;

        [NonSerialized] public int index;

        public QuizMode Mode => mode switch
        {
            "find" => QuizMode.FindProvince,
            "region" => QuizMode.Region,
            "feature" => QuizMode.Feature,
            "topic" => QuizMode.Topic,
            "plate" => QuizMode.Plate,
            "neighbor" => QuizMode.Neighbor,
            "attribute" => QuizMode.Attribute,
            "featurechoice" => QuizMode.FeatureChoice,
            "regionall" => QuizMode.RegionAll,
            "mcq" => QuizMode.Mcq,
            "bordercountry" => QuizMode.BorderCountry,
            _ => QuizMode.ComingSoon
        };

        public bool IsPlayable => Mode != QuizMode.ComingSoon;
        public bool HasRegionFilter => regions != null && regions.Length > 0;

        public string ModeLabel => Mode switch
        {
            QuizMode.FindProvince => "İLİ BUL",
            QuizMode.Region => "BÖLGE MODU",
            QuizMode.Feature => "YER ŞEKLİ",
            QuizMode.Topic => "KONU HARİTASI",
            QuizMode.Plate => "PLAKA",
            QuizMode.Neighbor => "KOMŞU İLLER",
            QuizMode.Attribute => "ÖZELLİK SEÇ",
            QuizMode.FeatureChoice => "TÜRÜNÜ SEÇ",
            QuizMode.RegionAll => "BÖLGEYİ TAMAMLA",
            QuizMode.Mcq => "KPSS SORUSU",
            QuizMode.BorderCountry => "SINIR KOMŞUSU",
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
    public class LevelGroup
    {
        public string id;
        public string title;
    }

    [Serializable]
    public class LevelCatalog
    {
        public const string ResourcePath = "Data/levels";

        public LevelGroup[] groups;
        public LevelDefinition[] levels;

        public IReadOnlyList<LevelDefinition> Levels => levels;

        public static LevelCatalog Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[KPSS] Bölüm verisi bulunamadı: Resources/{ResourcePath}.json");
                return new LevelCatalog { levels = Array.Empty<LevelDefinition>(), groups = Array.Empty<LevelGroup>() };
            }
            var catalog = JsonUtility.FromJson<LevelCatalog>(asset.text);
            if (catalog.groups == null || catalog.groups.Length == 0)
                catalog.groups = new[] { new LevelGroup { id = "", title = "BÖLÜMLER" } };
            for (int i = 0; i < catalog.levels.Length; i++) catalog.levels[i].index = i;
            return catalog;
        }

        public List<LevelDefinition> InGroup(string groupId)
        {
            var list = new List<LevelDefinition>();
            foreach (var l in levels)
                if (string.IsNullOrEmpty(groupId) || l.group == groupId) list.Add(l);
            return list;
        }

        /// <summary>Aynı gruptaki bir sonraki oynanabilir bölüm (yoksa katalogdaki sıradaki).</summary>
        public LevelDefinition NextPlayable(LevelDefinition current)
        {
            for (int i = current.index + 1; i < levels.Length; i++)
                if (levels[i].IsPlayable) return levels[i];
            return null;
        }
    }
}
