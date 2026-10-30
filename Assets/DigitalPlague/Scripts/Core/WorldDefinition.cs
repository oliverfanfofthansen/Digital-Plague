using System;
using System.Collections.Generic;

namespace DigitalPlague.Core
{
    public sealed class WorldDefinition
    {
        public IReadOnlyList<RegionData> Locations { get; }
        public IReadOnlyList<NetworkLink> Connections { get; }

        public WorldDefinition(IEnumerable<RegionData> locations, IEnumerable<NetworkLink> connections)
        {
            var regionList = new List<RegionData>(locations);
            var linkList = new List<NetworkLink>(connections);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RegionData region in regionList)
            {
                if (region == null || region.TotalDevices <= 0 || !ids.Add(region.Id))
                    throw new ArgumentException("Locations need unique IDs and positive device populations.");
            }

            if (regionList.Count == 0) throw new ArgumentException("The world needs locations.");
            var links = new HashSet<string>(StringComparer.Ordinal);
            foreach (NetworkLink link in linkList)
            {
                string key = string.CompareOrdinal(link.From, link.To) < 0
                    ? link.From + "/" + link.To : link.To + "/" + link.From;
                if (!ids.Contains(link.From) || !ids.Contains(link.To) || !links.Add(key))
                    throw new ArgumentException("Links need existing endpoints and must be unique.");
            }

            Locations = regionList.AsReadOnly();
            Connections = linkList.AsReadOnly();
        }
    }
}
