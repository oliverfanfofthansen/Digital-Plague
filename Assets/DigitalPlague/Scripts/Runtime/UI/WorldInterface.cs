using System;
using System.Collections.Generic;
using System.Globalization;
using DigitalPlague.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalPlague.UI
{
    /// <summary>Game screens and presentation. Gameplay changes go through GameManager.</summary>
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(100)]
    public sealed class WorldInterface : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TextAsset countryBoundaries;

        private readonly List<Action> unsubscribe = new List<Action>();
        private readonly Dictionary<string, Label> labels = new Dictionary<string, Label>();
        private readonly HashSet<string> reportedLocations = new HashSet<string>();
        private readonly List<string> feed = new List<string>();
        private VisualElement root;
        private GamePreferences preferences;
        private SimulationSpeed speedBeforeSettings;
        private bool settingsOpen;
        private string connectionsFor;

        public WorldMapElement Map { get; private set; }

        private void OnEnable()
        {
            gameManager.Initialize();
            if (gameManager.World == null) throw new InvalidOperationException("WorldInterface needs a world-mode GameManager.");
            root = GetComponent<UIDocument>().rootVisualElement;
            labels.Clear();
            preferences = GamePreferences.Load();
            WorldMapGeometry geometry = WorldMapGeometry.Parse(countryBoundaries);

            Map = new WorldMapElement();
            Map.Configure(gameManager.World, geometry, gameManager.SelectLocation);
            Element("map-host").Clear();
            Element("map-host").Add(Map);
            var backdrop = new WorldMapElement(false);
            backdrop.Configure(gameManager.World, geometry, null);
            Element("menu-map-host").Clear();
            Element("menu-map-host").Add(backdrop);

            Bind("new-game-button", NewGame);
            Bind("start-game-button", () => gameManager.StartGame());
            Bind("main-menu-button", gameManager.ReturnToMainMenu);
            Bind("menu-settings-button", OpenSettings);
            Bind("game-settings-button", OpenSettings);
            Bind("apply-settings-button", () => CloseSettings(true));
            Bind("cancel-settings-button", () => CloseSettings(false));
            Bind("quit-button", QuitGame);
            Bind("pause-button", () => gameManager.SetSimulationSpeed(SimulationSpeed.Paused));
            Bind("normal-button", () => gameManager.SetSimulationSpeed(SimulationSpeed.Normal));
            Bind("double-button", () => gameManager.SetSimulationSpeed(SimulationSpeed.Double));
            Bind("fast-button", () => gameManager.SetSimulationSpeed(SimulationSpeed.Fast));
            Bind("zoom-in-button", Map.ZoomIn);
            Bind("zoom-out-button", Map.ZoomOut);
            Bind("reset-map-button", Map.ResetView);
            root.RegisterCallback<KeyDownEvent>(HandleKey);
            gameManager.WorldChanged += Refresh;
            ApplyPreferences();
            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.WorldChanged -= Refresh;
            foreach (Action detach in unsubscribe) detach();
            unsubscribe.Clear();
            root?.UnregisterCallback<KeyDownEvent>(HandleKey);
        }

        private void NewGame()
        {
            reportedLocations.Clear();
            feed.Clear();
            connectionsFor = null;
            Map.ResetView();
            gameManager.BeginNewGame();
        }

        private void Refresh()
        {
            bool menu = gameManager.State == GameState.MainMenu;
            bool choosing = gameManager.State == GameState.SelectingStart;
            bool playing = !menu && !choosing;
            Element("main-menu").style.display = menu ? DisplayStyle.Flex : DisplayStyle.None;
            Element("game-screen").style.display = menu ? DisplayStyle.None : DisplayStyle.Flex;
            Element("start-game-button").style.display = choosing ? DisplayStyle.Flex : DisplayStyle.None;
            Element("start-game-button").SetEnabled(gameManager.SelectedLocation != null);
            Element("speed-controls").SetEnabled(playing);
            Map.Refresh(gameManager.SelectedLocationId);

            WorldSimulation world = gameManager.World;
            Text("global-influence", Percent(world.InfluenceFraction));
            Text("device-total", Math.Floor(world.TotalInfluencedDevices).ToString("N0", CultureInfo.InvariantCulture));
            Text("active-total", $"{world.ActiveLocations} / {world.Locations.Count}");
            Text("map-phase", choosing ? "CHOOSE YOUR ORIGIN" : gameManager.State == GameState.Paused ? "GLOBAL NETWORK / PAUSED" : "GLOBAL NETWORK / LIVE");
            Text("map-instruction", choosing ? "Select an outlined country or a location marker, then start the simulation."
                : "Inspect any location. Influence follows the fictional network connections.");
            Text("speed-label", choosing ? "AWAITING ORIGIN" : gameManager.Clock.Speed == SimulationSpeed.Paused ? "PAUSED" : $"{(int)gameManager.Clock.Speed}x SIMULATION");
            TimeSpan time = TimeSpan.FromSeconds(gameManager.Clock.ElapsedSeconds);
            Text("elapsed-label", $"T+ {(long)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}");
            foreach (var speed in new[] { ("pause-button", 0), ("normal-button", 1), ("double-button", 2), ("fast-button", 5) })
                Element(speed.Item1).EnableInClassList("is-selected", playing && (int)gameManager.Clock.Speed == speed.Item2);

            RefreshLocation(choosing);
            if (world.HasStarted)
            {
                foreach (LocationState location in world.Locations)
                {
                    if (!location.IsInfluenced || !reportedLocations.Add(location.Data.Id)) continue;
                    feed.Insert(0, $"T+{gameManager.Clock.ElapsedSeconds:0}s  /  {location.Data.DisplayName} signal established.");
                    if (feed.Count > 3) feed.RemoveAt(feed.Count - 1);
                }
            }
            Text("network-feed", feed.Count == 0 ? "Awaiting origin selection." : string.Join("\n\n", feed));
        }

        private void RefreshLocation(bool choosing)
        {
            LocationState location = gameManager.SelectedLocation;
            Text("selection-caption", choosing ? "ORIGIN SELECTION" : location?.Data.Id == gameManager.World.OriginId ? "ORIGIN NODE" : "LOCATION INTELLIGENCE");
            Text("location-name", location?.Data.DisplayName ?? "Select a location");
            Text("location-coordinates", location == null ? "NO NODE SELECTED" : Coordinates(location.Data.Coordinates));
            Text("location-devices", location == null ? "—" : location.Data.TotalDevices.ToString("N0", CultureInfo.InvariantCulture));
            Text("location-connectivity", location == null ? "—" : Percent(location.Data.Connectivity, 0));
            Text("location-security", location == null ? "—" : Percent(location.Data.SecurityLevel, 0));
            Element("connectivity-fill").style.width = Length.Percent((float)(location?.Data.Connectivity ?? 0) * 100);
            Element("security-fill").style.width = Length.Percent((float)(location?.Data.SecurityLevel ?? 0) * 100);
            Text("location-influence", Percent(location?.InfluenceFraction ?? 0));
            Text("location-influenced-devices", $"{Math.Floor(location?.InfluencedDevices ?? 0).ToString("N0", CultureInfo.InvariantCulture)} influenced devices");
            Text("location-tradeoff", location == null ? "High connectivity improves reach. High security slows local influence."
                : location.Data.SecurityLevel >= .75 ? "Well connected, well protected. Expect slower initial growth."
                : "Lower resistance to influence. Network links determine its reach.");
            Text("selection-status", choosing ? location == null ? "Select an origin on the map." : "ORIGIN READY / all other locations begin clear."
                : gameManager.World.ActiveLocations == gameManager.World.Locations.Count ? "All locations reached. Influence continues to grow."
                : location?.Data.Id == gameManager.World.OriginId ? "Initial foothold / influence propagating." : "Live telemetry / select a linked node to inspect.");

            if (connectionsFor == gameManager.SelectedLocationId && connectionsFor != null) return;
            connectionsFor = gameManager.SelectedLocationId;
            VisualElement links = Element("connected-locations");
            links.Clear();
            if (location == null) return;
            foreach (NetworkLink link in gameManager.World.Definition.Connections)
            {
                string id = link.From == location.Data.Id ? link.To : link.To == location.Data.Id ? link.From : null;
                if (id == null) continue;
                string targetId = id;
                links.Add(new Button(() => gameManager.SelectLocation(targetId))
                { text = gameManager.World.GetLocation(targetId).Data.DisplayName });
            }
        }

        private void OpenSettings()
        {
            if (settingsOpen) return;
            settingsOpen = true;
            speedBeforeSettings = gameManager.Clock.Speed;
            gameManager.SetSimulationSpeed(SimulationSpeed.Paused);
            root.Q<Toggle>("show-routes-toggle").value = preferences.ShowConnections;
            root.Q<Toggle>("high-contrast-toggle").value = preferences.HighContrast;
            root.Q<Slider>("zoom-sensitivity-slider").value = preferences.ZoomSensitivity;
            Element("settings-overlay").style.display = DisplayStyle.Flex;
        }

        private void CloseSettings(bool apply)
        {
            if (!settingsOpen) return;
            if (apply)
            {
                preferences.ShowConnections = root.Q<Toggle>("show-routes-toggle").value;
                preferences.HighContrast = root.Q<Toggle>("high-contrast-toggle").value;
                preferences.ZoomSensitivity = root.Q<Slider>("zoom-sensitivity-slider").value;
                preferences.Save();
                ApplyPreferences();
            }
            settingsOpen = false;
            Element("settings-overlay").style.display = DisplayStyle.None;
            gameManager.SetSimulationSpeed(speedBeforeSettings);
        }

        private void ApplyPreferences()
        {
            Map.ShowConnections = preferences.ShowConnections;
            Map.HighContrast = preferences.HighContrast;
            Map.ZoomSensitivity = preferences.ZoomSensitivity;
            root.EnableInClassList("high-contrast", preferences.HighContrast);
            Map.Refresh(gameManager.SelectedLocationId);
        }

        private void HandleKey(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;
            if (settingsOpen) CloseSettings(false); else OpenSettings();
            evt.StopPropagation();
        }

        private static string Coordinates(GeoCoordinate point)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0}°{1} / {2:0}°{3}",
                Math.Abs(point.Latitude), point.Latitude >= 0 ? "N" : "S",
                Math.Abs(point.Longitude), point.Longitude >= 0 ? "E" : "W");
        }

        private static string Percent(double fraction, int decimals = 2) =>
            (fraction * 100).ToString("F" + decimals, CultureInfo.InvariantCulture) + "%";

        private void Text(string name, string value)
        {
            if (!labels.TryGetValue(name, out Label label))
            {
                label = root.Q<Label>(name) ?? throw new InvalidOperationException("Missing UI label: " + name);
                labels.Add(name, label);
            }
            label.text = value;
        }

        private VisualElement Element(string name) => root.Q(name) ?? throw new InvalidOperationException("Missing UI element: " + name);

        private void Bind(string name, Action action)
        {
            Button button = root.Q<Button>(name) ?? throw new InvalidOperationException("Missing UI button: " + name);
            button.clicked += action;
            unsubscribe.Add(() => button.clicked -= action);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
