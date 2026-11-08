using System.Collections.Generic;
using KPSSGeography.Core;
using KPSSGeography.Data;
using UnityEngine;

namespace KPSSGeography.Map
{
    /// <summary>
    /// Tek bir fiziki harita öğesinin görseli ve tıklama alanı.
    /// Akarsu = mavi şerit, dağ = kahverengi sırt + üçgen semboller, volkan = koni + lav,
    /// ova = yeşil alan, plato = toprak rengi alan.
    /// </summary>
    public class FeatureView : MonoBehaviour, IMapTarget
    {
        const int RaiseOffset = 40;

        public GeoFeature Data { get; private set; }
        public string Category => Data.category;
        public string DisplayName => Data.name;
        public Vector3 LabelWorldPosition => new Vector3(Data.label[0], Data.label[1], 0f) + transform.parent.position;
        public ProvinceState State { get; private set; }

        /// <summary>Küçük alanlar (ör. küçük göller) için daha geniş tıklama toleransı.</summary>
        public bool IsSmall => Data.IsPolygon && bounds.width + bounds.height < 0.35f;

        /// <summary>Bu öğe şu an soru olarak tıklanabilir mi?</summary>
        public bool Interactive { get; set; }

        MapPalette palette;
        Mesh mesh;
        MeshRenderer meshRenderer;
        Color[] baseColors;
        Color[] colors;
        int baseOrder;

        Vector2[][] lines;
        Vector2[][] rings;
        Vector2 point;
        Rect bounds;

        bool hovered;
        bool focused;
        float focusTime;
        float tintWeight;
        Color tintColor;
        Color flashColor;
        float flashTime;
        float flashDuration;
        int flashPulses;
        bool dirty = true;
        float lastBlend;

        public void Initialize(GeoFeature data, Material material, MapPalette mapPalette)
        {
            Data = data;
            palette = mapPalette;
            name = $"{data.category}: {data.name}";

            var b = new LineMeshBuilder();
            switch (data.category)
            {
                case GeoFeature.River:
                    baseOrder = 22;
                    foreach (var l in data.lines) b.AddPolyline(LineMeshBuilder.ToPoints(l.points), 0.05f, palette.river);
                    break;
                case GeoFeature.Mountain:
                    // KPSS haritalarındaki gibi: açık kahve dış alan, koyu kahve çekirdek, sırt boyunca küçük semboller
                    baseOrder = 24;
                    if (data.vertices != null && data.vertices.Length > 0)
                    {
                        b.AddTriangles(data.vertices, data.triangles, palette.mountainLight);
                        if (data.innerVertices != null && data.innerVertices.Length > 0)
                            b.AddTriangles(data.innerVertices, data.innerTriangles, palette.mountain);
                        foreach (var l in data.lines)
                            b.AddRidgeMarks(LineMeshBuilder.ToPoints(l.points), 0.24f, 0.12f, palette.mountainMark);
                    }
                    else
                    {
                        foreach (var l in data.lines)
                        {
                            var pts = LineMeshBuilder.ToPoints(l.points);
                            b.AddPolyline(pts, 0.11f, palette.mountain);
                            b.AddRidgeMarks(pts, 0.2f, 0.15f, palette.mountainMark);
                        }
                    }
                    break;
                case GeoFeature.Volcano:
                    baseOrder = 26;
                    Vector2 c = data.LabelPoint;
                    b.AddCircle(c + new Vector2(0f, -0.02f), 0.2f, new Color(0f, 0f, 0f, 0.18f));
                    b.AddTriangle(c + new Vector2(-0.19f, -0.12f), c + new Vector2(-0.05f, 0.17f), c + new Vector2(0.05f, 0.17f), palette.volcano);
                    b.AddTriangle(c + new Vector2(-0.19f, -0.12f), c + new Vector2(0.05f, 0.17f), c + new Vector2(0.19f, -0.12f), palette.volcano);
                    b.AddQuad(c + new Vector2(-0.065f, 0.12f), c + new Vector2(0.065f, 0.12f), c + new Vector2(0.05f, 0.17f), c + new Vector2(-0.05f, 0.17f), palette.lava);
                    b.AddTriangle(c + new Vector2(-0.03f, 0.17f), c + new Vector2(0f, 0.26f), c + new Vector2(0.03f, 0.17f), palette.lava);
                    break;
                case GeoFeature.Massif:
                    // Katmanlı kaya bloğu: koyu çerçeveli taş moru altıgen, içinde açık renkli başkalaşım şeritleri
                    baseOrder = 26;
                    BuildMassif(b, data.LabelPoint);
                    break;
                case GeoFeature.Lake:
                    baseOrder = 21;
                    b.AddTriangles(data.vertices, data.triangles, palette.lake);
                    foreach (var r in data.outline)
                        b.AddPolyline(LineMeshBuilder.ToPoints(r.points), 0.02f, palette.lakeShore, true);
                    break;
                case GeoFeature.Fault:
                    baseOrder = 23;
                    foreach (var l in data.lines)
                        b.AddDashedPolyline(LineMeshBuilder.ToPoints(l.points), 0.065f, 0.16f, 0.09f, palette.fault);
                    break;
                case GeoFeature.Gulf when !data.IsPoint:
                case GeoFeature.Peninsula when !data.IsPoint:
                {
                    // Körfez, boğaz, yarımada, ada: gerçek kıyı çizgisi boyunca beyaz kenarlı renkli şerit
                    baseOrder = 24;
                    Color tone = data.subtype == "strait" ? palette.strait : data.subtype == "gulf" ? palette.gulf
                        : data.subtype == "island" ? palette.island : palette.peninsula;
                    foreach (var l in data.lines)
                    {
                        var pts = LineMeshBuilder.ToPoints(l.points);
                        b.AddPolyline(pts, 0.17f, palette.coastEdge);
                        b.AddPolyline(pts, 0.11f, tone);
                    }
                    break;
                }
                case GeoFeature.Dam:
                case GeoFeature.Gulf:
                case GeoFeature.Peninsula:
                case GeoFeature.Unesco:
                case GeoFeature.Park:
                    baseOrder = 27;
                    BuildMarker(b, data);
                    break;
                case GeoFeature.Country:
                    // İller gibi pastel dolgu + beyaz sınır
                    baseOrder = -12; // gri dekor ülkelerin (-20) üstünde, Türkiye gölgesinin (-10) altında
                    b.AddTriangles(data.vertices, data.triangles, palette.CountryColor(data.subtype));
                    foreach (var r in data.outline)
                        b.AddPolyline(LineMeshBuilder.ToPoints(r.points), 0.03f, palette.countryBorder, true);
                    break;
                case GeoFeature.Gate:
                    baseOrder = 28;
                    BuildGate(b, data.LabelPoint);
                    break;
                case GeoFeature.Mine:
                    baseOrder = 28;
                    BuildMine(b, data);
                    break;
                case GeoFeature.CoastType:
                {
                    // Kıyı tipi: kıyı çizgisi boyunca beyaz kenarlı renkli şerit; biriktirme şekli: renkli rozet
                    Color tc = palette.CoastTypeColor(data.subtype);
                    if (data.IsPolygon)
                    {
                        // Tombolo, lagün, ria: haritadaki yarımada / göl / boğaz şeklinin kendisi, tip renginde
                        baseOrder = 23;
                        b.AddTriangles(data.vertices, data.triangles, new Color(tc.r, tc.g, tc.b, 0.8f));
                        foreach (var r in data.outline)
                            b.AddPolyline(LineMeshBuilder.ToPoints(r.points), 0.03f, palette.coastEdge, true);
                    }
                    else if (data.IsLine)
                    {
                        baseOrder = 24;
                        foreach (var l in data.lines)
                        {
                            var pts = LineMeshBuilder.ToPoints(l.points);
                            b.AddPolyline(pts, 0.16f, palette.coastEdge);
                            b.AddPolyline(pts, 0.1f, tc);
                        }
                    }
                    else
                    {
                        baseOrder = 27;
                        Vector2 cp = data.LabelPoint;
                        b.AddCircle(cp + new Vector2(0.012f, -0.02f), 0.16f, new Color(0f, 0f, 0f, 0.2f));
                        b.AddCircle(cp, 0.15f, palette.markerEdge);
                        b.AddCircle(cp, 0.125f, Color.white);
                        b.AddCircle(cp, 0.09f, tc);
                    }
                    break;
                }
                case GeoFeature.Plain:
                case GeoFeature.Plateau:
                    bool plain = data.category == GeoFeature.Plain;
                    baseOrder = 20;
                    b.AddTriangles(data.vertices, data.triangles, plain ? palette.plain : palette.plateau);
                    foreach (var r in data.outline)
                        b.AddPolyline(LineMeshBuilder.ToPoints(r.points), 0.025f, plain ? palette.plainEdge : palette.plateauEdge, true);
                    break;
            }

            mesh = b.ToMesh(data.id);
            baseColors = b.Colors.ToArray();
            colors = (Color[])baseColors.Clone();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = baseOrder;

            // Tıklama verisi
            if (data.IsLine)
            {
                lines = new Vector2[data.lines.Length][];
                for (int i = 0; i < lines.Length; i++) lines[i] = LineMeshBuilder.ToPoints(data.lines[i].points);
            }
            else if (data.IsPolygon)
            {
                rings = new Vector2[data.outline.Length][];
                for (int i = 0; i < rings.Length; i++) rings[i] = LineMeshBuilder.ToPoints(data.outline[i].points);
            }
            point = data.LabelPoint;
            bounds = ComputeBounds();
        }

        /// <summary>Nokta türü öğeler için tür/alt türe göre harita işareti.</summary>
        void BuildMarker(LineMeshBuilder b, GeoFeature data)
        {
            Vector2 c = data.LabelPoint;
            Color edge = palette.markerEdge;
            b.AddCircle(c + new Vector2(0.012f, -0.02f), 0.16f, new Color(0f, 0f, 0f, 0.2f));
            switch (data.category)
            {
                case GeoFeature.Dam:
                    b.AddCircle(c, 0.14f, Color.white);
                    b.AddCircle(c, 0.115f, palette.dam);
                    b.AddQuad(c + new Vector2(-0.075f, -0.025f), c + new Vector2(0.075f, -0.025f),
                        c + new Vector2(0.075f, 0.025f), c + new Vector2(-0.075f, 0.025f), Color.white);
                    break;
                case GeoFeature.Gulf:
                    Color water = data.subtype == "strait" ? palette.strait : palette.gulf;
                    b.AddCircle(c, 0.145f, water);
                    b.AddCircle(c, 0.09f, Color.white);
                    b.AddCircle(c, 0.05f, water);
                    break;
                case GeoFeature.Peninsula:
                    Color fill = data.subtype == "island" ? palette.island : data.subtype == "cape" ? palette.cape : palette.peninsula;
                    b.AddCircle(c, 0.145f, edge);
                    b.AddCircle(c, 0.115f, fill);
                    break;
                case GeoFeature.Unesco:
                    b.AddDiamond(c, 0.18f, edge);
                    b.AddDiamond(c, 0.145f, palette.unesco);
                    b.AddDiamond(c, 0.055f, Color.white);
                    break;
                case GeoFeature.Park:
                    b.AddCircle(c, 0.145f, Color.white);
                    b.AddCircle(c, 0.12f, palette.park);
                    b.AddTriangle(c + new Vector2(-0.07f, -0.05f), c + new Vector2(0f, 0.08f), c + new Vector2(0.07f, -0.05f), Color.white);
                    break;
            }
        }

        /// <summary>
        /// Maden işaretleri (ders atlaslarındaki gibi): yatak = beyaz rozet üzerinde maden renginde kare;
        /// fabrika = beyaz rozet üzerinde bacalı, testere çatılı fabrika silueti (çatı şeridi maden renginde).
        /// </summary>
        void BuildMine(LineMeshBuilder b, GeoFeature data)
        {
            Vector2 c = data.LabelPoint;
            Color ore = !string.IsNullOrEmpty(data.color) && ColorUtility.TryParseHtmlString(data.color, out var parsed)
                ? parsed : palette.MineralColor(data.mineral);
            b.AddCircle(c + new Vector2(0.012f, -0.02f), 0.16f, new Color(0f, 0f, 0f, 0.22f));
            b.AddCircle(c, 0.155f, palette.markerEdge);
            b.AddCircle(c, 0.13f, Color.white);
            if (!data.IsFactory)
            {
                const float s = 0.075f;
                b.AddQuad(c + new Vector2(-s, -s), c + new Vector2(s, -s), c + new Vector2(s, s), c + new Vector2(-s, s), ore);
                return;
            }
            // Gövde
            b.AddQuad(c + new Vector2(-0.09f, -0.075f), c + new Vector2(0.09f, -0.075f),
                c + new Vector2(0.09f, 0.0f), c + new Vector2(-0.09f, 0.0f), palette.factory);
            // Testere çatı (iki diş)
            b.AddTriangle(c + new Vector2(-0.09f, 0.0f), c + new Vector2(-0.09f, 0.055f), c + new Vector2(-0.02f, 0.0f), palette.factory);
            b.AddTriangle(c + new Vector2(-0.02f, 0.0f), c + new Vector2(-0.02f, 0.055f), c + new Vector2(0.05f, 0.0f), palette.factory);
            // Baca
            b.AddQuad(c + new Vector2(0.055f, 0.0f), c + new Vector2(0.085f, 0.0f),
                c + new Vector2(0.085f, 0.1f), c + new Vector2(0.055f, 0.1f), palette.factory);
            // Maden rengi şerit (hangi madeni işlediği)
            b.AddQuad(c + new Vector2(-0.09f, -0.075f), c + new Vector2(0.09f, -0.075f),
                c + new Vector2(0.09f, -0.05f), c + new Vector2(-0.09f, -0.05f), ore);
        }

        void BuildMassif(LineMeshBuilder b, Vector2 c)
        {
            b.AddCircle(c + new Vector2(0.012f, -0.02f), 0.16f, new Color(0f, 0f, 0f, 0.2f));
            AddHexagon(b, c, 0.16f, palette.markerEdge);
            AddHexagon(b, c, 0.13f, palette.massif);
            // Başkalaşım (foliasyon) şeritleri
            for (int i = -1; i <= 1; i++)
            {
                float y = i * 0.045f;
                float half = 0.085f - Mathf.Abs(i) * 0.02f;
                b.AddQuad(c + new Vector2(-half, y - 0.012f), c + new Vector2(half, y + 0.004f),
                    c + new Vector2(half, y + 0.018f), c + new Vector2(-half, y + 0.002f), palette.massifLight);
            }
        }

        static void AddHexagon(LineMeshBuilder b, Vector2 c, float r, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                float a0 = Mathf.Deg2Rad * (60f * i + 30f), a1 = Mathf.Deg2Rad * (60f * (i + 1) + 30f);
                // saat yönünde (diğer şekillerle aynı yön; arka yüz kesilmesin)
                b.AddTriangle(c, c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r, c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, color);
            }
        }

        /// <summary>Sınır kapısı: beyaz rozet üzerinde kırmızı-beyaz şeritli bariyer kolu ve direği.</summary>
        void BuildGate(LineMeshBuilder b, Vector2 c)
        {
            b.AddCircle(c + new Vector2(0.012f, -0.02f), 0.15f, new Color(0f, 0f, 0f, 0.22f));
            b.AddCircle(c, 0.15f, palette.markerEdge);
            b.AddCircle(c, 0.125f, Color.white);

            // Direk ve ayağı
            b.AddQuad(c + new Vector2(-0.098f, -0.085f), c + new Vector2(-0.032f, -0.085f),
                c + new Vector2(-0.032f, -0.06f), c + new Vector2(-0.098f, -0.06f), palette.gatePost);
            b.AddQuad(c + new Vector2(-0.08f, -0.06f), c + new Vector2(-0.05f, -0.06f),
                c + new Vector2(-0.05f, 0.05f), c + new Vector2(-0.08f, 0.05f), palette.gatePost);

            // Bariyer kolu: koyu çerçeve + kırmızı/beyaz şeritler
            float x0 = -0.065f, x1 = 0.105f, y0 = 0.0f, y1 = 0.05f, pad = 0.012f;
            b.AddQuad(c + new Vector2(x0 - pad, y0 - pad), c + new Vector2(x1 + pad, y0 - pad),
                c + new Vector2(x1 + pad, y1 + pad), c + new Vector2(x0 - pad, y1 + pad), palette.gatePost);
            const int stripes = 5;
            float w = (x1 - x0) / stripes;
            for (int i = 0; i < stripes; i++)
            {
                float a = x0 + i * w, z = a + w;
                b.AddQuad(c + new Vector2(a, y0), c + new Vector2(z, y0), c + new Vector2(z, y1), c + new Vector2(a, y1),
                    i % 2 == 0 ? palette.gateRed : Color.white);
            }
            // Direğin tepesindeki menteşe
            b.AddCircle(c + new Vector2(-0.065f, 0.025f), 0.026f, palette.gatePost, 10);
        }

        Rect ComputeBounds()
        {
            float minX = point.x, maxX = point.x, minY = point.y, maxY = point.y;
            void Grow(Vector2[][] sets)
            {
                if (sets == null) return;
                foreach (var s in sets)
                foreach (var p in s)
                {
                    minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                }
            }
            Grow(lines);
            Grow(rings);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        /// <summary>Harita noktasının bu öğeye ekran pikseli cinsinden uzaklığı (alanın içindeyse 0).</summary>
        public float DistanceInPixels(Vector2 p, float unitsPerPixel)
        {
            float margin = 40f * unitsPerPixel;
            if (p.x < bounds.xMin - margin || p.x > bounds.xMax + margin || p.y < bounds.yMin - margin || p.y > bounds.yMax + margin)
                return float.MaxValue;

            if (Data.IsPoint)
                return Vector2.Distance(p, point + new Vector2(0f, 0.03f)) / unitsPerPixel;

            if (Data.IsLine)
            {
                float best = float.MaxValue;
                foreach (var l in lines)
                    for (int i = 0; i < l.Length - 1; i++)
                        best = Mathf.Min(best, SegmentDistance(p, l[i], l[i + 1]));
                return best / unitsPerPixel;
            }

            bool inside = false;
            float edge = float.MaxValue;
            foreach (var ring in rings)
            {
                for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                {
                    Vector2 a = ring[i], b = ring[j];
                    if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                        inside = !inside;
                    edge = Mathf.Min(edge, SegmentDistance(p, a, b));
                }
            }
            return inside ? 0f : edge / unitsPerPixel;
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-12f));
            return Vector2.Distance(p, a + ab * t);
        }

        // ---------- IMapTarget ----------

        public void SetState(ProvinceState state) { State = state; dirty = true; }
        public void SetHovered(bool value) { if (hovered != value) { hovered = value; dirty = true; } }

        public void SetFocused(bool value)
        {
            focused = value;
            focusTime = 0f;
            SetRaised(value);
            dirty = true;
        }

        public void PlayCorrect() => Flash(palette.featureCorrect, 0.5f, 1);
        public void PlayWrongClick() => Flash(palette.wrongFlash, 0.6f, 2);
        public void PlayReveal(float duration) => Flash(palette.reveal, duration, 3);

        public void ResetVisual()
        {
            State = ProvinceState.Idle;
            hovered = false;
            focused = false;
            flashTime = 0f;
            tintWeight = 0f;
            Interactive = false;
            SetRaised(false);
            dirty = true;
        }

        void Flash(Color color, float duration, int pulses)
        {
            flashColor = color;
            flashDuration = duration;
            flashTime = duration;
            flashPulses = pulses;
            SetRaised(true);
        }

        void SetRaised(bool raised)
        {
            if (meshRenderer != null) meshRenderer.sortingOrder = raised ? baseOrder + RaiseOffset : baseOrder;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            float blend = ExamLook.Blend;
            if (!Mathf.Approximately(blend, lastBlend))
            {
                lastBlend = blend;
                dirty = true;
            }

            // Kalıcı durum rengi (doğru = yeşil, kaçırılan = kırmızı)
            Color stateTint = State == ProvinceState.Correct ? palette.featureCorrect
                : State == ProvinceState.Missed ? palette.featureMissed : Color.white;
            float stateWeight = State == ProvinceState.Idle ? 0f : 0.7f;
            float targetWeight = hovered ? Mathf.Max(stateWeight, 0.45f * (1f - blend)) : stateWeight;
            Color targetTint = hovered && State == ProvinceState.Idle ? Color.white : stateTint;

            float k = 1f - Mathf.Exp(-18f * dt);
            float newWeight = Mathf.Lerp(tintWeight, targetWeight, k);
            Color newTint = Color.Lerp(tintColor, targetTint, k);
            if (Mathf.Abs(newWeight - tintWeight) > 0.001f || newTint != tintColor) dirty = true;
            tintWeight = newWeight;
            tintColor = newTint;

            Color overlay = Color.clear;
            float overlayWeight = 0f;
            if (focused)
            {
                focusTime += dt;
                overlay = palette.reveal;
                overlayWeight = 0.35f + 0.5f * (0.5f + 0.5f * Mathf.Sin(focusTime * 6f));
                dirty = true;
            }
            if (flashTime > 0f)
            {
                flashTime -= dt;
                float t = 1f - Mathf.Clamp01(flashTime / flashDuration);
                overlay = flashColor;
                overlayWeight = 0.5f - 0.5f * Mathf.Cos(t * flashPulses * Mathf.PI * 2f);
                dirty = true;
                if (flashTime <= 0f && !focused) SetRaised(false);
            }

            if (!dirty) return;
            dirty = false;
            for (int i = 0; i < colors.Length; i++)
            {
                Color c = ExamLook.Pale(baseColors[i], lastBlend);
                Color t = Color.Lerp(c, tintColor, tintWeight);
                t.a = c.a;
                if (overlayWeight > 0f)
                {
                    t = Color.Lerp(t, overlay, overlayWeight);
                    t.a = Mathf.Max(c.a, overlayWeight);
                }
                colors[i] = t;
            }
            mesh.colors = colors;
        }

        void OnDestroy()
        {
            if (mesh) Destroy(mesh);
        }
    }
}
