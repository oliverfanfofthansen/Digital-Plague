using System;
using DigitalPlague.Core;
using NUnit.Framework;

namespace DigitalPlague.Tests
{
    public sealed class WorldSimulationTests
    {
        private static WorldDefinition Chain(double security = .5, double connectivity = .8)
        {
            return new WorldDefinition(new[]
            {
                new RegionData("A", "Origin", 1000000, connectivity, security),
                new RegionData("B", "Neighbor", 2000000, .8, .5),
                new RegionData("C", "Distant", 1000000, .8, .5),
                new RegionData("D", "Isolated", 1000000, .8, .5)
            }, new[] { new NetworkLink("A", "B"), new NetworkLink("B", "C") });
        }

        [Test]
        public void OnlyChosenOriginIsSeededAndUnstartedWorldDoesNotAdvance()
        {
            var world = new WorldSimulation(WorldLocations.Create());
            world.Advance(10);
            Assert.That(world.TotalInfluencedDevices, Is.Zero);
            world.Seed("BRA");
            Assert.That(world.ActiveLocations, Is.EqualTo(1));
            foreach (LocationState location in world.Locations)
                Assert.That(location.InfluencedDevices, location.Data.Id == "BRA" ? Is.GreaterThan(0) : Is.Zero);
        }

        [Test]
        public void SpreadOnlyCrossesOneConnectionPerTickAndNeverReachesIsolatedNodes()
        {
            var world = new WorldSimulation(Chain());
            world.Seed("A");
            world.Advance(1);
            Assert.That(world.GetLocation("B").InfluencedDevices, Is.GreaterThan(0));
            Assert.That(world.GetLocation("C").InfluencedDevices, Is.Zero);
            world.Advance(1);
            Assert.That(world.GetLocation("C").InfluencedDevices, Is.GreaterThan(0));
            for (int tick = 0; tick < 1000; tick++) world.Advance(1);
            Assert.That(world.GetLocation("D").InfluencedDevices, Is.Zero);
            foreach (LocationState location in world.Locations)
                Assert.That(location.InfluenceFraction, Is.InRange(0, 1));
        }

        [Test]
        public void SecurityAndConnectivityChangeLocalGrowth()
        {
            var lowSecurity = new WorldSimulation(Chain(.2));
            var highSecurity = new WorldSimulation(Chain(.9));
            var lowConnectivity = new WorldSimulation(Chain(.2, .2));
            foreach (WorldSimulation world in new[] { lowSecurity, highSecurity, lowConnectivity })
            {
                world.Seed("A");
                for (int tick = 0; tick < 20; tick++) world.Advance(1);
            }
            Assert.That(lowSecurity.GetLocation("A").InfluenceFraction,
                Is.GreaterThan(highSecurity.GetLocation("A").InfluenceFraction));
            Assert.That(lowSecurity.GetLocation("A").InfluenceFraction,
                Is.GreaterThan(lowConnectivity.GetLocation("A").InfluenceFraction));
        }

        [Test]
        public void FramePartitionAndSpeedProduceIdenticalSimulationForEqualTickCounts()
        {
            var regular = new WorldSimulation(WorldLocations.Create());
            var accelerated = new WorldSimulation(WorldLocations.Create());
            regular.Seed("IND");
            accelerated.Seed("IND");
            var clockA = new SimulationClock();
            var clockB = new SimulationClock(initialSpeed: SimulationSpeed.Fast);
            clockA.Tick += tick => regular.Advance(tick.DeltaSeconds);
            clockB.Tick += tick => accelerated.Advance(tick.DeltaSeconds);
            for (int frame = 0; frame < 3600; frame++) clockA.Advance(1.0 / 60);
            foreach (double delta in new[] { 1.0, .5, 5.5, 3.0, 2.0 }) clockB.Advance(delta);
            Assert.That(clockA.TickCount, Is.EqualTo(clockB.TickCount));
            foreach (LocationState location in regular.Locations)
                Assert.That(accelerated.GetLocation(location.Data.Id).InfluencedDevices,
                    Is.EqualTo(location.InfluencedDevices).Within(1e-8));
        }

        [Test]
        public void RestartClearsEveryPreviousLocationAndClockFraction()
        {
            var world = new WorldSimulation(WorldLocations.Create());
            var clock = new SimulationClock();
            int callbacks = 0;
            clock.Tick += tick => callbacks++;
            world.Seed("USA");
            world.Advance(200);
            clock.Advance(2.75);
            clock.Reset();
            world.Seed("JPN");
            Assert.That(clock.TickCount, Is.Zero);
            Assert.That(clock.Speed, Is.EqualTo(SimulationSpeed.Paused));
            Assert.That(world.ActiveLocations, Is.EqualTo(1));
            Assert.That(world.GetLocation("USA").InfluencedDevices, Is.Zero);
            clock.SetSpeed(SimulationSpeed.Normal);
            clock.Advance(.25);
            Assert.That(clock.TickCount, Is.Zero);
            clock.Advance(.75);
            Assert.That(callbacks, Is.EqualTo(3), "Reset must preserve tick subscribers.");
        }

        [Test]
        public void BrokenWorldConnectionsAndUnknownSeedsAreRejected()
        {
            var definition = Chain();
            Assert.Throws<ArgumentException>(() => new WorldDefinition(definition.Locations,
                new[] { new NetworkLink("A", "missing") }));
            Assert.Throws<ArgumentException>(() => new WorldDefinition(definition.Locations,
                new[] { new NetworkLink("A", "B"), new NetworkLink("B", "A") }));
            var world = new WorldSimulation(definition);
            world.Seed("A");
            Assert.Throws<ArgumentException>(() => world.Seed("missing"));
            Assert.That(world.OriginId, Is.EqualTo("A"));
        }

        [TestCase(91, 0)]
        [TestCase(0, -181)]
        [TestCase(double.NaN, 0)]
        public void InvalidGeographyIsRejected(double latitude, double longitude)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(latitude, longitude));
        }
    }
}
