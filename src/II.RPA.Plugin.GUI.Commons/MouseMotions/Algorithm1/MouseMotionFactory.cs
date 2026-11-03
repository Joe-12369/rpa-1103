using II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class MouseMotionFactory
    {
        private static readonly MouseMotionFactory _defaultFactory = new MouseMotionFactory();

        public Random Random { get; set; }

        public MouseMotionNature Nature { get; set; }

        public MouseMotionFactory()
          : this(MouseMotionNature.DefaultMouseMotionNature())
        {
        }

        public MouseMotionFactory(MouseMotionNature nature)
        {
            Nature = nature;
            Random = new Random();
        }

        public MouseMotion Create(
          int xDest,
          int yDest,
          int maxX,
          int maxY)
        {
            return new MouseMotion(Nature, Random, xDest, yDest, maxX, maxY);
        }

        public IEnumerable<MouseRoute> BuildRoutes(
            int xStart,
            int yStart,
            int xDest,
            int yDest,
            int maxX,
            int maxY)
            => Create(xDest, yDest, maxX, maxY).BuildRoutes(xStart, yStart);

        public static MouseMotionFactory GetDefault() => _defaultFactory;

        public IDeviationProvider DeviationProvider
        {
            get => Nature.DeviationProvider;
            set => Nature.DeviationProvider = value;
        }

        public INoiseProvider NoiseProvider
        {
            get => Nature.NoiseProvider;
            set => Nature.NoiseProvider = value;
        }

        public ISpeedManager SpeedManager
        {
            get => Nature.SpeedManager;
            set => Nature.SpeedManager = value;
        }

        public OvershootManager OvershootManager
        {
            get => Nature.OvershootManager;
            set => Nature.OvershootManager = value;
        }
    }
}
