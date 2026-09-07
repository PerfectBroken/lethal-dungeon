using System;

namespace LethalDungeon.Domain
{
    public interface IMonotonicTimeSource
    {
        TimeSpan Now { get; }
    }

    // Immutable observations remain valid after subsequent polls.
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
        private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(1080);
        private readonly IMonotonicTimeSource source;
        private TimeSpan origin;
        private TimeSpan lastObserved;
        private bool hasStarted;
        private bool hasExpired;

        public ExpeditionClock(IMonotonicTimeSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public void Start()
        {
            if (hasStarted) return;

            TimeSpan now = source.Now;
            if (now < TimeSpan.Zero)
                throw new InvalidOperationException("Monotonic time cannot be negative.");

            origin = now;
            lastObserved = now;
            hasStarted = true;
        }

        public ClockSnapshot Poll()
        {
            if (!hasStarted)
                return new ClockSnapshot(false, TimeSpan.Zero, 360, false, false);

            if (hasExpired)
                return new ClockSnapshot(true, Deadline, 1440, true, false);

            TimeSpan now = source.Now;
            if (now < TimeSpan.Zero || now < lastObserved)
                throw new InvalidOperationException("Monotonic time cannot move backward.");

            // Validate before accepting the observation so a failed poll changes no state.
            lastObserved = now;
            TimeSpan elapsed = now - origin;
            if (elapsed >= Deadline)
            {
                hasExpired = true;
                return new ClockSnapshot(true, Deadline, 1440, true, true);
            }

            int gameMinute = 360 + (int)(elapsed.Ticks / TimeSpan.TicksPerSecond);
            return new ClockSnapshot(true, elapsed, gameMinute, false, false);
        }
    }
}
