using System;
using System.Collections.Generic;
using KPSSGeography.Quiz;
using UnityEngine;

namespace KPSSGeography.Meta
{
    /// <summary>
    /// Bölüm ilerlemesi (yıldız, en yüksek puan) ve kilit durumu. PlayerPrefs'e JSON olarak yazılır.
    /// Kural: bir bölüm, kendinden önceki oynanabilir bölümden en az 1 yıldız alınınca açılır.
    /// </summary>
    public class ProgressStore
    {
        const string Key = "KPSS_Progress_v1";

        [Serializable]
        class Entry
        {
            public string id;
            public int stars;
            public int best;
        }

        [Serializable]
        class SaveData
        {
            public List<Entry> entries = new List<Entry>();
        }

        readonly LevelCatalog catalog;
        readonly Dictionary<string, Entry> byId = new Dictionary<string, Entry>();

        /// <summary>Test için: tüm oynanabilir bölümleri kilitsiz göster.</summary>
        public bool UnlockAll { get; set; }

        public ProgressStore(LevelCatalog levelCatalog)
        {
            catalog = levelCatalog;
            Load();
        }

        const string ExamSuffix = "@exam";

        /// <summary>Sınav modundaki ilerlemenin kayıt anahtarı (normal modla karışmaz).</summary>
        public static string ExamKey(string id) => id + ExamSuffix;

        public int GetStars(string id, bool exam) => GetStars(exam ? ExamKey(id) : id);
        public int GetBest(string id, bool exam) => GetBest(exam ? ExamKey(id) : id);

        public int GetStars(string id) => byId.TryGetValue(id, out var e) ? e.stars : 0;
        public int GetBest(string id) => byId.TryGetValue(id, out var e) ? e.best : 0;

        public bool IsUnlocked(LevelDefinition level)
        {
            if (!level.IsPlayable) return false;
            if (UnlockAll) return true;
            for (int i = level.index - 1; i >= 0; i--)
            {
                var prev = catalog.levels[i];
                if (prev.IsPlayable) return GetStars(prev.id) > 0;
            }
            return true; // ilk oynanabilir bölüm
        }

        public int TotalStars
        {
            get
            {
                int sum = 0;
                foreach (var e in byId.Values)
                    if (!e.id.EndsWith(ExamSuffix)) sum += e.stars; // sınav yıldızları ayrı sayılır
                return sum;
            }
        }

        public int MaxStars
        {
            get
            {
                int n = 0;
                foreach (var l in catalog.levels) if (l.IsPlayable) n++;
                return n * 3;
            }
        }

        /// <summary>Sonucu kaydeder. Dönüş: puan rekor mu?</summary>
        public bool Record(string id, int stars, int score)
        {
            if (!byId.TryGetValue(id, out var e))
            {
                e = new Entry { id = id };
                byId[id] = e;
            }
            bool newBest = score > e.best;
            e.stars = Mathf.Max(e.stars, stars);
            e.best = Mathf.Max(e.best, score);
            Save();
            return newBest;
        }

        public void ResetAll()
        {
            byId.Clear();
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        void Load()
        {
            byId.Clear();
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data?.entries == null) return;
                foreach (var e in data.entries)
                    if (!string.IsNullOrEmpty(e.id)) byId[e.id] = e;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KPSS] İlerleme kaydı okunamadı, sıfırdan başlanıyor: " + ex.Message);
            }
        }

        void Save()
        {
            var data = new SaveData { entries = new List<Entry>(byId.Values) };
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
