using System.Collections.Generic;
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
                case GeoFeature.Dam:
                case GeoFeature.Gulf:
                case GeoFeature.Peninsula:
                case GeoFeature.Unesco:
                case GeoFeature.Park:
                    baseOrder = 27;
                    BuildMarker(b, data);
                    break;
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

            // Kalıcı durum rengi (doğru = yeşil, kaçırılan = kırmızı)
            Color stateTint = State == ProvinceState.Correct ? palette.featureCorrect
                : State == ProvinceState.Missed ? palette.featureMissed : Color.white;
            float stateWeight = State == ProvinceState.Idle ? 0f : 0.7f;
            float targetWeight = hovered ? Mathf.Max(stateWeight, 0.45f) : stateWeight;
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
                Color c = baseColors[i];
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
