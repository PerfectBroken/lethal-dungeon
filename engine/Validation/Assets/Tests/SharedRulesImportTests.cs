using System;
using LethalDungeon.Domain;
using NUnit.Framework;

namespace LethalDungeon.EngineValidation
{
    // These tests must run in the editor; passing desktop tests is not an import result.
    public class SharedRulesImportTests
    {
        private sealed class ManualTime : IMonotonicTimeSource
        {
            public TimeSpan Now { get; set; }
        }

        [Test]
        public void ImportedBackpackRuleRetainsDecimalAccuracy()
        {
            Assert.That(BackpackLoad.SpeedMultiplier(0), Is.EqualTo(1.20m));
            Assert.That(BackpackLoad.SpeedMultiplier(5), Is.EqualTo(1.05m));
            Assert.That(BackpackLoad.SpeedMultiplier(10), Is.EqualTo(0.90m));
        }

        [Test]
        public void ImportedBackpackRuleRejectsInvalidInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BackpackLoad.SpeedMultiplier(11));
        }

        [Test]
        public void ImportedClockPreservesLoadingAndMidnightRules()
        {
            var source = new ManualTime { Now = TimeSpan.FromHours(2) };
            var clock = new ExpeditionClock(source);
            Assert.That(clock.Poll().HasStarted, Is.False);
            clock.Start();
            source.Now += TimeSpan.FromSeconds(720);
            Assert.That(clock.Poll().GameMinute, Is.EqualTo(1080));
            source.Now += TimeSpan.FromSeconds(360);
            Assert.That(clock.Poll().DeadlineReachedNow, Is.True);
            Assert.That(clock.Poll().DeadlineReachedNow, Is.False);
        }
    }
}
