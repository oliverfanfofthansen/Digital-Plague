using System;

namespace DigitalPlague.Core
{
    /// <summary>
    /// Converts real seconds into fixed simulation ticks. Has no Unity dependency.
    /// Pausing preserves the unfinished tick and discards real time spent paused.
    /// </summary>
    public sealed class SimulationClock
    {
        private double accumulatedSeconds;

        public event Action<SimulationTick> Tick;
        public event Action<SimulationSpeed> SpeedChanged;

        public double TickDurationSeconds { get; }
        public SimulationSpeed Speed { get; private set; }
        public long TickCount { get; private set; }
        public double ElapsedSeconds => TickCount * TickDurationSeconds;

        public SimulationClock(double tickDurationSeconds = 1.0,
            SimulationSpeed initialSpeed = SimulationSpeed.Normal)
        {
            if (!IsFinite(tickDurationSeconds) || tickDurationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickDurationSeconds));
            }

            ValidateSpeed(initialSpeed);
            TickDurationSeconds = tickDurationSeconds;
            Speed = initialSpeed;
        }

        public void SetSpeed(SimulationSpeed speed)
        {
            ValidateSpeed(speed);
            if (Speed == speed)
            {
                return;
            }

            Speed = speed;
            SpeedChanged?.Invoke(speed);
        }

        /// <summary>
        /// Call once per frame with unscaled real seconds. All due ticks are
        /// processed, including after a slow frame; no simulation time is dropped.
        /// </summary>
        public void Advance(double realDeltaSeconds)
        {
            if (!IsFinite(realDeltaSeconds) || realDeltaSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds));
            }

            if (Speed == SimulationSpeed.Paused)
            {
                return;
            }

            double nextAccumulatedSeconds = accumulatedSeconds + realDeltaSeconds * (int)Speed;
            if (!IsFinite(nextAccumulatedSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds));
            }

            accumulatedSeconds = nextAccumulatedSeconds;

            // Absorb only rounding noise at a tick boundary, e.g. 60 * (1 / 60).
            double tolerance = TickDurationSeconds * 1e-10;
            while (Speed != SimulationSpeed.Paused &&
                   accumulatedSeconds + tolerance >= TickDurationSeconds)
            {
                accumulatedSeconds = Math.Max(0, accumulatedSeconds - TickDurationSeconds);
                TickCount++;
                Tick?.Invoke(new SimulationTick(TickCount, TickDurationSeconds));
            }
        }

        private static void ValidateSpeed(SimulationSpeed speed)
        {
            if (speed != SimulationSpeed.Paused && speed != SimulationSpeed.Normal &&
                speed != SimulationSpeed.Double && speed != SimulationSpeed.Fast)
            {
                throw new ArgumentOutOfRangeException(nameof(speed));
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
