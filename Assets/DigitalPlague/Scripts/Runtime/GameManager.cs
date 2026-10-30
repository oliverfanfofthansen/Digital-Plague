using System;
using System.Collections.Generic;
using DigitalPlague.Core;
using UnityEngine;

namespace DigitalPlague
{
    /// <summary>
    /// Scene composition root: owns the clock and regions and drives simulation.
    /// No UI, global singleton, persistence, or future gameplay systems live here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField, Min(0.001f)]
        private double tickDurationSeconds = 1.0;

        [SerializeField]
        private SimulationSpeed startingSpeed = SimulationSpeed.Normal;

        public event Action<SimulationTick> SimulationTicked;
        public event Action<GameState> StateChanged;

        public SimulationClock Clock { get; private set; }
        public IReadOnlyList<RegionData> Regions { get; private set; }
        public GameState State { get; private set; } = GameState.Paused;

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

            Regions = FictionalRegions.Create();
            Clock = new SimulationClock(tickDurationSeconds, startingSpeed);
            Clock.Tick += HandleTick;
            Clock.SpeedChanged += HandleSpeedChanged;
            State = startingSpeed == SimulationSpeed.Paused ? GameState.Paused : GameState.Running;
        }

        private void Update()
        {
            Clock.Advance(Time.unscaledDeltaTime);
        }

        public void SetSimulationSpeed(SimulationSpeed speed)
        {
            Initialize();
            Clock.SetSpeed(speed);
        }

        private void HandleSpeedChanged(SimulationSpeed speed)
        {
            GameState nextState = speed == SimulationSpeed.Paused ? GameState.Paused : GameState.Running;
            if (State != nextState)
            {
                State = nextState;
                StateChanged?.Invoke(State);
            }
        }

        private void HandleTick(SimulationTick tick)
        {
            // Future systems can subscribe here and use tick.DeltaSeconds.
            SimulationTicked?.Invoke(tick);
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
