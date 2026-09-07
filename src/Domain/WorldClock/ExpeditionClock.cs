using System;

namespace LethalDungeon.Domain
{
    public interface IMonotonicTimeSource
    {
        TimeSpan Now { get; }
    }

    // Data-only contract. No time calculation or state transition is implemented here.
    public sealed class ClockSnapshot
    {
        public bool HasStarted { get; }
        public TimeSpan Elapsed { get; }
        public int GameMinute { get; }
        public bool HasExpired { get; }
        public bool DeadlineReachedNow { get; }

        public ClockSnapshot(bool hasStarted, TimeSpan elapsed, int gameMinute,
            bool hasExpired, bool deadlineReachedNow)
        {
            HasStarted = hasStarted;
            Elapsed = elapsed;
            GameMinute = gameMinute;
            HasExpired = hasExpired;
            DeadlineReachedNow = deadlineReachedNow;
        }
    }

    public sealed class ExpeditionClock
    {
        // Intentionally no validation/state yet; null-source test must also be Red.
        public ExpeditionClock(IMonotonicTimeSource source) { }

        public void Start()
            => throw new NotImplementedException("CLOCK start rules await Green implementation.");

        public ClockSnapshot Poll()
            => throw new NotImplementedException("CLOCK polling rules await Green implementation.");
    }
}
