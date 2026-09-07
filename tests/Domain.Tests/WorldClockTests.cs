using System;
using LethalDungeon.Domain;
using NUnit.Framework;

namespace LethalDungeon.Tests
{
    public class WorldClockTests
    {
        private sealed class ManualTime : IMonotonicTimeSource
        {
            public TimeSpan Value { get; set; }
            public int Reads { get; private set; }
            public bool ForbidReads { get; set; }
            public TimeSpan Now
            {
                get
                {
                    if (ForbidReads) throw new InvalidOperationException("Unexpected time-source read.");
                    Reads++;
                    return Value;
                }
            }
        }

        // CLOCK-001: Neither construction nor polling before arrival consumes loading time.
        [Test]
        public void BeforeStartDoesNotConsumeLoadingTime()
        {
            var source = new ManualTime { Value = TimeSpan.FromHours(3), ForbidReads = true };
            var clock = new ExpeditionClock(source);
            for (int i = 0; i < 3; i++)
            {
                var value = clock.Poll();
                Assert.That(value.HasStarted, Is.False);
                Assert.That(value.Elapsed, Is.EqualTo(TimeSpan.Zero));
                Assert.That(value.GameMinute, Is.EqualTo(360));
                Assert.That(value.HasExpired, Is.False);
                Assert.That(value.DeadlineReachedNow, Is.False);
            }
            Assert.That(source.Reads, Is.Zero);
        }

        [Test]
        public void StartUsesArrivalAsOrigin()
        {
            var source = new ManualTime { Value = TimeSpan.FromHours(3) };
            var clock = new ExpeditionClock(source);
            clock.Start();
            Assert.That(clock.Poll().Elapsed, Is.EqualTo(TimeSpan.Zero));
            source.Value += TimeSpan.FromSeconds(1);
            var value = clock.Poll();
            Assert.That(value.HasStarted, Is.True);
            Assert.That(value.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(value.GameMinute, Is.EqualTo(361));
        }

        // CLOCK-002, CLOCK-006: One TimeSpan tick on each side of critical boundaries.
        [TestCase(0L, 360, false)]
        [TestCase(9999999L, 360, false)]
        [TestCase(10000000L, 361, false)]
        [TestCase(7199999999L, 1079, false)]
        [TestCase(7200000000L, 1080, false)]
        [TestCase(7200000001L, 1080, false)]
        [TestCase(10799999999L, 1439, false)]
        [TestCase(10800000000L, 1440, true)]
        [TestCase(10800000001L, 1440, true)]
        public void TimelineHasSpecifiedBoundaries(long ticks, int minute, bool expired)
        {
            var source = new ManualTime();
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.FromTicks(ticks);
            var value = clock.Poll();
            Assert.That(value.GameMinute, Is.EqualTo(minute));
            Assert.That(value.HasStarted, Is.True);
            Assert.That(value.HasExpired, Is.EqualTo(expired));
            Assert.That(value.DeadlineReachedNow, Is.EqualTo(expired));
            Assert.That(value.Elapsed.Ticks, Is.EqualTo(Math.Min(ticks, 10800000000L)));
        }

        // CLOCK-003
        [Test]
        public void DuplicateStartCannotResetTime()
        {
            var source = new ManualTime();
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.FromSeconds(100);
            source.ForbidReads = true;
            clock.Start();
            source.ForbidReads = false;
            Assert.That(clock.Poll().Elapsed, Is.EqualTo(TimeSpan.FromSeconds(100)));
        }

        [Test]
        public void StartAfterExpiryCannotRestart()
        {
            var source = new ManualTime();
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.FromSeconds(1080);
            Assert.That(clock.Poll().DeadlineReachedNow, Is.True);
            source.ForbidReads = true;
            clock.Start();
            var value = clock.Poll();
            Assert.That(value.HasExpired, Is.True);
            Assert.That(value.DeadlineReachedNow, Is.False);
            Assert.That(value.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(1080)));
        }

        // CLOCK-004: Sampling frequency is not the expedition clock.
        [Test]
        public void PollFrequencyDoesNotChangeElapsedTime()
        {
            var source = new ManualTime();
            var frequent = new ExpeditionClock(source);
            var sparse = new ExpeditionClock(source);
            frequent.Start();
            sparse.Start();
            for (int i = 0; i <= 720; i++)
            {
                source.Value = TimeSpan.FromSeconds(i);
                frequent.Poll();
            }
            var a = frequent.Poll();
            var b = sparse.Poll();
            Assert.That(a.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(720)));
            Assert.That(b.Elapsed, Is.EqualTo(a.Elapsed));
            Assert.That(b.GameMinute, Is.EqualTo(1080));
        }

        [Test]
        public void FractionalTimeIsPreserved()
        {
            var source = new ManualTime();
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.FromTicks(2500000);
            Assert.That(clock.Poll().Elapsed.Ticks, Is.EqualTo(2500000));
            source.Value = TimeSpan.FromTicks(7500000);
            Assert.That(clock.Poll().GameMinute, Is.EqualTo(360));
            source.Value = TimeSpan.FromTicks(10000000);
            Assert.That(clock.Poll().GameMinute, Is.EqualTo(361));
        }

        // CLOCK-005
        [Test]
        public void DeadlinePulseOccursOnce()
        {
            var source = new ManualTime();
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.FromSeconds(1079);
            Assert.That(clock.Poll().DeadlineReachedNow, Is.False);
            source.Value = TimeSpan.FromSeconds(1080);
            Assert.That(clock.Poll().DeadlineReachedNow, Is.True);
            for (int i = 0; i < 4; i++)
            {
                var value = clock.Poll();
                Assert.That(value.HasExpired, Is.True);
                Assert.That(value.DeadlineReachedNow, Is.False);
            }
        }

        [Test]
        public void JumpAcrossDeadlineEmitsPulse()
        {
            var source = new ManualTime();
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.FromSeconds(1000);
            Assert.That(clock.Poll().HasExpired, Is.False);
            source.Value = TimeSpan.FromSeconds(2000);
            var value = clock.Poll();
            Assert.That(value.HasExpired, Is.True);
            Assert.That(value.DeadlineReachedNow, Is.True);
            Assert.That(value.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(1080)));
        }

        [Test]
        public void ExpiredClockStopsSampling()
        {
            var source = new ManualTime();
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.FromSeconds(1080);
            clock.Poll();
            int reads = source.Reads;
            source.ForbidReads = true;
            Assert.That(clock.Poll().HasExpired, Is.True);
            Assert.That(source.Reads, Is.EqualTo(reads));
        }

        // CLOCK-006
        [Test]
        public void VeryLargeElapsedTimeExpiresSafely()
        {
            var source = new ManualTime { Value = TimeSpan.FromTicks(1) };
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.MaxValue;
            var value = clock.Poll();
            Assert.That(value.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(1080)));
            Assert.That(value.GameMinute, Is.EqualTo(1440));
            Assert.That(value.DeadlineReachedNow, Is.True);
        }

        [Test]
        public void OldSnapshotRemainsUnchanged()
        {
            var source = new ManualTime();
            var clock = new ExpeditionClock(source);
            var before = clock.Poll();
            clock.Start();
            source.Value = TimeSpan.FromSeconds(1);
            var first = clock.Poll();
            source.Value = TimeSpan.FromSeconds(1080);
            var expired = clock.Poll();
            clock.Poll();
            Assert.That(before.HasStarted, Is.False);
            Assert.That(before.GameMinute, Is.EqualTo(360));
            Assert.That(first.GameMinute, Is.EqualTo(361));
            Assert.That(first.HasExpired, Is.False);
            Assert.That(first.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(expired.DeadlineReachedNow, Is.True);
        }

        // CLOCK-007
        [Test]
        public void NullSourceIsRejected()
        {
            var error = Assert.Throws<ArgumentNullException>(new Action(() => new ExpeditionClock(null!)));
            Assert.That(error!.ParamName, Is.EqualTo("source"));
        }

        [Test]
        public void NegativeStartIsRejectedAndCanRetry()
        {
            var source = new ManualTime { Value = TimeSpan.FromTicks(-1) };
            var clock = new ExpeditionClock(source);
            Assert.Throws<InvalidOperationException>(new Action(() => clock.Start()));
            Assert.That(clock.Poll().HasStarted, Is.False);
            source.Value = TimeSpan.FromSeconds(100);
            clock.Start();
            source.Value = TimeSpan.FromSeconds(101);
            Assert.That(clock.Poll().GameMinute, Is.EqualTo(361));
        }

        [Test]
        public void BackwardTimeIsRejectedWithoutChangingState()
        {
            var source = new ManualTime { Value = TimeSpan.FromSeconds(100) };
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.FromSeconds(110);
            Assert.That(clock.Poll().Elapsed, Is.EqualTo(TimeSpan.FromSeconds(10)));
            source.Value = TimeSpan.FromSeconds(109);
            Assert.Throws<InvalidOperationException>(new Action(() => clock.Poll()));
            source.Value = TimeSpan.FromSeconds(110);
            Assert.That(clock.Poll().Elapsed, Is.EqualTo(TimeSpan.FromSeconds(10)));
            source.Value = TimeSpan.FromSeconds(1180);
            Assert.That(clock.Poll().DeadlineReachedNow, Is.True);
            Assert.That(clock.Poll().DeadlineReachedNow, Is.False);
        }

        [Test]
        public void NegativePollIsRejectedWithoutChangingState()
        {
            var source = new ManualTime();
            var clock = new ExpeditionClock(source);
            clock.Start();
            source.Value = TimeSpan.FromTicks(-1);
            Assert.Throws<InvalidOperationException>(new Action(() => clock.Poll()));
            source.Value = TimeSpan.FromSeconds(1);
            Assert.That(clock.Poll().GameMinute, Is.EqualTo(361));
        }
    }
}
