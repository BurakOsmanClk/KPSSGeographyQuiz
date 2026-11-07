using KPSSGeography.Core;
using KPSSGeography.Data;
using UnityEngine;

namespace KPSSGeography.Map
{
    public enum ProvinceState
    {
        Idle,
        Correct,
        Missed
    }

    /// <summary>
    /// Tek bir ilin görseli ve tıklama alanı. Mesh, il etiket noktası (pivot) etrafında kurulur;
    /// böylece "pop" animasyonu ilin kendi merkezinde ölçeklenir.
    /// </summary>
    public class ProvinceView : MonoBehaviour, IMapTarget
    {
        const int BaseFillOrder = 0;
        const int RaisedFillOrder = 10;

        public ProvinceData Data { get; private set; }
        public string DisplayName => Data.name;
        public Vector3 LabelWorldPosition => transform.position;
        public ProvinceState State { get; private set; }
        public bool IsDimmed => dimmed;
        public Color BaseColor => baseColor;
        public Color CorrectColor => correctColor;
        public Rect WorldRect { get; private set; }

        MapPalette palette;
        Vector2[][] rings;
        Mesh fillMesh;
        Color[] fillColors;
        MeshRenderer fillRenderer;
        MeshRenderer borderRenderer;
        Mesh borderMesh;
        Color[] borderColors;
        float lastBlend = -1f;

        Color baseColor;
        Color correctColor;
        bool dimmed;
        bool neutral;
        bool uniformDim;
        bool focused;
        float focusTime;

        Color smoothedColor;
        Color appliedColor;
        bool hovered;

        Color flashColor;
        float flashDuration;
        float flashTime;
        int flashPulses;

        float popTime = -1f;
        const float PopDuration = 0.35f;

        public void Initialize(ProvinceData data, Material material, MapPalette mapPalette)
        {
            Data = data;
            palette = mapPalette;
            baseColor = palette.ProvinceBase(data);
            correctColor = MapPalette.Saturated(palette.RegionColor(data.region));
            name = $"{data.plate:00} {data.name}";

            Vector2 pivot = data.LabelPoint;
            transform.localPosition = new Vector3(pivot.x, pivot.y, 0f);

            // --- Dolgu mesh'i ---
            int vCount = data.vertices.Length / 2;
            var verts = new Vector3[vCount];
            for (int i = 0; i < vCount; i++)
                verts[i] = new Vector3(data.vertices[i * 2] - pivot.x, data.vertices[i * 2 + 1] - pivot.y, 0f);

            fillColors = new Color[vCount];
            fillMesh = new Mesh { name = data.name + "_Fill" };
            fillMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            fillMesh.vertices = verts;
            fillMesh.triangles = data.triangles;
            fillMesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = fillMesh;
            fillRenderer = gameObject.AddComponent<MeshRenderer>();
            fillRenderer.sharedMaterial = material;
            fillRenderer.sortingOrder = BaseFillOrder;

            // --- Sınır çizgileri (MeshTopology.Lines: yakınlaştırmadan bağımsız 1px çizgi) ---
            rings = new Vector2[data.rings.Length][];
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            var lineVerts = new System.Collections.Generic.List<Vector3>();
            var lineIdx = new System.Collections.Generic.List<int>();
            for (int r = 0; r < data.rings.Length; r++)
            {
                float[] p = data.rings[r].points;
                int n = p.Length / 2;
                var ring = new Vector2[n];
                int start = lineVerts.Count;
                for (int i = 0; i < n; i++)
                {
                    var pt = new Vector2(p[i * 2], p[i * 2 + 1]);
                    ring[i] = pt;
                    minX = Mathf.Min(minX, pt.x); maxX = Mathf.Max(maxX, pt.x);
                    minY = Mathf.Min(minY, pt.y); maxY = Mathf.Max(maxY, pt.y);
                    lineVerts.Add(new Vector3(pt.x - pivot.x, pt.y - pivot.y, 0f));
                    lineIdx.Add(start + i);
                    lineIdx.Add(start + (i + 1) % n);
                }
                rings[r] = ring;
            }
            WorldRect = Rect.MinMaxRect(minX, minY, maxX, maxY);

            borderColors = new Color[lineVerts.Count];
            for (int i = 0; i < borderColors.Length; i++) borderColors[i] = palette.border;

            borderMesh = new Mesh { name = data.name + "_Border" };
            borderMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            borderMesh.SetVertices(lineVerts);
            borderMesh.SetColors(borderColors);
            borderMesh.SetIndices(lineIdx, MeshTopology.Lines, 0);

            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(transform, false);
            borderGo.AddComponent<MeshFilter>().sharedMesh = borderMesh;
            borderRenderer = borderGo.AddComponent<MeshRenderer>();
            borderRenderer.sharedMaterial = material;
            borderRenderer.sortingOrder = BaseFillOrder + 1;

            ResetVisual();
        }

        /// <summary>Harita koordinatındaki nokta bu ilin içinde mi? (çift-tek kuralı; çoklu poligon ve delikleri destekler)</summary>
        public bool Contains(Vector2 p)
        {
            if (!WorldRect.Contains(p)) return false;
            bool inside = false;
            foreach (var ring in rings)
            {
                for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                {
                    Vector2 a = ring[i], b = ring[j];
                    if ((a.y > p.y) != (b.y > p.y) &&
                        p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                        inside = !inside;
                }
            }
            return inside;
        }

        public void ResetVisual()
        {
            State = ProvinceState.Idle;
            hovered = false;
            dimmed = false;
            neutral = false;
            uniformDim = false;
            if (borderRenderer) borderRenderer.enabled = true;
            focused = false;
            flashTime = 0f;
            popTime = -1f;
            transform.localScale = Vector3.one;
            SetRaised(false);
            smoothedColor = baseColor;
            Apply(baseColor, true);
        }

        public void SetState(ProvinceState state) => State = state;
        public void SetHovered(bool value) => hovered = value;
        public void SetDimmed(bool value) => dimmed = value;

        /// <summary>Nötr görünüm: bölge rengi gizlenir (cevabı ele vermesin) ama il tıklanabilir kalır.</summary>
        public void SetNeutral(bool value) => neutral = value;

        /// <summary>İl içi sınır çizgisini göster/gizle (yer şekli haritalarında il sınırları görünmez).</summary>
        public void SetBordersVisible(bool visible)
        {
            if (borderRenderer) borderRenderer.enabled = visible;
        }

        /// <summary>Soluk (odak dışı) iller bölge tonu taşımadan tek düz renk olsun (il kimliği sezdirilmesin).</summary>
        public void SetUniformDim(bool value) => uniformDim = value;

        /// <summary>Bölge modunda sorulan ili cevaplanana kadar nabız gibi vurgular.</summary>
        public void SetFocused(bool value)
        {
            focused = value;
            focusTime = 0f;
            SetRaised(value || flashTime > 0f);
        }

        public void PlayCorrect()
        {
            Flash(palette.correctFlash, 0.45f, 1);
            popTime = 0f;
        }

        public void PlayWrongClick() => Flash(palette.wrongFlash, 0.6f, 2);

        public void PlayReveal(float duration) => Flash(palette.reveal, duration, 3);

        void Flash(Color color, float duration, int pulses)
        {
            flashColor = color;
            flashDuration = duration;
            flashTime = duration;
            flashPulses = Mathf.Max(1, pulses);
            SetRaised(true);
        }

        void SetRaised(bool raised)
        {
            int order = raised ? RaisedFillOrder : BaseFillOrder;
            if (fillRenderer) fillRenderer.sortingOrder = order;
            if (borderRenderer) borderRenderer.sortingOrder = order + 1;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            Color target = State switch
            {
                ProvinceState.Correct => correctColor,
                ProvinceState.Missed => palette.missed,
                _ => baseColor
            };
            if (dimmed && State == ProvinceState.Idle) target = Color.Lerp(baseColor, palette.dim, uniformDim ? 1f : 0.82f);
            else if (neutral && State == ProvinceState.Idle) target = Color.Lerp(baseColor, palette.dim, 0.78f);
            float exam = ExamLook.Blend;
            if (exam > 0f && State == ProvinceState.Idle)
                target = Color.Lerp(target, dimmed && !uniformDim ? palette.examDim : palette.examLand, exam);
            if (hovered) target = Color.Lerp(target, palette.hover, 0.5f * (1f - exam)); // sınavda ipucu yok

            if (!Mathf.Approximately(exam, lastBlend))
            {
                lastBlend = exam;
                Color bc = Color.Lerp(palette.border, palette.examBorder, exam);
                for (int i = 0; i < borderColors.Length; i++) borderColors[i] = bc;
                borderMesh.colors = borderColors;
            }

            smoothedColor = Color.Lerp(smoothedColor, target, 1f - Mathf.Exp(-20f * dt));
            if (Mathf.Abs(smoothedColor.r - target.r) + Mathf.Abs(smoothedColor.g - target.g) +
                Mathf.Abs(smoothedColor.b - target.b) + Mathf.Abs(smoothedColor.a - target.a) < 0.004f)
                smoothedColor = target;
            Color final = smoothedColor;

            if (focused)
            {
                focusTime += dt;
                float w = 0.5f + 0.5f * Mathf.Sin(focusTime * 6f);
                final = Color.Lerp(smoothedColor, palette.reveal, 0.35f + 0.5f * w);
            }

            if (flashTime > 0f)
            {
                flashTime -= dt;
                float t = 1f - Mathf.Clamp01(flashTime / flashDuration);
                float w = 0.5f - 0.5f * Mathf.Cos(t * flashPulses * Mathf.PI * 2f);
                final = Color.Lerp(smoothedColor, flashColor, w);
                if (flashTime <= 0f && popTime < 0f && !focused) SetRaised(false);
            }

            if (popTime >= 0f)
            {
                popTime += dt;
                float t = Mathf.Clamp01(popTime / PopDuration);
                float s = 1f + 0.18f * Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);
                transform.localScale = new Vector3(s, s, 1f);
                if (t >= 1f)
                {
                    popTime = -1f;
                    transform.localScale = Vector3.one;
                    if (flashTime <= 0f && !focused) SetRaised(false);
                }
            }

            Apply(final, false);
        }

        void Apply(Color c, bool force)
        {
            if (!force && c == appliedColor) return;
            appliedColor = c;
            for (int i = 0; i < fillColors.Length; i++) fillColors[i] = c;
            fillMesh.colors = fillColors;
        }

        void OnDestroy()
        {
            if (fillMesh) Destroy(fillMesh);
            if (borderMesh) Destroy(borderMesh);
        }
    }
}
