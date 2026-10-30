using System;
using UnityEngine;

namespace DigitalPlague.UI
{
    [Serializable]
    public sealed class WorldMapGeometry
    {
        public CountryShape[] countries;

        public static WorldMapGeometry Parse(TextAsset asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            var geometry = JsonUtility.FromJson<WorldMapGeometry>(asset.text);
            if (geometry?.countries == null || geometry.countries.Length == 0)
                throw new ArgumentException("World map data contains no country boundaries.");
            return geometry;
        }
    }

    [Serializable]
    public sealed class CountryShape
    {
        public string code;
        public string name;
        public MapRing[] rings;
    }

    [Serializable]
    public sealed class MapRing
    {
        // x = longitude, y = latitude. Rendering/projection stays outside the core.
        public Vector2[] points;
    }
}
