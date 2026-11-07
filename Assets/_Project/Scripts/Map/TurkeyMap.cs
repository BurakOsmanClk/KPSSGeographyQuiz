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
        }

        public ProvinceView FindAt(Vector2 mapPoint)
        {
            if (!Bounds.Contains(mapPoint)) return null;
            for (int i = 0; i < provinces.Count; i++)
                if (provinces[i].Contains(mapPoint))
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
