using System;
using System.Collections.Generic;
using DigitalPlague.Core;
using NUnit.Framework;

namespace DigitalPlague.Tests
{
    public sealed class RegionDataTests
    {
        [Test]
        public void InitialRegionsHaveUniqueStableIdsAndAReadOnlyCollection()
        {
            IReadOnlyList<RegionData> regions = FictionalRegions.Create();
            var ids = new HashSet<string>();
            Assert.That(regions.Count, Is.EqualTo(4));
            foreach (RegionData region in regions)
            {
                Assert.That(ids.Add(region.Id), Is.True, "Region IDs must be unique.");
                Assert.That(region.TotalDevices, Is.GreaterThan(0));
            }

            Assert.Throws<NotSupportedException>(() =>
                ((IList<RegionData>)regions).RemoveAt(0));
        }

        [TestCase(-0.1, 0.5)]
        [TestCase(1.1, 0.5)]
        [TestCase(double.NaN, 0.5)]
        [TestCase(0.5, -0.1)]
        [TestCase(0.5, double.PositiveInfinity)]
        public void InvalidNormalizedPropertiesAreRejected(double connectivity, double security)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RegionData("test", "Test", 100, connectivity, security));
        }

        [Test]
        public void InvalidIdentityAndDeviceCountsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new RegionData("", "Test", 100, 0, 0));
            Assert.Throws<ArgumentException>(() => new RegionData("test", " ", 100, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegionData("test", "Test", -1, 0, 0));
        }
    }
}
