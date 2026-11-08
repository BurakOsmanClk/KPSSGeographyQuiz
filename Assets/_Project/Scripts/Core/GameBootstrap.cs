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
            var topicDb = TopicDatabase.Load();
            var builder = new QuestionBuilder(map, features, topicDb, McqDatabase.Load(), palette);
            game.Initialize(quizConfig, map, features, sfx, camController, progress, builder, relief, effects);

            var pointer = map.gameObject.AddComponent<MapPointer>();
            pointer.Initialize(map, cam);
            LearnScreen learn = null; // aşağıda kurulur; dokunuşlar "Öğren" ekranı açıkken ona gider
            pointer.CanInteract = () => game.AcceptsMapInput || (learn != null && learn.AcceptsProvinceInput);
            pointer.ProvinceClicked += p =>
            {
                if (learn != null && learn.AcceptsProvinceInput) learn.OnProvinceClicked(p);
                else game.SubmitProvince(p);
            };

            var featurePointer = features.gameObject.AddComponent<FeaturePointer>();
            featurePointer.Initialize(features, cam);
            featurePointer.CanInteract = () => game.AcceptsFeatureInput || (learn != null && learn.AcceptsFeatureInput);
            featurePointer.FeatureClicked += f =>
            {
                if (learn != null && learn.AcceptsFeatureInput) learn.OnFeatureClicked(f);
                else game.SubmitFeature(f);
            };

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
            // Öğren: süresiz konu anlatımı (iklim, yer şekilleri, sular, ekonomi, beşeri)
            learn = new GameObject("LearnController").AddComponent<LearnScreen>();
            learn.Labels = labels;
            learn.Initialize(uiRoot, map, features, relief, effects, camController, sfx, topicDb, LessonDatabase.Load());

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

            // Sınav modu: renkli haritanın soluk gri KPSS haritasına "buharlaşarak" dönüşmesi
            var exam = new GameObject("ExamModeController").AddComponent<ExamModeController>();
            exam.Initialize(camController, sfx);

            var flow = gameObject.AddComponent<AppFlow>();
            flow.Initialize(game, hud, title, levels, learn, catalog, progress, adPolicy, exam);

            Debug.Log($"[KPSS] Hazır: {map.Provinces.Count} il, {features.Features.Count} yer şekli, {catalog.levels.Length} bölüm yüklendi.");
        }

        static Canvas CreateCanvas(out RectTransform root)
        {
            var go = new GameObject("UI", typeof(RectTransform));
            go.layer = 5;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            // Güvenli alan + ölçek: içerik çentik/kamera deliği dışında kalır ve güvenli alan her zaman en az
            // 1920×1080 birimdir; her telefonda, tablette ve pencere boyutunda ekranlar üst üste binmez.
            root = go.AddComponent<SafeAreaFitter>().Initialize();
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
