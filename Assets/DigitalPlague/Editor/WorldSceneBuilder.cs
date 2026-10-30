using System.Collections.Generic;
using System.IO;
using DigitalPlague.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalPlague.Editor
{
    public static class WorldSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DigitalPlagueWorld.unity";
        public const string PrefabPath = "Assets/DigitalPlague/Prefabs/WorldGame.prefab";
        private const string PanelPath = "Assets/DigitalPlague/UI/WorldPanelSettings.asset";

        [MenuItem("Digital Plague/Open World Game")]
        public static void OpenWorldGame()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            OpenOrCreate();
        }

        // Safe to invoke in batch mode. Existing scenes/prefabs are never overwritten.
        public static void OpenOrCreate()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            else EditorSceneManager.OpenScene(ScenePath);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(scene => scene.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void CreateScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DigitalPlague/Prefabs"))
                AssetDatabase.CreateFolder("Assets/DigitalPlague", "Prefabs");

            if (!File.Exists(PanelPath))
            {
                var panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/DigitalPlague/UI/SimulationTheme.tss");
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1600, 900);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = .5f;
                AssetDatabase.CreateAsset(panel, PanelPath);
                AssetDatabase.SaveAssets();
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!File.Exists(PrefabPath)) CreatePrefab();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(.03f, .07f, .10f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void CreatePrefab()
        {
            var root = new GameObject("World Game");
            var manager = root.AddComponent<GameManager>();
            var managerData = new SerializedObject(manager);
            managerData.FindProperty("startInMainMenu").boolValue = true;
            managerData.ApplyModifiedPropertiesWithoutUndo();

            var document = root.AddComponent<UIDocument>();
            var ui = root.AddComponent<WorldInterface>();
            var documentData = new SerializedObject(document);
            documentData.FindProperty("m_PanelSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            documentData.FindProperty("sourceAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/DigitalPlague/UI/WorldGame.uxml");
            documentData.ApplyModifiedPropertiesWithoutUndo();
            var uiData = new SerializedObject(ui);
            uiData.FindProperty("gameManager").objectReferenceValue = manager;
            uiData.FindProperty("countryBoundaries").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/DigitalPlague/Data/WorldCountries.json");
            uiData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }
    }
}
