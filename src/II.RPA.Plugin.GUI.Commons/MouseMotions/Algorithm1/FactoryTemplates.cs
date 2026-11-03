

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class FactoryTemplates
    {
        public static MouseMotionFactory CreateGrannyMotionFactory() => CreateGrannyMotionFactory(MouseMotionNature.DefaultMouseMotionNature());

        public static MouseMotionFactory CreateGrannyMotionFactory(
          MouseMotionNature nature)
        {
            MouseMotionFactory grannyMotionFactory = new MouseMotionFactory(nature);
            DefaultSpeedManager defaultSpeedManager = new DefaultSpeedManager(new[]
            {
                new Flow(FlowTemplates.JaggedFlow()),
                new Flow(FlowTemplates.Random()),
                new Flow(FlowTemplates.InterruptedFlow()),
                new Flow(FlowTemplates.InterruptedFlow2()),
                new Flow(FlowTemplates.AdjustingFlow()),
                new Flow(FlowTemplates.StoppingFlow())
            });
            grannyMotionFactory.DeviationProvider = new SinusoidalDeviationProvider(9.0);
            grannyMotionFactory.NoiseProvider = new DefaultNoiseProvider(1.6);
            grannyMotionFactory.Nature.ReactionTimeBaseMs = 100;
            OvershootManager overshootManager = grannyMotionFactory.OvershootManager;
            overshootManager.Overshoots = 3;
            overshootManager.MinDistanceForOvershoots = 3L;
            overshootManager.MinOvershootMovementMs = 400L;
            overshootManager.OvershootRandomModifierDivider = OvershootManager.OVERSHOOT_RANDOM_MODIFIER_DIVIDER / 2;
            overshootManager.OvershootSpeedupDivider = OvershootManager.OVERSHOOT_SPEEDUP_DIVIDER * 2.0;
            grannyMotionFactory.Nature.TimeToStepsDivider = MouseMotionNature.TIME_TO_STEPS_DIVIDER - 2;
            defaultSpeedManager.MouseMovementTimeMs = 1000L;
            grannyMotionFactory.SpeedManager = defaultSpeedManager;
            return grannyMotionFactory;
        }

        public static MouseMotionFactory CreateDemoRobotMotionFactory(
          long motionTimeMsPer100Pixels)
        {
            return CreateDemoRobotMotionFactory(MouseMotionNature.DefaultMouseMotionNature(), motionTimeMsPer100Pixels);
        }

        public static MouseMotionFactory CreateDemoRobotMotionFactory(
          MouseMotionNature nature,
          long motionTimeMsPer100Pixels)
        {
            MouseMotionFactory robotMotionFactory = new MouseMotionFactory(nature);
            ISpeedManager speedManager = new ConstSpeedManager(motionTimeMsPer100Pixels);
            robotMotionFactory.DeviationProvider = new ZeroDeviationProvider();
            robotMotionFactory.NoiseProvider = new ZeroNoiseProvider();
            robotMotionFactory.OvershootManager.Overshoots = 0;
            robotMotionFactory.SpeedManager = speedManager;
            return robotMotionFactory;
        }

        public static MouseMotionFactory CreateFastGamerMotionFactory() => CreateFastGamerMotionFactory(MouseMotionNature.DefaultMouseMotionNature());

        public static MouseMotionFactory CreateFastGamerMotionFactory(
          MouseMotionNature nature)
        {
            MouseMotionFactory gamerMotionFactory = new MouseMotionFactory(nature);
            DefaultSpeedManager defaultSpeedManager = new DefaultSpeedManager(new[]
            {
                new Flow(FlowTemplates.VariatingFlow()),
                new Flow(FlowTemplates.SlowStartupFlow()),
                new Flow(FlowTemplates.SlowStartup2Flow()),
                new Flow(FlowTemplates.AdjustingFlow()),
                new Flow(FlowTemplates.JaggedFlow())
            });
            gamerMotionFactory.DeviationProvider = new SinusoidalDeviationProvider(SinusoidalDeviationProvider.DEFAULT_SLOPE_DIVIDER);
            gamerMotionFactory.NoiseProvider = new DefaultNoiseProvider(DefaultNoiseProvider.DEFAULT_NOISINESS_DIVIDER);
            gamerMotionFactory.Nature.ReactionTimeVariationMs = 100;
            defaultSpeedManager.MouseMovementTimeMs = 250L;
            gamerMotionFactory.OvershootManager.Overshoots = 4;
            gamerMotionFactory.SpeedManager = defaultSpeedManager;
            return gamerMotionFactory;
        }

        public static MouseMotionFactory CreateAverageComputerUserMotionFactory() => CreateAverageComputerUserMotionFactory(MouseMotionNature.DefaultMouseMotionNature());

        public static MouseMotionFactory CreateAverageComputerUserMotionFactory(
          MouseMotionNature nature)
        {
            MouseMotionFactory userMotionFactory = new MouseMotionFactory(nature);
            DefaultSpeedManager defaultSpeedManager = new DefaultSpeedManager(new[]
            {
                new Flow(FlowTemplates.VariatingFlow()),
                new Flow(FlowTemplates.InterruptedFlow()),
                new Flow(FlowTemplates.InterruptedFlow2()),
                new Flow(FlowTemplates.SlowStartupFlow()),
                new Flow(FlowTemplates.SlowStartup2Flow()),
                new Flow(FlowTemplates.AdjustingFlow()),
                new Flow(FlowTemplates.JaggedFlow()),
                new Flow(FlowTemplates.StoppingFlow())
            });
            userMotionFactory.DeviationProvider = new SinusoidalDeviationProvider(
                SinusoidalDeviationProvider.DEFAULT_SLOPE_DIVIDER);
            userMotionFactory.NoiseProvider = new DefaultNoiseProvider(
                DefaultNoiseProvider.DEFAULT_NOISINESS_DIVIDER);
            userMotionFactory.Nature.ReactionTimeVariationMs = 110;
            defaultSpeedManager.MouseMovementTimeMs = 400L;
            userMotionFactory.OvershootManager.Overshoots = 4;
            userMotionFactory.SpeedManager = defaultSpeedManager;
            return userMotionFactory;
        }
    }
}