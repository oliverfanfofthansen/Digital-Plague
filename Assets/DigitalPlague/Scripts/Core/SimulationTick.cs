namespace DigitalPlague.Core
{
    /// <summary>A completed step of simulation time, independent of rendering.</summary>
    public readonly struct SimulationTick
    {
        public long Number { get; }
        public double DeltaSeconds { get; }
        public double ElapsedSeconds { get; }

        public SimulationTick(long number, double deltaSeconds)
        {
            Number = number;
            DeltaSeconds = deltaSeconds;
            ElapsedSeconds = number * deltaSeconds;
        }
    }
}
