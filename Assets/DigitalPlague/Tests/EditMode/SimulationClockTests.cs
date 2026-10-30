using System;
using System.Collections.Generic;
using DigitalPlague.Core;
using NUnit.Framework;

namespace DigitalPlague.Tests
{
    public sealed class SimulationClockTests
    {
        [TestCase(30, SimulationSpeed.Normal, 10)]
        [TestCase(60, SimulationSpeed.Normal, 10)]
        [TestCase(144, SimulationSpeed.Normal, 10)]
        [TestCase(30, SimulationSpeed.Double, 20)]
        [TestCase(144, SimulationSpeed.Double, 20)]
        [TestCase(30, SimulationSpeed.Fast, 50)]
        [TestCase(144, SimulationSpeed.Fast, 50)]
        [TestCase(60, SimulationSpeed.Paused, 0)]
        public void TenRealSecondsProduceSameTicksAtDifferentFrameRates(
            int framesPerSecond, SimulationSpeed speed, long expectedTicks)
        {
            var clock = new SimulationClock(initialSpeed: speed);
            for (int frame = 0; frame < framesPerSecond * 10; frame++)
            {
                clock.Advance(1.0 / framesPerSecond);
            }

            Assert.That(clock.TickCount, Is.EqualTo(expectedTicks));
            Assert.That(clock.ElapsedSeconds, Is.EqualTo((double)expectedTicks));
        }

        [Test]
        public void IrregularFramesAndASlowFrameDoNotDropTicks()
        {
            var clock = new SimulationClock(0.25, SimulationSpeed.Fast);
            var regularClock = new SimulationClock(0.25, SimulationSpeed.Fast);
            foreach (double delta in new[] { 0.01, 0.34, 0.15, 3.5, 0.0, 2.0 })
            {
                clock.Advance(delta);
            }

            for (int frame = 0; frame < 360; frame++)
            {
                regularClock.Advance(1.0 / 60);
            }

            Assert.That(clock.TickCount, Is.EqualTo(120));
            Assert.That(clock.TickCount, Is.EqualTo(regularClock.TickCount));
        }

        [Test]
        public void PauseDiscardsPausedTimeAndPreservesPartialTick()
        {
            var clock = new SimulationClock();
            clock.Advance(0.75);
            clock.SetSpeed(SimulationSpeed.Paused);
            clock.Advance(300);
            Assert.That(clock.TickCount, Is.Zero);

            clock.SetSpeed(SimulationSpeed.Normal);
            clock.Advance(0.25);
            Assert.That(clock.TickCount, Is.EqualTo(1));
        }

        [Test]
        public void ChangingSpeedDoesNotRescaleAlreadyAccumulatedTime()
        {
            var clock = new SimulationClock();
            clock.Advance(0.5);
            clock.SetSpeed(SimulationSpeed.Double);
            clock.Advance(0.25);
            Assert.That(clock.TickCount, Is.EqualTo(1));

            clock.SetSpeed(SimulationSpeed.Fast);
            clock.Advance(0.2);
            Assert.That(clock.TickCount, Is.EqualTo(2));
        }

        [Test]
        public void EveryTickHasTheSameDurationAndSequentialNumber()
        {
            var clock = new SimulationClock(0.5);
            var ticks = new List<SimulationTick>();
            clock.Tick += ticks.Add;
            clock.Advance(2.25);

            Assert.That(ticks.Count, Is.EqualTo(4));
            for (int index = 0; index < ticks.Count; index++)
            {
                Assert.That(ticks[index].Number, Is.EqualTo(index + 1));
                Assert.That(ticks[index].DeltaSeconds, Is.EqualTo(0.5));
                Assert.That(ticks[index].ElapsedSeconds, Is.EqualTo((index + 1) * 0.5));
            }
        }

        [Test]
        public void TickListenerCanPauseCatchUpWithoutLosingPendingTime()
        {
            var clock = new SimulationClock();
            Action<SimulationTick> pause = tick => clock.SetSpeed(SimulationSpeed.Paused);
            clock.Tick += pause;
            clock.Advance(3);
            Assert.That(clock.TickCount, Is.EqualTo(1));

            clock.Tick -= pause;
            clock.SetSpeed(SimulationSpeed.Normal);
            clock.Advance(0);
            Assert.That(clock.TickCount, Is.EqualTo(3));
        }

        [Test]
        public void SpeedChangedOnlyFiresForActualChanges()
        {
            var clock = new SimulationClock();
            var changes = new List<SimulationSpeed>();
            clock.SpeedChanged += changes.Add;
            clock.SetSpeed(SimulationSpeed.Normal);
            clock.SetSpeed(SimulationSpeed.Paused);
            clock.SetSpeed(SimulationSpeed.Paused);
            clock.SetSpeed(SimulationSpeed.Fast);
            Assert.That(changes, Is.EqualTo(new[] { SimulationSpeed.Paused, SimulationSpeed.Fast }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidTickDurationsAreRejected(double duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationClock(duration));
        }

        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void InvalidRealDeltasAreRejected(double delta)
        {
            var clock = new SimulationClock();
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(delta));
        }

        [Test]
        public void UnsupportedSpeedsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SimulationClock(initialSpeed: (SimulationSpeed)3));
            var clock = new SimulationClock();
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSpeed((SimulationSpeed)3));
        }
    }
}
