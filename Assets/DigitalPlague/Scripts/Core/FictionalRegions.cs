using System;
using System.Collections.Generic;

namespace DigitalPlague.Core
{
    public static class FictionalRegions
    {
        public static IReadOnlyList<RegionData> Create()
        {
            return Array.AsReadOnly(new[]
            {
                new RegionData("aurora-grid", "Aurora Grid", 1250000, 0.82, 0.65),
                new RegionData("neon-coast", "Neon Coast", 2400000, 0.95, 0.45),
                new RegionData("cinder-reach", "Cinder Reach", 780000, 0.58, 0.80),
                new RegionData("echo-isles", "Echo Isles", 460000, 0.40, 0.35)
            });
        }
    }
}
