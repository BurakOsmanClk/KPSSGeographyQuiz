using KPSSGeography.Data;
using KPSSGeography.Map;
using KPSSGeography.Quiz;
using KPSSGeography.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace KPSSGeography.Core
{
    /// <summary>
    /// Prototip sahnesinin tek giriş noktası. Sahnede boş bir GameObject'e eklenmesi yeterli:
    /// harita, kamera kontrolü, giriş, ses, oyun akışı ve arayüz çalışma anında kurulur.
    /// Kurallar ve renkler bu bileşenin Inspector'ından ayarlanabilir.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] QuizConfig quizConfig = new QuizConfig();
        [SerializeField] MapPalette palette = new MapPalette();

        /// <summary>
        /// Sahnede GameBootstrap yoksa (ör. SampleScene'de Play'e basıldıysa) otomatik oluşturur.
        /// Böylece prototip hangi sahnede olursa olsun Play ile başlar.
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

            var db = ProvinceDatabase.Load();
            if (db == null)
            {
                enabled = false;
                return;
            }

            Camera cam = SetupCamera();
            EnsureEventSystem();

            // Harita
            var map = new GameObject("TurkeyMap").AddComponent<TurkeyMap>();
            map.Build(db, palette);

            // Kamera kontrolü
            var camController = cam.GetComponent<MapCameraController>();
            if (camController == null) camController = cam.gameObject.AddComponent<MapCameraController>();
            camController.Setup(map.Bounds);

            // Ses
            var sfx = new GameObject("Sfx").AddComponent<SfxPlayer>();

            // Oyun akışı
            var game = new GameObject("CityQuizGame").AddComponent<CityQuizGame>();
            game.Initialize(quizConfig, map, sfx, camController);

            // Fare ile seçim
            var pointer = map.gameObject.AddComponent<MapPointer>();
            pointer.Initialize(map, cam);
            pointer.CanInteract = () => game.AcceptsInput;
            pointer.ProvinceClicked += game.SubmitAnswer;

            // Arayüz
            var hud = new GameObject("QuizHUD").AddComponent<QuizHud>();
            hud.Initialize(game, quizConfig, cam, sfx);

            Debug.Log($"[KPSS] Hazır: {map.Provinces.Count} il yüklendi, shader: {map.SharedMaterial.shader.name}");
        }

        Camera SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.identity;
            var urpData = cam.GetUniversalAdditionalCameraData();
            if (urpData != null) urpData.renderPostProcessing = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = palette.sea;
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
    }
}
