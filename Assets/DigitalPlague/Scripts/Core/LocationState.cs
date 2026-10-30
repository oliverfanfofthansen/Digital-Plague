namespace DigitalPlague.Core
{
    /// <summary>Mutable simulation state alongside immutable region properties.</summary>
    public sealed class LocationState
    {
        public RegionData Data { get; }
        public double InfluencedDevices { get; internal set; }
        public double InfluenceFraction => InfluencedDevices / Data.TotalDevices;
        public bool IsInfluenced => InfluencedDevices >= 1;

        internal LocationState(RegionData data) => Data = data;
    }
}
