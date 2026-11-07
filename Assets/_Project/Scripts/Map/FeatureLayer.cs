using System.Collections.Generic;
using KPSSGeography.Data;
using UnityEngine;

namespace KPSSGeography.Map
{
    /// <summary>
    /// Fiziki harita katmanı. Göller zemin haritasının parçasıdır ve her zaman görünür.
    /// Akarsu, dağ, volkan, ova ve plato her biri yalnızca kendi bölümünde, tek başına gösterilir.
    /// </summary>
    public class FeatureLayer : MonoBehaviour
    {
        public const string Mixed = "mixed";

        readonly List<FeatureView> features = new List<FeatureView>();
        public IReadOnlyList<FeatureView> Features => features;

        public void Build(GeoFeatureDatabase db, Material material, MapPalette palette)
        {
            foreach (var lake in db.lakes) BuildLake(lake, material, palette);

            foreach (var data in db.features)
            {
                var go = new GameObject(data.name);
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<FeatureView>();
                view.Initialize(data, material, palette);
                features.Add(view);
            }
            ShowDefault();
        }

        /// <summary>Kategori bu bölüme dahil mi? ("mixed" hepsini kapsar; şu an hiçbir bölüm kullanmıyor)</summary>
        public static bool Matches(string levelCategory, string featureCategory) =>
            levelCategory == Mixed || levelCategory == featureCategory;

        /// <summary>Menüler ve il bölümleri: yer şekli gösterilmez (sadece göller).</summary>
        public void ShowDefault()
        {
            foreach (var f in features)
            {
                f.ResetVisual();
                f.gameObject.SetActive(false);
            }
        }

        /// <summary>Yer şekli bölümü: yalnızca o kategorideki öğeleri gösterir (isteğe bağlı tıklanabilir).</summary>
        public void ShowCategory(string category, bool interactive = true)
        {
            foreach (var f in features)
            {
                f.ResetVisual();
                bool match = Matches(category, f.Category);
                f.gameObject.SetActive(match);
                f.Interactive = match && interactive;
            }
        }

        /// <summary>Tek bir öğeyi gösterir (ör. KPSS sorusunda vurgulanan göl), diğerlerini gizler.</summary>
        public void ShowOnly(FeatureView target)
        {
            foreach (var f in features)
            {
                f.ResetVisual();
                f.gameObject.SetActive(f == target);
            }
        }

        public void ClearStates()
        {
            foreach (var f in features) f.SetState(ProvinceState.Idle);
        }

        public FeatureView Find(string id)
        {
            foreach (var f in features)
                if (f.Data.id == id) return f;
            return null;
        }

        /// <summary>İmlecin altındaki (piksel toleranslı) en uygun öğe. Küçük öğeler (nokta, çizgi) önceliklidir.</summary>
        public FeatureView FindAt(Vector2 mapPoint, float unitsPerPixel)
        {
            FeatureView best = null;
            float bestScore = float.MaxValue;
            foreach (var f in features)
            {
                if (!f.Interactive || !f.gameObject.activeSelf) continue;
                float d = f.DistanceInPixels(mapPoint, unitsPerPixel);
                float limit = f.Data.IsPoint ? 30f : f.Data.IsLine ? 16f : f.IsSmall ? 16f : 6f;
                if (d > limit) continue;
                float score = d - (f.Data.IsPoint ? 20f : f.Data.IsLine ? 10f : 0f);
                if (score < bestScore) { bestScore = score; best = f; }
            }
            return best;
        }

        void BuildLake(LakeData lake, Material material, MapPalette palette)
        {
            var b = new LineMeshBuilder();
            b.AddTriangles(lake.vertices, lake.triangles, palette.lake);
            foreach (var r in lake.outline)
                b.AddPolyline(LineMeshBuilder.ToPoints(r.points), 0.02f, palette.lakeShore, true);

            var go = new GameObject("Lake: " + lake.name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = b.ToMesh(lake.name);
            var r2 = go.AddComponent<MeshRenderer>();
            r2.sharedMaterial = material;
            r2.sortingOrder = 4;
        }
    }
}
