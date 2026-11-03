using System;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class OvershootManager
    {
        public static readonly double OVERSHOOT_SPEEDUP_DIVIDER = 1.8;
        public static readonly int MIN_OVERSHOOT_MOVEMENT_MS = 40;
        public static readonly int OVERSHOOT_RANDOM_MODIFIER_DIVIDER = 20;
        public static readonly int MIN_DISTANCE_FOR_OVERSHOOTS = 10;
        public static readonly int DEFAULT_OVERSHOOT_AMOUNT = 3;
        private readonly Random _random;

        public long MinOvershootMovementMs { get; set; }

        public long MinDistanceForOvershoots { get; set; }

        public double OvershootRandomModifierDivider { get; set; }

        public double OvershootSpeedupDivider { get; set; }

        public int Overshoots { get; set; }

        public OvershootManager(Random random)
        {
            _random = random;

            MinOvershootMovementMs = MIN_OVERSHOOT_MOVEMENT_MS;
            MinDistanceForOvershoots = MIN_DISTANCE_FOR_OVERSHOOTS;
            OvershootRandomModifierDivider = OVERSHOOT_RANDOM_MODIFIER_DIVIDER;
            OvershootSpeedupDivider = OVERSHOOT_SPEEDUP_DIVIDER;
            Overshoots = DEFAULT_OVERSHOOT_AMOUNT;
        }

        public int GetOvershoots(Flow flow, long mouseMovementMs, double distance)
            => distance < MinDistanceForOvershoots ? 0 : Overshoots;

        public IntPoint GetOvershootAmount(
          double distanceToRealTargetX,
          double distanceToRealTargetY,
          long mouseMovementMs,
          int overshootsRemaining)
        {
            double num = Utils.Hypot(
                distanceToRealTargetX,
                distanceToRealTargetY)
                / OvershootRandomModifierDivider;

            return new IntPoint(
                (int)(_random.NextDouble() * num - num / 2.0) * overshootsRemaining,
                (int)(_random.NextDouble() * num - num / 2.0) * overshootsRemaining);
        }

        public long DeriveNextMouseMovementTimeMs(long mouseMovementMs, int overshootsRemaining)
            => Math.Max((long)(mouseMovementMs / OvershootSpeedupDivider), MinOvershootMovementMs);
    }
}
