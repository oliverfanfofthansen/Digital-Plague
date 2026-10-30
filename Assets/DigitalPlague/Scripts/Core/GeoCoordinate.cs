using System;

namespace DigitalPlague.Core
{
    /// <summary>Shared geographic identity for a flat map and a future globe.</summary>
    public readonly struct GeoCoordinate
    {
        public double Latitude { get; }
        public double Longitude { get; }

        public GeoCoordinate(double latitude, double longitude)
        {
            if (double.IsNaN(latitude) || latitude < -90 || latitude > 90)
                throw new ArgumentOutOfRangeException(nameof(latitude));
            if (double.IsNaN(longitude) || longitude < -180 || longitude > 180)
                throw new ArgumentOutOfRangeException(nameof(longitude));
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
