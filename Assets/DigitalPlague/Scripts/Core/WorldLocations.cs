namespace DigitalPlague.Core
{
    /// <summary>All statistics and links are fictional game balance, not real country data.</summary>
    public static class WorldLocations
    {
        public static WorldDefinition Create()
        {
            return new WorldDefinition(new[]
            {
                new RegionData("USA", "United States", 92000000, .92, .77, new GeoCoordinate(38, -100)),
                new RegionData("BRA", "Brazil", 48000000, .68, .43, new GeoCoordinate(-12, -52)),
                new RegionData("GBR", "United Kingdom", 26000000, .91, .81, new GeoCoordinate(54, -3)),
                new RegionData("DEU", "Germany", 34000000, .88, .86, new GeoCoordinate(51, 10)),
                new RegionData("NGA", "Nigeria", 22000000, .59, .32, new GeoCoordinate(9, 8)),
                new RegionData("ZAF", "South Africa", 17000000, .71, .56, new GeoCoordinate(-29, 25)),
                new RegionData("IND", "India", 84000000, .83, .48, new GeoCoordinate(22, 79)),
                new RegionData("CHN", "China", 106000000, .90, .84, new GeoCoordinate(35, 104)),
                new RegionData("JPN", "Japan", 39000000, .96, .89, new GeoCoordinate(37, 138)),
                new RegionData("AUS", "Australia", 19000000, .87, .72, new GeoCoordinate(-25, 134))
            }, new[]
            {
                new NetworkLink("USA", "GBR"), new NetworkLink("USA", "BRA"),
                new NetworkLink("USA", "JPN"), new NetworkLink("GBR", "DEU"),
                new NetworkLink("GBR", "NGA"), new NetworkLink("DEU", "CHN"),
                new NetworkLink("DEU", "IND"), new NetworkLink("NGA", "ZAF"),
                new NetworkLink("NGA", "BRA"), new NetworkLink("ZAF", "IND"),
                new NetworkLink("IND", "CHN"), new NetworkLink("IND", "AUS"),
                new NetworkLink("CHN", "JPN"), new NetworkLink("JPN", "AUS"),
                new NetworkLink("BRA", "ZAF")
            });
        }
    }
}
