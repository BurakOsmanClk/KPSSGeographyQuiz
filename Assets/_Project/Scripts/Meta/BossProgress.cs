using System.Collections.Generic;
using UnityEngine;

namespace KPSSGeography.Meta
{
    /// <summary>
    /// Boss savaşlarında "kapatılan" iller: doğru bilinen il kalıcı olarak kapanır, her savaşta haritada boyalı görünür.
    /// Uzun vadeli hedef 81 ilin hepsini kapatmak. PlayerPrefs'te virgülle ayrılmış plaka listesi olarak saklanır.
    /// </summary>
    public static class BossProgress
    {
        const string Key = "kpss.boss.closed";
        public const int TotalProvinces = 81;

        static HashSet<int> closed;

        static HashSet<int> Closed
        {
            get
            {
                if (closed != null) return closed;
                closed = new HashSet<int>();
                foreach (var part in PlayerPrefs.GetString(Key, "").Split(','))
                    if (int.TryParse(part, out int plate) && plate >= 1 && plate <= TotalProvinces) closed.Add(plate);
                return closed;
            }
        }

        public static int Count => Closed.Count;
        public static IEnumerable<int> Plates => Closed;
        public static bool IsClosed(int plate) => Closed.Contains(plate);

        /// <summary>İli kapatır. Dönüş: daha önce kapalı değilse true.</summary>
        public static bool Close(int plate)
        {
            if (!Closed.Add(plate)) return false;
            PlayerPrefs.SetString(Key, string.Join(",", Closed));
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetAll()
        {
            Closed.Clear();
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
