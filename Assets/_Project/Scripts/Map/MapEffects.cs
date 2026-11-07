using System.Collections.Generic;
using KPSSGeography.Core;
using UnityEngine;

namespace KPSSGeography.Map
{
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
        public void DrawOutline(List<Vector2[]> rings, Color color)
        {
            if (!GameSettings.Effects || rings == null || rings.Count == 0) return;
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
