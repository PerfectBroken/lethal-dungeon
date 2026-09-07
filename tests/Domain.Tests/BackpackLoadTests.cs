using System;
using LethalDungeon.Domain;
using NUnit.Framework;

namespace LethalDungeon.Tests
{
    public class BackpackLoadTests
    {
        // LOAD-001: Explicit expected outputs, independent of the production formula.
        [TestCase(0, "1.20")]
        [TestCase(1, "1.17")]
        [TestCase(2, "1.14")]
        [TestCase(3, "1.11")]
        [TestCase(4, "1.08")]
        [TestCase(5, "1.05")]
        [TestCase(6, "1.02")]
        [TestCase(7, "0.99")]
        [TestCase(8, "0.96")]
        [TestCase(9, "0.93")]
        [TestCase(10, "0.90")]
        public void EverySlotHasSpecifiedMultiplier(int slots, string expected)
        {
            Assert.That(BackpackLoad.SpeedMultiplier(slots), Is.EqualTo(
                decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)));
        }

        // LOAD-002
        [Test]
        public void EachAdditionalSlotReducesSpeed()
        {
            for (int slots = 0; slots < 10; slots++)
            {
                decimal current = BackpackLoad.SpeedMultiplier(slots);
                decimal next = BackpackLoad.SpeedMultiplier(slots + 1);
                Assert.That(current - next, Is.EqualTo(0.03m));
                Assert.That(next, Is.InRange(0.9m, 1.2m));
            }
        }

        [Test]
        public void RepeatedAndInterleavedCallsAreStable()
        {
            foreach (int slots in new[] { 5, 0, 10, 5, 10, 0, 5 })
            {
                decimal expected = slots == 5 ? 1.05m : slots == 0 ? 1.20m : 0.90m;
                Assert.That(BackpackLoad.SpeedMultiplier(slots), Is.EqualTo(expected));
            }
        }

        // LOAD-003: Reject caller errors, do not silently clamp them.
        [TestCase(-1)]
        [TestCase(11)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void InvalidSlotsAreRejected(int slots)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(new Action(() => BackpackLoad.SpeedMultiplier(slots)));
            Assert.That(error!.ParamName, Is.EqualTo("occupiedSlots"));
        }

        [Test]
        public void InvalidCallDoesNotAffectLaterValidCall()
        {
            Assert.Throws<ArgumentOutOfRangeException>(new Action(() => BackpackLoad.SpeedMultiplier(11)));
            Assert.That(BackpackLoad.SpeedMultiplier(5), Is.EqualTo(1.05m));
        }
    }
}
