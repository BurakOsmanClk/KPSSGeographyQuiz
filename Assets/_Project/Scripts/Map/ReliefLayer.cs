using KPSSGeography.Core;
using UnityEngine;

namespace KPSSGeography.Map
{
    /// <summary>
    /// Gölgeli kabartma (hillshade) katmanı. Doku (Resources/Textures/turkey_relief.png, Tools/geo/build_relief.py)
    /// harita sınırına birebir oturur; illerin üstüne, göl ve yer şekillerinin altına yarı saydam çizilir.
    /// Gücü bölüm türüne göre değişir (yer şekli bölümlerinde daha belirgin).
    /// </summary>
    public class ReliefLayer : MonoBehaviour
    {
        public const string ResourcePath = "Textures/turkey_relief";

        Mesh mesh;
        Color[] colors = new Color[4];
        float strength = 0.8f;
        float target = 0.8f;
        float lastBlend;

        public bool IsAvailable { get; private set; }

        public void Build(Rect bounds, Shader shader)
        {
            var tex = Resources.Load<Texture2D>(ResourcePath);
            if (tex == null)
            {
                Debug.LogWarning($"[KPSS] Kabartma dokusu bulunamadı: Resources/{ResourcePath}.png");
                return;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            IsAvailable = true;

            mesh = new Mesh { name = "Relief" };
            mesh.vertices = new[]
            {
                new Vector3(bounds.xMin, bounds.yMin, 0f), new Vector3(bounds.xMax, bounds.yMin, 0f),
                new Vector3(bounds.xMin, bounds.yMax, 0f), new Vector3(bounds.xMax, bounds.yMax, 0f)
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            Apply();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterial = new Material(shader) { name = "Relief", mainTexture = tex };
            r.sortingOrder = 2;
        }

        /// <summary>0 = görünmez, 1 = tam güç. Yumuşak geçişle uygulanır.</summary>
        public void SetStrength(float value) => target = Mathf.Clamp01(value);

        void Update()
        {
            if (mesh == null) return;
            float blend = ExamLook.Blend;
            if (Mathf.Approximately(strength, target) && Mathf.Approximately(blend, lastBlend)) return;
            strength = Mathf.MoveTowards(strength, target, Time.unscaledDeltaTime * 1.5f);
            lastBlend = blend;
            Apply();
        }

        void Apply()
        {
            // Sınav haritası "boş" olmalı: kabartma neredeyse tamamen kaybolur
            float a = strength * (1f - 0.88f * ExamLook.Blend);
            for (int i = 0; i < 4; i++) colors[i] = new Color(1f, 1f, 1f, a);
            mesh.colors = colors;
        }
    }
}
