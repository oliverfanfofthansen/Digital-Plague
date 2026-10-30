using System;

namespace DigitalPlague.Core
{
    /// <summary>
    /// Initial region properties. Connectivity and security are normalized 0..1
    /// placeholder values; they do not implement spread or defense behavior.
    /// IDs are stable so a future globe and map can refer to the same region.
    /// </summary>
    public sealed class RegionData
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int TotalDevices { get; }
        public double Connectivity { get; }
        public double SecurityLevel { get; }

        public RegionData(string id, string displayName, int totalDevices,
            double connectivity, double securityLevel)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A region needs a stable ID.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A region needs a display name.", nameof(displayName));
            }

            if (totalDevices < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalDevices));
            }

            ValidateNormalized(connectivity, nameof(connectivity));
            ValidateNormalized(securityLevel, nameof(securityLevel));

            Id = id;
            DisplayName = displayName;
            TotalDevices = totalDevices;
            Connectivity = connectivity;
            SecurityLevel = securityLevel;
        }

        private static void ValidateNormalized(double value, string parameterName)
        {
            if (double.IsNaN(value) || value < 0 || value > 1)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Expected a value from 0 to 1.");
            }
        }
    }
}
