#if UNITY_EDITOR
using System.Collections;
using DigitalPlague.Core;
using DigitalPlague.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DigitalPlague.Tests
{
    public sealed class WorldGameTests
    {
        private GameManager manager;
        private WorldInterface ui;
        private VisualElement root;
        private GamePreferences previousPreferences;

        [UnitySetUp]
        public IEnumerator LoadWorld()
        {
            previousPreferences = GamePreferences.Load();
            EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/DigitalPlagueWorld.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            manager = Object.FindAnyObjectByType<GameManager>();
            ui = Object.FindAnyObjectByType<WorldInterface>();
            root = ui.GetComponent<UIDocument>().rootVisualElement;
        }

        [UnityTearDown]
        public IEnumerator RestorePreferences()
        {
            previousPreferences?.Save();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTest]
        public IEnumerator MenuOriginSpreadInspectionPauseAndRestartWorkTogether()
        {
            Assert.That(manager.State, Is.EqualTo(GameState.MainMenu));
            Assert.That(manager.Clock.TickCount, Is.Zero);
            Submit("new-game-button");
            yield return null;
            Assert.That(manager.State, Is.EqualTo(GameState.SelectingStart));
            Assert.That(root.Q<Button>("start-game-button").enabledSelf, Is.False);
            Assert.That(manager.StartGame(), Is.False);
            foreach (LocationState location in manager.World.Locations)
            {
                Submit("location-" + location.Data.Id);
                yield return null;
                Assert.That(root.Q<Label>("location-name").text, Is.EqualTo(location.Data.DisplayName));
            }
            Submit("location-BRA");
            yield return null;
            Submit("start-game-button");
            yield return null;
            Assert.That(manager.World.OriginId, Is.EqualTo("BRA"));
            Assert.That(manager.World.ActiveLocations, Is.EqualTo(1));

            Submit("fast-button");
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(manager.Clock.Speed, Is.EqualTo(SimulationSpeed.Fast));
            Assert.That(manager.World.GetLocation("USA").InfluencedDevices, Is.GreaterThan(0));
            Submit("location-USA");
            yield return null;
            Assert.That(manager.SelectedLocationId, Is.EqualTo("USA"));
            Assert.That(root.Q<Label>("location-name").text, Is.EqualTo("United States"));
            foreach (string button in new[] { "normal-button", "double-button", "pause-button" })
            {
                Submit(button);
                yield return null;
            }
            Assert.That(manager.Clock.Speed, Is.EqualTo(SimulationSpeed.Paused));
            long paused = manager.Clock.TickCount;
            double total = manager.World.TotalInfluencedDevices;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(manager.Clock.TickCount, Is.EqualTo(paused));
            Assert.That(manager.World.TotalInfluencedDevices, Is.EqualTo(total));
            Submit("main-menu-button");
            yield return null;
            Submit("new-game-button");
            yield return null;
            Assert.That(manager.World.TotalInfluencedDevices, Is.Zero);
            Assert.That(manager.Clock.TickCount, Is.Zero);
            Assert.That(manager.SelectedLocationId, Is.Null);
        }

        [UnityTest]
        public IEnumerator MapHasGeographyAndZoomPanResetWithoutChangingSimulation()
        {
            Submit("new-game-button");
            yield return null;
            yield return null;
            Assert.That(root.Query<Button>(className: "map-marker").ToList().Count, Is.EqualTo(10));
            Rect map = ui.Map.worldBound;
            Rect screen = root.worldBound;
            Assert.That(map.width * map.height / (screen.width * screen.height), Is.GreaterThan(.65f));
            Vector2 brazil = ui.Map.Project(manager.World.GetLocation("BRA").Data.Coordinates);
            Assert.That(brazil.x, Is.InRange(0, map.width));
            Submit("zoom-in-button");
            yield return null;
            Assert.That(ui.Map.ZoomLevel, Is.GreaterThan(1));
            ui.Map.PanBy(new Vector2(50, 20));
            Assert.That(ui.Map.PanOffset.sqrMagnitude, Is.GreaterThan(0));
            Submit("reset-map-button");
            yield return null;
            Assert.That(ui.Map.ZoomLevel, Is.EqualTo(1));
            Assert.That(ui.Map.PanOffset, Is.EqualTo(Vector2.zero));
            Assert.That(manager.World.TotalInfluencedDevices, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SettingsPauseAndRestoreSpeedAndApplyRealMapPreferences()
        {
            Submit("new-game-button");
            yield return null;
            Submit("location-IND");
            yield return null;
            Submit("start-game-button");
            yield return null;
            Submit("double-button");
            yield return null;
            Submit("game-settings-button");
            yield return null;
            Assert.That(manager.Clock.Speed, Is.EqualTo(SimulationSpeed.Paused));
            root.Q<Toggle>("show-routes-toggle").value = false;
            root.Q<Toggle>("high-contrast-toggle").value = true;
            root.Q<Slider>("zoom-sensitivity-slider").value = 1.4f;
            Submit("apply-settings-button");
            yield return null;
            Assert.That(manager.Clock.Speed, Is.EqualTo(SimulationSpeed.Double));
            Assert.That(ui.Map.ShowConnections, Is.False);
            Assert.That(ui.Map.HighContrast, Is.True);
            Assert.That(ui.Map.ZoomSensitivity, Is.EqualTo(1.4f).Within(.001f));
        }

        private void Submit(string name)
        {
            Button button = root.Q<Button>(name);
            Assert.That(button, Is.Not.Null, name);
            button.Focus();
            using (var evt = NavigationSubmitEvent.GetPooled()) button.SendEvent(evt);
        }
    }
}
#endif
