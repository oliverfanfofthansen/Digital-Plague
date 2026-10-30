using System;
using System.Globalization;
using DigitalPlague.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalPlague.UI
{
    /// <summary>Displays simulation data and translates UI input into commands.</summary>
    [RequireComponent(typeof(UIDocument))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // Bind after UIDocument creates its visual tree.
    public sealed class SimulationHud : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        private SimulationClock clock;
        private Label speedLabel;
        private Label elapsedLabel;
        private Label tickLabel;
        private Label stateLabel;
        private Button pauseButton;
        private Button normalButton;
        private Button doubleButton;
        private Button fastButton;

        private void OnEnable()
        {
            if (gameManager == null)
            {
                Debug.LogError("SimulationHud needs a GameManager reference.", this);
                enabled = false;
                return;
            }

            gameManager.Initialize();
            clock = gameManager.Clock;
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            speedLabel = Require<Label>(root, "speed-label");
            elapsedLabel = Require<Label>(root, "elapsed-label");
            tickLabel = Require<Label>(root, "tick-label");
            stateLabel = Require<Label>(root, "state-label");
            pauseButton = Require<Button>(root, "pause-button");
            normalButton = Require<Button>(root, "normal-button");
            doubleButton = Require<Button>(root, "double-button");
            fastButton = Require<Button>(root, "fast-button");

            pauseButton.clicked += Pause;
            normalButton.clicked += SetNormalSpeed;
            doubleButton.clicked += SetDoubleSpeed;
            fastButton.clicked += SetFastSpeed;
            clock.Tick += HandleTick;
            clock.SpeedChanged += HandleSpeedChanged;

            VisualElement regionList = Require<VisualElement>(root, "region-list");
            regionList.Clear();
            foreach (RegionData region in gameManager.Regions)
            {
                var row = new VisualElement { tooltip = region.Id };
                row.AddToClassList("region-row");
                row.Add(CreateCell(region.DisplayName, "region-name"));
                row.Add(CreateCell(region.TotalDevices.ToString("N0", CultureInfo.InvariantCulture)));
                row.Add(CreateCell(region.Connectivity.ToString("P0", CultureInfo.InvariantCulture)));
                row.Add(CreateCell(region.SecurityLevel.ToString("P0", CultureInfo.InvariantCulture)));
                regionList.Add(row);
            }

            RefreshStatus();
        }

        private void OnDisable()
        {
            if (clock != null)
            {
                clock.Tick -= HandleTick;
                clock.SpeedChanged -= HandleSpeedChanged;
                clock = null;
            }

            if (pauseButton != null) pauseButton.clicked -= Pause;
            if (normalButton != null) normalButton.clicked -= SetNormalSpeed;
            if (doubleButton != null) doubleButton.clicked -= SetDoubleSpeed;
            if (fastButton != null) fastButton.clicked -= SetFastSpeed;
        }

        private void Pause() => gameManager.SetSimulationSpeed(SimulationSpeed.Paused);
        private void SetNormalSpeed() => gameManager.SetSimulationSpeed(SimulationSpeed.Normal);
        private void SetDoubleSpeed() => gameManager.SetSimulationSpeed(SimulationSpeed.Double);
        private void SetFastSpeed() => gameManager.SetSimulationSpeed(SimulationSpeed.Fast);
        private void HandleTick(SimulationTick tick) => RefreshStatus();
        private void HandleSpeedChanged(SimulationSpeed speed) => RefreshStatus();

        private void RefreshStatus()
        {
            bool paused = clock.Speed == SimulationSpeed.Paused;
            speedLabel.text = paused ? "Paused" : $"{(int)clock.Speed}x";
            stateLabel.text = paused ? "Simulation paused" : "Simulation running";

            TimeSpan elapsed = TimeSpan.FromSeconds(clock.ElapsedSeconds);
            elapsedLabel.text = $"Day {elapsed.Days + 1:000}  {elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            tickLabel.text = $"{clock.TickCount:N0} ticks  /  {clock.TickDurationSeconds:0.###} sim seconds per tick";

            pauseButton.EnableInClassList("is-selected", paused);
            normalButton.EnableInClassList("is-selected", clock.Speed == SimulationSpeed.Normal);
            doubleButton.EnableInClassList("is-selected", clock.Speed == SimulationSpeed.Double);
            fastButton.EnableInClassList("is-selected", clock.Speed == SimulationSpeed.Fast);
        }

        private static Label CreateCell(string text, string className = "numeric-cell")
        {
            var label = new Label(text);
            label.AddToClassList("region-cell");
            label.AddToClassList(className);
            return label;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"SimulationHud UXML is missing '{name}' ({typeof(T).Name}).");
        }
    }
}
