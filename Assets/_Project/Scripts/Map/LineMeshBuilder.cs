using System.Collections.Generic;
using UnityEngine;

namespace KPSSGeography.Map
{
    /// <summary>Kalın çizgi (miter birleşimli şerit), dolgu ve ikon mesh'lerini köşe renkleriyle üretir.</summary>
    public class LineMeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Color> Colors = new List<Color>();
        public readonly List<int> Triangles = new List<int>();

        public void AddPolyline(IReadOnlyList<Vector2> pts, float width, Color color, bool closed = false)
        {
            int n = pts.Count;
            if (n < 2) return;
            float half = width * 0.5f;
            int start = Vertices.Count;
            int count = closed ? n + 1 : n;

            for (int k = 0; k < count; k++)
            {
                int i = k % n;
                Vector2 p = pts[i];
                Vector2 prev = closed ? pts[(i - 1 + n) % n] : pts[Mathf.Max(i - 1, 0)];
                Vector2 next = closed ? pts[(i + 1) % n] : pts[Mathf.Min(i + 1, n - 1)];
                Vector2 d1 = (p - prev).sqrMagnitude > 1e-10f ? (p - prev).normalized : (next - p).normalized;
                Vector2 d2 = (next - p).sqrMagnitude > 1e-10f ? (next - p).normalized : d1;
                Vector2 n1 = new Vector2(-d1.y, d1.x);
                Vector2 n2 = new Vector2(-d2.y, d2.x);
                Vector2 normal = (n1 + n2).sqrMagnitude > 1e-6f ? (n1 + n2).normalized : n1;
                float miter = half / Mathf.Max(0.35f, Vector2.Dot(normal, n1));

                Vertices.Add(p + normal * miter);
                Vertices.Add(p - normal * miter);
                Colors.Add(color);
                Colors.Add(color);
            }

            for (int k = 0; k < count - 1; k++)
            {
                int a = start + k * 2;
                Triangles.Add(a); Triangles.Add(a + 2); Triangles.Add(a + 1);
                Triangles.Add(a + 1); Triangles.Add(a + 2); Triangles.Add(a + 3);
            }
        }

        /// <summary>Kesik çizgi (fay hatları için).</summary>
        public void AddDashedPolyline(IReadOnlyList<Vector2> pts, float width, float dash, float gap, Color color)
        {
            var seg = new List<Vector2>();
            bool drawing = true;
            float remaining = dash;
            seg.Add(pts[0]);
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float len = Vector2.Distance(a, b);
                float pos = 0f;
                while (len - pos > remaining)
                {
                    pos += remaining;
                    Vector2 p = Vector2.Lerp(a, b, pos / len);
                    if (drawing)
                    {
                        seg.Add(p);
                        AddPolyline(seg, width, color);
                        seg.Clear();
                    }
                    else seg.Add(p);
                    drawing = !drawing;
                    remaining = drawing ? dash : gap;
                }
                remaining -= len - pos;
                if (drawing) seg.Add(b);
            }
            if (drawing && seg.Count >= 2) AddPolyline(seg, width, color);
        }

        /// <summary>Dörtgen (baklava) işaret.</summary>
        public void AddDiamond(Vector2 c, float r, Color color) =>
            AddQuad(c + new Vector2(0f, -r), c + new Vector2(r, 0f), c + new Vector2(0f, r), c + new Vector2(-r, 0f), color);

        public void AddTriangles(float[] flatVertices, int[] triangles, Color color)
        {
            int start = Vertices.Count;
            for (int i = 0; i < flatVertices.Length / 2; i++)
            {
                Vertices.Add(new Vector3(flatVertices[i * 2], flatVertices[i * 2 + 1], 0f));
                Colors.Add(color);
            }
            foreach (int t in triangles) Triangles.Add(start + t);
        }

        public void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int s = Vertices.Count;
            Vertices.Add(a); Vertices.Add(b); Vertices.Add(c);
            Colors.Add(color); Colors.Add(color); Colors.Add(color);
            Triangles.Add(s); Triangles.Add(s + 1); Triangles.Add(s + 2);
        }

        public void AddQuad(Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl, Color color)
        {
            AddTriangle(bl, tl, br, color);
            AddTriangle(br, tl, tr, color);
        }

        public void AddCircle(Vector2 center, float radius, Color color, int segments = 18)
        {
            int s = Vertices.Count;
            Vertices.Add(center);
            Colors.Add(color);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vertices.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                Colors.Add(color);
            }
            for (int i = 0; i < segments; i++)
            {
                Triangles.Add(s); Triangles.Add(s + i + 2); Triangles.Add(s + i + 1);
            }
        }

        /// <summary>Sırt çizgisi boyunca belirli aralıklarla küçük dağ üçgenleri ekler (harita sembolü).</summary>
        public void AddRidgeMarks(IReadOnlyList<Vector2> pts, float spacing, float size, Color color)
        {
            float carry = spacing * 0.5f;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float len = Vector2.Distance(a, b);
                float d = carry;
                while (d < len)
                {
                    Vector2 p = Vector2.Lerp(a, b, d / len);
                    AddTriangle(p + new Vector2(-size * 0.55f, -size * 0.3f), p + new Vector2(0f, size * 0.7f),
                        p + new Vector2(size * 0.55f, -size * 0.3f), color);
                    d += spacing;
                }
                carry = d - len;
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(Vertices);
            mesh.SetColors(Colors);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Vector2[] ToPoints(float[] flat)
        {
            var pts = new Vector2[flat.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(flat[i * 2], flat[i * 2 + 1]);
            return pts;
        }
    }
}
