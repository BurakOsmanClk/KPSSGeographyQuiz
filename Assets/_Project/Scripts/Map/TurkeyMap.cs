using System.Collections.Generic;
using KPSSGeography.Data;
using UnityEngine;

namespace KPSSGeography.Map
{
    /// <summary>İl verisinden tüm haritayı (iller + gölge) çalışma anında kurar.</summary>
    public class TurkeyMap : MonoBehaviour
    {
        readonly List<ProvinceView> provinces = new List<ProvinceView>();
        readonly Dictionary<int, ProvinceView> byPlate = new Dictionary<int, ProvinceView>();

        public IReadOnlyList<ProvinceView> Provinces => provinces;
        public Rect Bounds { get; private set; }
        public Material SharedMaterial { get; private set; }

        public void Build(ProvinceDatabase db, MapPalette palette)
        {
            Bounds = db.Bounds;
            SharedMaterial = CreateMaterial();

            foreach (var data in db.provinces)
            {
                var go = new GameObject(data.name);
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<ProvinceView>();
                view.Initialize(data, SharedMaterial, palette);
                provinces.Add(view);
                byPlate[data.plate] = view;
            }

            BuildShadow(db, palette);
            BuildSeaBackground(palette);
        }

        /// <summary>
        /// Bölümün kapsadığı illeri öne çıkarır, diğerlerini soldurur ve tıklanamaz yapar.
        /// Dönüş: odaklanılan illerin harita koordinatındaki sınırı.
        /// </summary>
        public Rect SetFocus(System.Func<ProvinceView, bool> include)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            bool any = false;
            foreach (var p in provinces)
            {
                bool inside = include == null || include(p);
                p.SetDimmed(!inside);
                if (!inside) continue;
                any = true;
                var r = p.WorldRect;
                minX = Mathf.Min(minX, r.xMin); minY = Mathf.Min(minY, r.yMin);
                maxX = Mathf.Max(maxX, r.xMax); maxY = Mathf.Max(maxY, r.yMax);
            }
            return any ? Rect.MinMaxRect(minX, minY, maxX, maxY) : Bounds;
        }

        /// <summary>Doğru/yanlış boyamalarını temizler (soluklaştırma korunur).</summary>
        public void ClearStates()
        {
            foreach (var p in provinces) p.SetState(ProvinceState.Idle);
        }

        /// <summary>Tüm illeri nötr renge çevirir (bölge renkleri ipucu vermesin).</summary>
        public void SetNeutral(bool value)
        {
            foreach (var p in provinces) p.SetNeutral(value);
        }

        public void ClearFocus()
        {
            foreach (var p in provinces) p.SetDimmed(false);
        }

        public ProvinceView FindAt(Vector2 mapPoint)
        {
            if (!Bounds.Contains(mapPoint)) return null;
            for (int i = 0; i < provinces.Count; i++)
                if (!provinces[i].IsDimmed && provinces[i].Contains(mapPoint))
                    return provinces[i];
            return null;
        }

        public ProvinceView GetByPlate(int plate) => byPlate.TryGetValue(plate, out var v) ? v : null;

        public void ResetAll()
        {
            foreach (var p in provinces) p.ResetVisual();
        }

        /// <summary>Tüm Türkiye'nin altına hafif kaydırılmış yumuşak bir gölge çizer.</summary>
        void BuildShadow(ProvinceDatabase db, MapPalette palette)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            foreach (var d in db.provinces)
            {
                int baseIndex = verts.Count;
                for (int i = 0; i < d.vertices.Length / 2; i++)
                    verts.Add(new Vector3(d.vertices[i * 2], d.vertices[i * 2 + 1], 0f));
                foreach (int t in d.triangles) tris.Add(baseIndex + t);
            }

            var colors = new Color[verts.Count];
            for (int i = 0; i < colors.Length; i++) colors[i] = palette.shadow;

            var mesh = new Mesh { name = "TurkeyShadow", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.colors = colors;

            var go = new GameObject("Shadow");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.07f, -0.1f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = SharedMaterial;
            r.sortingOrder = -10;
        }

        /// <summary>Haritanın arkasına dikey gradyanlı büyük bir deniz düzlemi çizer.</summary>
        void BuildSeaBackground(MapPalette palette)
        {
            Vector2 c = Bounds.center;
            float hw = Bounds.width * 4f, hh = Bounds.height * 6f;
            // 4 satırlı düzlem: gradyan sadece haritanın çevresinde akar, uzakta düz renk olur
            float y0 = c.y - hh, y1 = Bounds.yMin - Bounds.height * 0.6f, y2 = Bounds.yMax + Bounds.height * 0.6f, y3 = c.y + hh;
            float[] ys = { y0, y1, y2, y3 };
            Color[] rowColors = { palette.seaBottom, palette.seaBottom, palette.seaTop, palette.seaTop };
            var verts = new Vector3[8];
            var colors = new Color[8];
            for (int row = 0; row < 4; row++)
            {
                verts[row * 2] = new Vector3(c.x - hw, ys[row], 0f);
                verts[row * 2 + 1] = new Vector3(c.x + hw, ys[row], 0f);
                colors[row * 2] = colors[row * 2 + 1] = rowColors[row];
            }
            var tris = new int[18];
            for (int row = 0; row < 3; row++)
            {
                int i = row * 2, t = row * 6;
                tris[t] = i; tris[t + 1] = i + 2; tris[t + 2] = i + 1;
                tris[t + 3] = i + 2; tris[t + 4] = i + 3; tris[t + 5] = i + 1;
            }
            var mesh = new Mesh { name = "Sea", vertices = verts, colors = colors, triangles = tris };

            var go = new GameObject("Sea");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = SharedMaterial;
            r.sortingOrder = -100;
        }

        static Material CreateMaterial()
        {
            // Sprites/Default: vertex rengi kullanan, ışıksız, URP'de de çalışan yerleşik shader.
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) Debug.LogError("[KPSS] Harita için uygun shader bulunamadı.");
            return new Material(shader) { name = "MapVertexColor" };
        }
    }
}
