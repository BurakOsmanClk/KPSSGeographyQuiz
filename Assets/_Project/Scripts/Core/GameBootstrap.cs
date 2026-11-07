using KPSSGeography.Ads;
using KPSSGeography.Data;
using KPSSGeography.Map;
using KPSSGeography.Meta;
using KPSSGeography.Quiz;
using KPSSGeography.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace KPSSGeography.Core
{
    /// <summary>
    /// Prototip sahnesinin tek giriş noktası. Sahnede boş bir GameObject'e eklenmesi yeterli:
    /// harita, kamera, giriş, ses, oyun akışı ve tüm arayüz çalışma anında kurulur.
    /// Kurallar ve renkler bu bileşenin Inspector'ından ayarlanabilir.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        /// <summary>
        /// Geliştirme süresince tüm bölümler açık. Kilit sistemini geri getirmek için false yap
        /// (ya da Inspector'da QuizConfig > Unlock All Levels kutusunu kullan).
        /// </summary>
        const bool UnlockAllLevelsForTesting = true;

        [SerializeField] QuizConfig quizConfig = new QuizConfig();
        [SerializeField] MapPalette palette = new MapPalette();

        /// <summary>
        /// Sahnede GameBootstrap yoksa (ör. SampleScene'de Play'e basıldıysa) otomatik oluşturur.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (Object.FindAnyObjectByType<GameBootstrap>() != null) return;
            Debug.Log("[KPSS] Sahnede GameBootstrap bulunamadı, otomatik oluşturuluyor.");
            new GameObject("Game (Auto)").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            Debug.Log($"[KPSS] Oyun başlatılıyor... {AppInfo.FullLabel}");
            Application.targetFrameRate = 120;
            Time.timeScale = 1f;

            var db = ProvinceDatabase.Load();
            if (db == null)
            {
                enabled = false;
                return;
            }
            var catalog = LevelCatalog.Load();
            var progress = new ProgressStore(catalog) { UnlockAll = UnlockAllLevelsForTesting || quizConfig.unlockAllLevels };

            Camera cam = SetupCamera();
            EnsureEventSystem();

            // Harita
            var map = new GameObject("TurkeyMap").AddComponent<TurkeyMap>();
            map.Build(db, palette);

            // Gölgeli kabartma (gerçek yükseklik verisinden)
            var relief = new GameObject("Relief").AddComponent<ReliefLayer>();
            relief.Build(map.Bounds, map.SharedMaterial.shader);

            // Efektler (bölge sınırı çizimi, ışıltı)
            var effects = new GameObject("MapEffects").AddComponent<MapEffects>();
            effects.Initialize(map.SharedMaterial);

            // Fiziki harita (göller, akarsular, dağlar, volkanlar, ovalar, platolar)
            var features = new GameObject("PhysicalFeatures").AddComponent<FeatureLayer>();
            features.Build(GeoFeatureDatabase.Load(), map.SharedMaterial, palette);

            // Kamera
            var camController = cam.GetComponent<MapCameraController>();
            if (camController == null) camController = cam.gameObject.AddComponent<MapCameraController>();
            camController.Setup(map.Bounds);

            // Ses
            var sfx = new GameObject("Sfx").AddComponent<SfxPlayer>();

            // Oyun
            var game = new GameObject("CityQuizGame").AddComponent<CityQuizGame>();
            var builder = new QuestionBuilder(map, features, TopicDatabase.Load(), McqDatabase.Load(), palette);
            game.Initialize(quizConfig, map, features, sfx, camController, progress, builder, relief, effects);

            var pointer = map.gameObject.AddComponent<MapPointer>();
            pointer.Initialize(map, cam);
            pointer.CanInteract = () => game.AcceptsMapInput;
            pointer.ProvinceClicked += game.SubmitProvince;

            var featurePointer = features.gameObject.AddComponent<FeaturePointer>();
            featurePointer.Initialize(features, cam);
            featurePointer.CanInteract = () => game.AcceptsFeatureInput;
            featurePointer.FeatureClicked += game.SubmitFeature;

            // Arayüz
            var canvas = CreateCanvas(out RectTransform uiRoot);
            // Harita yazıları (deniz/ülke adları, il adları, inceleme etiketleri)
            var labels = new GameObject("MapLabelsController").AddComponent<MapLabelLayer>();
            labels.Initialize(cam, () => camController.FullMapSize, map);

            var hud = new GameObject("QuizHudController").AddComponent<QuizHud>();
            hud.Labels = labels;
            hud.Initialize(uiRoot, uiRoot, game, palette, cam, sfx);
            var levels = new GameObject("LevelSelectController").AddComponent<LevelSelectScreen>();
            levels.Initialize(uiRoot, canvas, catalog, progress, sfx);
            var title = new GameObject("TitleController").AddComponent<TitleScreen>();
            title.Initialize(uiRoot, progress, sfx);

            // Reklamlar: Ads Mediation (LevelPlay) kurulana kadar test reklamı
            var mockAds = new GameObject("MockAdsController").AddComponent<MockAdService>();
            mockAds.Initialize(uiRoot, sfx);
            var adPolicy = new AdPolicy(mockAds, quizConfig);

            // Erişilebilirlik ayarları: renk körü paleti ve büyük yazı
            void ApplySettings()
            {
                Theme.ApplyColorblind(GameSettings.Colorblind);
                palette.ApplyColorblind(GameSettings.Colorblind);
                TextScaler.Apply(uiRoot);
            }
            ApplySettings();
            GameSettings.Changed += ApplySettings;
            settingsHandler = ApplySettings;

            var flow = gameObject.AddComponent<AppFlow>();
            flow.Initialize(game, hud, title, levels, catalog, progress, adPolicy);

            Debug.Log($"[KPSS] Hazır: {map.Provinces.Count} il, {features.Features.Count} yer şekli, {catalog.levels.Length} bölüm yüklendi.");
        }

        static Canvas CreateCanvas(out RectTransform root)
        {
            var go = new GameObject("UI", typeof(RectTransform));
            go.layer = 5;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            root = (RectTransform)go.transform;
            return canvas;
        }

        Camera SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.identity;
            var urpData = cam.GetUniversalAdditionalCameraData();
            if (urpData != null) urpData.renderPostProcessing = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = palette.seaBottom;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            if (Object.FindAnyObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            return cam;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        System.Action settingsHandler;

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (settingsHandler != null) GameSettings.Changed -= settingsHandler;
        }
    }
}
