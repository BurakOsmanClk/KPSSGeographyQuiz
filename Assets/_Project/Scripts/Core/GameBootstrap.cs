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
            Debug.Log("[KPSS] Oyun başlatılıyor...");
            Application.targetFrameRate = 120;
            Time.timeScale = 1f;

            var db = ProvinceDatabase.Load();
            if (db == null)
            {
                enabled = false;
                return;
            }
            var catalog = LevelCatalog.Load();
            var progress = new ProgressStore(catalog) { UnlockAll = quizConfig.unlockAllLevels };

            Camera cam = SetupCamera();
            EnsureEventSystem();

            // Harita
            var map = new GameObject("TurkeyMap").AddComponent<TurkeyMap>();
            map.Build(db, palette);

            // Kamera
            var camController = cam.GetComponent<MapCameraController>();
            if (camController == null) camController = cam.gameObject.AddComponent<MapCameraController>();
            camController.Setup(map.Bounds);

            // Ses
            var sfx = new GameObject("Sfx").AddComponent<SfxPlayer>();

            // Oyun
            var game = new GameObject("CityQuizGame").AddComponent<CityQuizGame>();
            game.Initialize(quizConfig, map, sfx, camController, progress);

            var pointer = map.gameObject.AddComponent<MapPointer>();
            pointer.Initialize(map, cam);
            pointer.CanInteract = () => game.AcceptsMapInput;
            pointer.ProvinceClicked += game.SubmitProvince;

            // Arayüz
            var canvas = CreateCanvas(out RectTransform uiRoot);
            var hud = new GameObject("QuizHudController").AddComponent<QuizHud>();
            hud.Initialize(uiRoot, uiRoot, game, palette, cam, sfx);
            var levels = new GameObject("LevelSelectController").AddComponent<LevelSelectScreen>();
            levels.Initialize(uiRoot, canvas, catalog, progress, sfx);
            var title = new GameObject("TitleController").AddComponent<TitleScreen>();
            title.Initialize(uiRoot, progress, sfx);

            var flow = gameObject.AddComponent<AppFlow>();
            flow.Initialize(game, hud, title, levels, catalog, progress);

            Debug.Log($"[KPSS] Hazır: {map.Provinces.Count} il, {catalog.levels.Length} bölüm yüklendi.");
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

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
