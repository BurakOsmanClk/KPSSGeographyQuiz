using System.Linq;
using System.Collections.Generic;
using KPSSGeography.Core;
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

        /// <summary>Göl ve komşu ülke gibi dekor alanlar: sınav görünümünde soluk griye karışır.</summary>
        class Decor
        {
            public Mesh mesh;
            public Color[] baseColors;
            public Color[] colors;
            public int fillCount;
            public Color examFill;
            public Color examEdge;
        }

        readonly List<Decor> decor = new List<Decor>();
        float lastBlend = -1f;

        readonly List<FeatureView> features = new List<FeatureView>();
        public IReadOnlyList<FeatureView> Features => features;

        public void Build(GeoFeatureDatabase db, Material material, MapPalette palette)
        {
            if (db.countries != null)
                foreach (var c in db.countries) BuildArea("Country: " + c.name, c, material, palette.country, palette.countryEdge, -20, 0.015f, palette.examCountry, palette.examCountryEdge);
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

        void BuildArea(string goName, LakeData area, Material material, Color fill, Color edge, int order, float edgeWidth,
            Color examFill, Color examEdge)
        {
            var b = new LineMeshBuilder();
            b.AddTriangles(area.vertices, area.triangles, fill);
            foreach (var r in area.outline)
                b.AddPolyline(LineMeshBuilder.ToPoints(r.points), edgeWidth, edge, true);
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            var mesh = b.ToMesh(goName);
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.sortingOrder = order;
            decor.Add(new Decor
            {
                mesh = mesh, baseColors = b.Colors.ToArray(), colors = b.Colors.ToArray(),
                fillCount = area.vertices.Length / 2, examFill = examFill, examEdge = examEdge
            });
        }

        void Update()
        {
            float blend = ExamLook.Blend;
            if (Mathf.Approximately(blend, lastBlend)) return;
            lastBlend = blend;
            foreach (var d in decor)
            {
                for (int i = 0; i < d.colors.Length; i++)
                {
                    Color target = i < d.fillCount ? d.examFill : d.examEdge;
                    Color c = Color.Lerp(d.baseColors[i], target, blend);
                    c.a = d.baseColors[i].a;
                    d.colors[i] = c;
                }
                d.mesh.colors = d.colors;
            }
        }

        void BuildLake(LakeData lake, Material material, MapPalette palette)
        {
            var b = new LineMeshBuilder();
            b.AddTriangles(lake.vertices, lake.triangles, palette.lake);
            foreach (var r in lake.outline)
                b.AddPolyline(LineMeshBuilder.ToPoints(r.points), 0.02f, palette.lakeShore, true);

            var go = new GameObject("Lake: " + lake.name);
            go.transform.SetParent(transform, false);
            var lakeMesh = b.ToMesh(lake.name);
            lakeMesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = lakeMesh;
            var r2 = go.AddComponent<MeshRenderer>();
            r2.sharedMaterial = material;
            r2.sortingOrder = 4;
            decor.Add(new Decor
            {
                mesh = lakeMesh, baseColors = b.Colors.ToArray(), colors = b.Colors.ToArray(),
                fillCount = lake.vertices.Length / 2, examFill = palette.examLake, examEdge = palette.examLakeEdge
            });
        }
    }
}
