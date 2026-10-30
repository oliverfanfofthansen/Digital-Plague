using System.IO;
using DigitalPlague.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalPlague.Editor
{
    /// <summary>Opens the demo or creates its assets when they do not exist.</summary>
    public static class FoundationSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DigitalPlagueFoundation.unity";
        private const string UiPath = "Assets/DigitalPlague/UI/";

        [MenuItem("Digital Plague/Open Foundation Scene")]
        public static void OpenFoundationScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            OpenOrCreateScene();
        }

        // Also usable with Unity's -executeMethod for automated validation.
        public static void OpenOrCreateScene()
        {
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiPath + "SimulationTheme.tss");
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "SimulationHud.uxml");
            if (theme == null || layout == null)
            {
                throw new System.InvalidOperationException("Import the Digital Plague UI assets first.");
            }

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPath + "SimulationPanelSettings.asset");
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.themeStyleSheet = theme;
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1280, 720);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = 0.5f;
                AssetDatabase.CreateAsset(panel, UiPath + "SimulationPanelSettings.asset");
                AssetDatabase.SaveAssets();
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(0.047f, 0.075f, 0.11f);

            var manager = new GameObject("GameManager").AddComponent<GameManager>();
            var hudObject = new GameObject("Simulation UI", typeof(UIDocument));
            var document = hudObject.GetComponent<UIDocument>();
            var hud = hudObject.AddComponent<SimulationHud>();
            var serializedDocument = new SerializedObject(document);
            serializedDocument.FindProperty("m_PanelSettings").objectReferenceValue = panel;
            serializedDocument.FindProperty("sourceAsset").objectReferenceValue = layout;
            serializedDocument.ApplyModifiedPropertiesWithoutUndo();

            var serializedHud = new SerializedObject(hud);
            serializedHud.FindProperty("gameManager").objectReferenceValue = manager;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
    }
}
