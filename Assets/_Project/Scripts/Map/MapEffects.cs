using System.Collections.Generic;
using KPSSGeography.Core;
using UnityEngine;

namespace KPSSGeography.Map
{
    /// <summary>
    /// Numaralı harita sorusunda haritaya çizilen şekil: kavisli ok (ticaret, göç, rüzgâr, akış yönü)
    /// ya da uçları noktalı doğru parçası (güzergâh). Koordinatlar dünya birimindedir.
    /// </summary>
    public class MapShape
    {
        public Vector2 from;
        public Vector2 to;
        public bool arrow;
        /// <summary>Numaranın yanına yazılan kısa açıklama (ör. "Fındık"); boş olabilir.</summary>
        public string caption;

        Vector2 Dir => (to - from).normalized;
        Vector2 Perp => new Vector2(-Dir.y, Dir.x);
        float Bulge => arrow ? 0.18f * Vector2.Distance(from, to) : 0f;
        Vector2 Control => (from + to) * 0.5f + Perp * Bulge;

        public Vector2 Point(float t)
        {
            float u = 1f - t;
            return u * u * from + 2f * u * t * Control + t * t * to;
        }

        /// <summary>Numara rozetinin konumu: şeklin ortasının biraz dışı.</summary>
        public Vector2 LabelPoint => Point(0.5f) + Perp * (arrow ? 0.32f : 0.3f);
    }

    /// <summary>
    /// Dünya koordinatlı hafif efektler: bölüm başında bölge sınırının "çizilerek" belirmesi ve
    /// doğru cevapta ışıltı patlaması. Tek bir mesh her kare yeniden kurulur (efekt yokken hiç iş yapılmaz).
    /// </summary>
    public class MapEffects : MonoBehaviour
    {
        class Outline
        {
            public List<Vector2[]> rings;
            public float[] lengths;
            public float total;
            public Color color;
            public float time;
        }

        class Particle
        {
            public Vector2 pos;
            public Vector2 vel;
            public float life;
            public float maxLife;
            public float size;
            public Color color;
        }

        const float DrawDuration = 0.9f;
        const float HoldDuration = 0.5f;
        const float FadeDuration = 0.6f;

        readonly List<Outline> outlines = new List<Outline>();
        readonly List<Particle> particles = new List<Particle>();
        Mesh mesh;
        MeshRenderer meshRenderer;
        readonly LineMeshBuilder builder = new LineMeshBuilder();

        public void Initialize(Material material)
        {
            mesh = new Mesh { name = "MapEffects" };
            mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = 70;
        }

        /// <summary>Verilen halkaları (bölge dış hattı) sırayla çizer, bekletir ve söndürür.</summary>
        /// <param name="force">Efektler kapalıyken de çiz (sınır, oyunun bir parçasıysa).</param>
        public void DrawOutline(List<Vector2[]> rings, Color color, bool force = false)
        {
            if ((!GameSettings.Effects && !force) || rings == null || rings.Count == 0) return;
            var o = new Outline { rings = rings, color = color, lengths = new float[rings.Count] };
            for (int r = 0; r < rings.Count; r++)
            {
                float len = 0f;
                var ring = rings[r];
                for (int i = 0; i < ring.Length; i++) len += Vector2.Distance(ring[i], ring[(i + 1) % ring.Length]);
                o.lengths[r] = len;
                o.total += len;
            }
            outlines.Add(o);
        }

        /// <summary>Doğru cevapta küçük yıldız parçacıkları saçar.</summary>
        public void Sparkle(Vector3 world, Color color, int count = 14)
        {
            if (!GameSettings.Effects) return;
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                float speed = Random.Range(0.6f, 1.6f);
                particles.Add(new Particle
                {
                    pos = world, vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed,
                    maxLife = Random.Range(0.45f, 0.75f), size = Random.Range(0.05f, 0.1f),
                    color = Color.Lerp(color, Color.white, Random.value * 0.6f)
                });
                particles[particles.Count - 1].life = particles[particles.Count - 1].maxLife;
            }
        }

        public void Clear()
        {
            outlines.Clear();
            particles.Clear();
            if (mesh != null) mesh.Clear();
            ClearShapes();
        }

        // ---------- Numaralı harita şekilleri (ok / doğru parçası) ----------

        Mesh shapeMesh;

        /// <summary>Okları ve doğru parçalarını çizer; <paramref name="highlight"/>'taki sıradakiler vurgu renginde.</summary>
        public void SetShapes(IList<MapShape> shapes, ICollection<int> highlight, Color normal, Color accent)
        {
            if (shapeMesh == null)
            {
                var go = new GameObject("QuestionShapes");
                go.transform.SetParent(transform, false);
                shapeMesh = new Mesh { name = "QuestionShapes" };
                go.AddComponent<MeshFilter>().sharedMesh = shapeMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = meshRenderer != null ? meshRenderer.sharedMaterial : null;
                r.sortingOrder = 65;
            }
            var b = new LineMeshBuilder();
            for (int i = 0; i < shapes.Count; i++)
            {
                var s = shapes[i];
                Color c = highlight != null && highlight.Contains(i) ? accent : normal;
                Color halo = new Color(1f, 1f, 1f, 0.85f);
                if (s.arrow) AddArrow(b, s, c, halo);
                else
                {
                    var pts = new[] { s.from, s.to };
                    b.AddPolyline(pts, 0.13f, halo);
                    b.AddPolyline(pts, 0.07f, c);
                    b.AddCircle(s.from, 0.11f, c);
                    b.AddCircle(s.to, 0.11f, c);
                }
            }
            shapeMesh.Clear();
            shapeMesh.SetVertices(b.Vertices);
            shapeMesh.SetColors(b.Colors);
            shapeMesh.SetTriangles(b.Triangles, 0);
            shapeMesh.RecalculateBounds();
        }

        static void AddArrow(LineMeshBuilder b, MapShape s, Color c, Color halo)
        {
            const int steps = 20;
            float head = Mathf.Min(0.32f, Vector2.Distance(s.from, s.to) * 0.3f);
            var pts = new List<Vector2>();
            for (int i = 0; i <= steps; i++) pts.Add(s.Point(i / (float)steps));
            // Uç: son teğet yönünde üçgen; çizgi ucun gerisinde biter
            Vector2 tip = s.to;
            Vector2 tangent = (s.Point(1f) - s.Point(0.92f)).normalized;
            Vector2 baseCenter = tip - tangent * head;
            while (pts.Count > 2 && Vector2.Distance(pts[pts.Count - 1], tip) < head) pts.RemoveAt(pts.Count - 1);
            pts.Add(baseCenter);
            Vector2 n = new Vector2(-tangent.y, tangent.x) * head * 0.55f;
            b.AddPolyline(pts, 0.13f, halo);
            b.AddPolyline(pts, 0.07f, c);
            b.AddTriangle(baseCenter + n * 1.25f + tangent * -0.02f, tip + tangent * 0.06f, baseCenter - n * 1.25f + tangent * -0.02f, halo);
            b.AddTriangle(baseCenter + n, tip, baseCenter - n, c);
        }

        public void ClearShapes()
        {
            if (shapeMesh != null) shapeMesh.Clear();
        }

        void LateUpdate()
        {
            if (mesh == null) return;
            if (outlines.Count == 0 && particles.Count == 0) return;

            float dt = Time.unscaledDeltaTime;
            builder.Vertices.Clear();
            builder.Colors.Clear();
            builder.Triangles.Clear();

            for (int k = outlines.Count - 1; k >= 0; k--)
            {
                var o = outlines[k];
                o.time += dt;
                float drawn = Mathf.Clamp01(o.time / DrawDuration);
                float fade = 1f - Mathf.Clamp01((o.time - DrawDuration - HoldDuration) / FadeDuration);
                if (fade <= 0f)
                {
                    outlines.RemoveAt(k);
                    continue;
                }
                var c = o.color;
                c.a *= fade;
                float budget = o.total * EaseOut(drawn);
                for (int r = 0; r < o.rings.Count && budget > 0f; r++)
                {
                    float take = Mathf.Min(budget, o.lengths[r]);
                    builder.AddPolyline(Partial(o.rings[r], take), 0.07f, c);
                    budget -= take;
                }
            }

            for (int k = particles.Count - 1; k >= 0; k--)
            {
                var p = particles[k];
                p.life -= dt;
                if (p.life <= 0f)
                {
                    particles.RemoveAt(k);
                    continue;
                }
                p.pos += p.vel * dt;
                p.vel *= Mathf.Exp(-3f * dt);
                float t = p.life / p.maxLife;
                var c = p.color;
                c.a = t;
                float s = p.size * (0.6f + 0.4f * t);
                builder.AddQuad(p.pos + new Vector2(0f, -s), p.pos + new Vector2(s * 0.45f, 0f), p.pos + new Vector2(0f, s), p.pos + new Vector2(-s * 0.45f, 0f), c);
                builder.AddQuad(p.pos + new Vector2(-s, 0f), p.pos + new Vector2(0f, -s * 0.45f), p.pos + new Vector2(s, 0f), p.pos + new Vector2(0f, s * 0.45f), c);
            }

            mesh.Clear();
            mesh.SetVertices(builder.Vertices);
            mesh.SetColors(builder.Colors);
            mesh.SetTriangles(builder.Triangles, 0);
        }

        static float EaseOut(float x) => 1f - (1f - x) * (1f - x);

        /// <summary>Halkanın baştan itibaren <paramref name="length"/> uzunluğundaki kısmı.</summary>
        static List<Vector2> Partial(Vector2[] ring, float length)
        {
            var pts = new List<Vector2> { ring[0] };
            float acc = 0f;
            for (int i = 0; i < ring.Length; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Length];
                float seg = Vector2.Distance(a, b);
                if (acc + seg >= length)
                {
                    pts.Add(Vector2.Lerp(a, b, (length - acc) / Mathf.Max(seg, 1e-6f)));
                    return pts;
                }
                pts.Add(b);
                acc += seg;
            }
            return pts;
        }
    }
}
