using System;

namespace DigitalPlague.Core
{
    /// <summary>Balance values in simulated seconds and normalized 0..1 fractions.</summary>
    [Serializable]
    public sealed class SpreadSettings
    {
        public double InitialInfluenceFraction = 0.0001;
        public double NewLocationInfluenceFraction = 0.0001;
        public double GrowthRatePerSecond = 0.08;
        public double ConnectivityBaseline = 0.4;
        public double SecurityResistance = 0.75;
        public double TransmissionRatePerSecond = 0.04;
        public double ExposureToSeed = 0.01;

        internal SpreadSettings Snapshot()
        {
            var copy = (SpreadSettings)MemberwiseClone();
            Check(InitialInfluenceFraction, nameof(InitialInfluenceFraction), 0, 1, true);
            Check(NewLocationInfluenceFraction, nameof(NewLocationInfluenceFraction), 0, 1, true);
            Check(GrowthRatePerSecond, nameof(GrowthRatePerSecond), 0, double.MaxValue);
            Check(ConnectivityBaseline, nameof(ConnectivityBaseline), 0, 1);
            Check(SecurityResistance, nameof(SecurityResistance), 0, 1);
            Check(TransmissionRatePerSecond, nameof(TransmissionRatePerSecond), 0, double.MaxValue);
            Check(ExposureToSeed, nameof(ExposureToSeed), 0, double.MaxValue, true);
            return copy;
        }

        private static void Check(double value, string name, double min, double max, bool exclusiveMin = false)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max ||
                (exclusiveMin && value == min))
                throw new ArgumentOutOfRangeException(name, "Invalid spread balance value.");
        }
    }
}
