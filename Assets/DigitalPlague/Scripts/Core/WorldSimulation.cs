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

        public WorldDefinition Definition { get; }
        public IReadOnlyList<LocationState> Locations { get; }
        public bool HasStarted { get; private set; }
        public string OriginId { get; private set; }
        public long TotalDevices { get; }
        public double TotalInfluencedDevices { get; private set; }
        public double InfluenceFraction => TotalInfluencedDevices / TotalDevices;
        public int ActiveLocations { get; private set; }

        public WorldSimulation(WorldDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            locations = new LocationState[definition.Locations.Count];
            incomingPressure = new double[locations.Length];
            nextInfluence = new double[locations.Length];
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
            foreach (LocationState location in locations) location.InfluencedDevices = 0;
        }

        public void Seed(string originId)
        {
            LocationState origin = GetLocation(originId); // Validate before resetting.
            Reset();
            OriginId = originId;
            origin.InfluencedDevices = Math.Max(1, origin.Data.TotalDevices * 0.008);
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
                double susceptibility = (0.4 + data.Connectivity) * (1 - 0.75 * data.SecurityLevel);
                double localGrowth = fraction * 0.055 * susceptibility;
                double transferredInfluence = incomingPressure[index] * 0.0008 * susceptibility;
                double change = (localGrowth + transferredInfluence) * (1 - fraction) * deltaSeconds;
                nextInfluence[index] = Math.Min(1, fraction + change) * data.TotalDevices;
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
