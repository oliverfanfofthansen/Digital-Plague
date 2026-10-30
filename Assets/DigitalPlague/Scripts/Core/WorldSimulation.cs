using System;
using System.Collections.Generic;

namespace DigitalPlague.Core
{
    /// <summary>
    /// A deterministic toy model of fictional influence, with no network access.
    /// Reads the previous state and applies all changes together each fixed tick.
    /// </summary>
    public sealed class WorldSimulation
    {
        private readonly Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly LocationState[] locations;
        private readonly double[] incomingPressure;
        private readonly double[] nextInfluence;
        private readonly double[] exposure;
        private readonly SpreadSettings settings;

        public WorldDefinition Definition { get; }
        public IReadOnlyList<LocationState> Locations { get; }
        public bool HasStarted { get; private set; }
        public string OriginId { get; private set; }
        public long TotalDevices { get; }
        public double TotalInfluencedDevices { get; private set; }
        public double InfluenceFraction => TotalInfluencedDevices / TotalDevices;
        public int ActiveLocations { get; private set; }

        public WorldSimulation(WorldDefinition definition, SpreadSettings settings = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.settings = (settings ?? new SpreadSettings()).Snapshot();
            locations = new LocationState[definition.Locations.Count];
            incomingPressure = new double[locations.Length];
            nextInfluence = new double[locations.Length];
            exposure = new double[locations.Length];
            long totalDevices = 0;
            for (int index = 0; index < locations.Length; index++)
            {
                RegionData data = definition.Locations[index];
                locations[index] = new LocationState(data);
                indices.Add(data.Id, index);
                totalDevices += data.TotalDevices;
            }
            TotalDevices = totalDevices;
            Locations = Array.AsReadOnly(locations);
        }

        public LocationState GetLocation(string id)
        {
            if (id == null || !indices.TryGetValue(id, out int index))
                throw new ArgumentException("Unknown location ID.", nameof(id));
            return locations[index];
        }

        public void Reset()
        {
            HasStarted = false;
            OriginId = null;
            TotalInfluencedDevices = 0;
            ActiveLocations = 0;
            Array.Clear(exposure, 0, exposure.Length);
            foreach (LocationState location in locations) location.InfluencedDevices = 0;
        }

        public void Seed(string originId)
        {
            LocationState origin = GetLocation(originId); // Validate before resetting.
            Reset();
            OriginId = originId;
            origin.InfluencedDevices = Math.Max(1, origin.Data.TotalDevices * settings.InitialInfluenceFraction);
            HasStarted = true;
            UpdateTotals();
        }

        public void Advance(double deltaSeconds)
        {
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (!HasStarted || deltaSeconds == 0) return;

            Array.Clear(incomingPressure, 0, incomingPressure.Length);
            foreach (NetworkLink link in Definition.Connections)
            {
                int from = indices[link.From];
                int to = indices[link.To];
                incomingPressure[to] += locations[from].InfluenceFraction;
                incomingPressure[from] += locations[to].InfluenceFraction;
            }

            for (int index = 0; index < locations.Length; index++)
            {
                LocationState location = locations[index];
                RegionData data = location.Data;
                double fraction = location.InfluenceFraction;
                double susceptibility = (settings.ConnectivityBaseline + data.Connectivity) *
                    (1 - settings.SecurityResistance * data.SecurityLevel);
                if (fraction == 0)
                {
                    // Exposure is not influence. An untouched country stays clear until
                    // it receives enough pressure, then starts with one small seed.
                    exposure[index] += incomingPressure[index] * settings.TransmissionRatePerSecond *
                        susceptibility * deltaSeconds;
                    nextInfluence[index] = exposure[index] >= settings.ExposureToSeed
                        ? Math.Max(1, data.TotalDevices * settings.NewLocationInfluenceFraction) : 0;
                    continue;
                }

                // Exact solution of dp/dt = r*p*(1-p). Bounded and stable even for
                // large time steps; all locations still read the previous tick's state.
                double decay = Math.Exp(-settings.GrowthRatePerSecond * susceptibility * deltaSeconds);
                double nextFraction = fraction / (fraction + (1 - fraction) * decay);
                nextInfluence[index] = Math.Min(1, nextFraction) * data.TotalDevices;
            }

            for (int index = 0; index < locations.Length; index++)
                locations[index].InfluencedDevices = nextInfluence[index];
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            TotalInfluencedDevices = 0;
            ActiveLocations = 0;
            foreach (LocationState location in locations)
            {
                TotalInfluencedDevices += location.InfluencedDevices;
                if (location.IsInfluenced) ActiveLocations++;
            }
        }
    }
}
