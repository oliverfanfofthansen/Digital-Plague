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
    public sealed class FoundationSceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadFoundationScene()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/DigitalPlagueFoundation.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneButtonsControlClockAndPauseFreezesElapsedTime()
        {
            var manager = Object.FindAnyObjectByType<GameManager>();
            var document = Object.FindAnyObjectByType<UIDocument>();
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.Regions.Count, Is.EqualTo(4));
            VisualElement root = document.rootVisualElement;
            Assert.That(root.Q("region-list").childCount, Is.EqualTo(4));

            foreach (var entry in new[]
            {
                ("double-button", SimulationSpeed.Double, "2x"),
                ("fast-button", SimulationSpeed.Fast, "5x"),
                ("normal-button", SimulationSpeed.Normal, "1x"),
                ("pause-button", SimulationSpeed.Paused, "Paused")
            })
            {
                Submit(root.Q<Button>(entry.Item1));
                // UI Toolkit dispatches submitted input through the panel update.
                yield return null;
                Assert.That(manager.Clock.Speed, Is.EqualTo(entry.Item2));
                Assert.That(root.Q<Label>("speed-label").text, Is.EqualTo(entry.Item3));
            }

            Assert.That(manager.State, Is.EqualTo(GameState.Paused));
            long pausedTicks = manager.Clock.TickCount;
            string pausedTime = root.Q<Label>("elapsed-label").text;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(manager.Clock.TickCount, Is.EqualTo(pausedTicks));
            Assert.That(root.Q<Label>("elapsed-label").text, Is.EqualTo(pausedTime));

            Submit(root.Q<Button>("fast-button"));
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(manager.State, Is.EqualTo(GameState.Running));
            Assert.That(manager.Clock.TickCount, Is.GreaterThan(pausedTicks));
            Assert.That(root.Q<Label>("elapsed-label").text, Is.Not.EqualTo(pausedTime));
        }

        [UnityTest]
        public IEnumerator ClockRunsWithUnityTimeScaleZeroAndHudCanBeReenabled()
        {
            var manager = Object.FindAnyObjectByType<GameManager>();
            var hud = Object.FindAnyObjectByType<SimulationHud>();
            var document = hud.GetComponent<UIDocument>();
            Time.timeScale = 0;
            manager.SetSimulationSpeed(SimulationSpeed.Fast);
            long before = manager.Clock.TickCount;
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(manager.Clock.TickCount, Is.GreaterThan(before));

            hud.enabled = false;
            manager.SetSimulationSpeed(SimulationSpeed.Paused);
            hud.enabled = true;
            Assert.That(document.rootVisualElement.Q<Label>("speed-label").text, Is.EqualTo("Paused"));
            Assert.That(document.rootVisualElement.Q("region-list").childCount, Is.EqualTo(4));
        }

        private static void Submit(Button button)
        {
            Assert.That(button, Is.Not.Null);
            button.Focus();
            using (var submitEvent = NavigationSubmitEvent.GetPooled())
            {
                button.SendEvent(submitEvent);
            }
        }
    }
}
#endif
