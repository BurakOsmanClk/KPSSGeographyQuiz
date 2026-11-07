using System.IO;
using System.Linq;
using KPSSGeography.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KPSSGeography.EditorTools
{
    /// <summary>Menü: KPSS > Prototip Sahnesini Oluştur. Sahneyi kurar, kaydeder ve Build Profiles listesine ekler.</summary>
    public static class PrototypeSceneCreator
    {
        const string ScenePath = "Assets/_Project/Scenes/CityQuizPrototype.unity";

        [MenuItem("KPSS/Prototip Sahnesini Oluştur", priority = 0)]
        public static void CreateScene()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Sahne zaten var",
                    $"{ScenePath} zaten mevcut. Üzerine yazılsın mı?", "Evet, yeniden oluştur", "İptal"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x8E, 0xC1, 0xDD, 0xFF);
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            new GameObject("Game").AddComponent<GameBootstrap>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log($"[KPSS] Prototip sahnesi oluşturuldu: {ScenePath}. Play'e basarak oynayabilirsin.");
        }

        [MenuItem("KPSS/Prototip Sahnesini Aç", priority = 1)]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath))
            {
                CreateScene();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
