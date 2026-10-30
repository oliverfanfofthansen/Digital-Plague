using System;
using System.Collections.Generic;
using DigitalPlague.Core;
using UnityEngine;

namespace DigitalPlague
{
    /// <summary>
    /// Scene composition root: owns the clock and regions and drives simulation.
    /// No UI, global singleton, or cross-scene persistence lives here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField, Min(0.001f)]
        private double tickDurationSeconds = 1.0;

        [SerializeField]
        private SimulationSpeed startingSpeed = SimulationSpeed.Normal;

        [SerializeField] private bool startInMainMenu;

        public event Action<SimulationTick> SimulationTicked;
        public event Action<GameState> StateChanged;
        public event Action WorldChanged;

        public SimulationClock Clock { get; private set; }
        public IReadOnlyList<RegionData> Regions { get; private set; }
        public GameState State { get; private set; } = GameState.Paused;
        public WorldSimulation World { get; private set; }
        public string SelectedLocationId { get; private set; }
        public LocationState SelectedLocation => SelectedLocationId == null ? null : World.GetLocation(SelectedLocationId);

        private void Awake()
        {
            Initialize();
        }

        // Idempotent so scene consumers do not depend on Awake/OnEnable ordering.
        public void Initialize()
        {
            if (Clock != null)
            {
                return;
            }

            if (startInMainMenu)
            {
                World = new WorldSimulation(WorldLocations.Create());
                Regions = World.Definition.Locations;
            }
            else Regions = FictionalRegions.Create();

            Clock = new SimulationClock(tickDurationSeconds,
                startInMainMenu ? SimulationSpeed.Paused : startingSpeed);
            Clock.Tick += HandleTick;
            Clock.SpeedChanged += HandleSpeedChanged;
            State = startInMainMenu ? GameState.MainMenu :
                startingSpeed == SimulationSpeed.Paused ? GameState.Paused : GameState.Running;
        }

        private void Update()
        {
            Clock.Advance(Time.unscaledDeltaTime);
        }

        public void SetSimulationSpeed(SimulationSpeed speed)
        {
            Initialize();
            if (World != null && State != GameState.Running && State != GameState.Paused) return;
            Clock.SetSpeed(speed);
        }

        public void BeginNewGame()
        {
            RequireWorld();
            SetState(GameState.SelectingStart);
            Clock.Reset();
            World.Reset();
            SelectedLocationId = null;
            WorldChanged?.Invoke();
        }

        public void SelectLocation(string id)
        {
            RequireWorld();
            if (State == GameState.MainMenu) return;
            World.GetLocation(id);
            SelectedLocationId = id;
            WorldChanged?.Invoke();
        }

        public bool StartGame()
        {
            RequireWorld();
            if (State != GameState.SelectingStart || SelectedLocationId == null) return false;
            Clock.Reset();
            World.Seed(SelectedLocationId);
            SetState(GameState.Running);
            Clock.SetSpeed(SimulationSpeed.Normal);
            WorldChanged?.Invoke();
            return true;
        }

        public void ReturnToMainMenu()
        {
            RequireWorld();
            SetState(GameState.MainMenu);
            Clock.SetSpeed(SimulationSpeed.Paused);
            WorldChanged?.Invoke();
        }

        private void RequireWorld()
        {
            Initialize();
            if (World == null) throw new InvalidOperationException("Use the World Game scene for this command.");
        }

        private void SetState(GameState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(State);
        }

        private void HandleSpeedChanged(SimulationSpeed speed)
        {
            if (World != null && (State == GameState.MainMenu || State == GameState.SelectingStart)) return;
            GameState nextState = speed == SimulationSpeed.Paused ? GameState.Paused : GameState.Running;
            SetState(nextState);
            WorldChanged?.Invoke();
        }

        private void HandleTick(SimulationTick tick)
        {
            World?.Advance(tick.DeltaSeconds);
            SimulationTicked?.Invoke(tick);
            WorldChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (Clock != null)
            {
                Clock.Tick -= HandleTick;
                Clock.SpeedChanged -= HandleSpeedChanged;
            }
        }
    }
}
