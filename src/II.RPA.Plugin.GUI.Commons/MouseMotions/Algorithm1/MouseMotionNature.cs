using System;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class MouseMotionNature
    {
        public static readonly int TIME_TO_STEPS_DIVIDER = 8;
        public static readonly int MIN_STEPS = 10;
        public static readonly int EFFECT_FADE_STEPS = 15;
        public static readonly int REACTION_TIME_BASE_MS = 20;
        public static readonly int REACTION_TIME_VARIATION_MS = 120;

        public double TimeToStepsDivider { get; set; }

        public int MinSteps { get; set; }

        public int EffectFadeSteps { get; set; }

        public int ReactionTimeBaseMs { get; set; }

        public int ReactionTimeVariationMs { get; set; }

        public IDeviationProvider DeviationProvider { get; set; }

        public INoiseProvider NoiseProvider { get; set; }

        public ISpeedManager SpeedManager { get; set; }

        public OvershootManager OvershootManager { get; set; }

        public static MouseMotionNature DefaultMouseMotionNature() => new MouseMotionNature()
        {
            DeviationProvider = new SinusoidalDeviationProvider(SinusoidalDeviationProvider.DEFAULT_SLOPE_DIVIDER),
            NoiseProvider = new DefaultNoiseProvider(DefaultNoiseProvider.DEFAULT_NOISINESS_DIVIDER),
            SpeedManager = new DefaultSpeedManager(),
            OvershootManager = new OvershootManager(new Random()),
            EffectFadeSteps = EFFECT_FADE_STEPS,
            MinSteps = MIN_STEPS,
            ReactionTimeBaseMs = REACTION_TIME_BASE_MS,
            ReactionTimeVariationMs = REACTION_TIME_VARIATION_MS,
            TimeToStepsDivider = TIME_TO_STEPS_DIVIDER
        };
    }
}
