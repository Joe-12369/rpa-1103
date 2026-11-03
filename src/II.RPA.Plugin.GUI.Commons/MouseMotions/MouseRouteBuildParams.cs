using FlaUI.Core.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions
{
    public class MouseRouteBuildParams
    {
        public MouseRouteBuildParams()
        {
            Algorithm = MouseRouteBuildAlgorithm.Algorizhm2;
        }

        public MouseRouteBuildParams(
           int endX,
           int endY,
           int speed,
           bool instant,
           MouseRouteBuildAlgorithm algorithm)
        {
            this.Algorithm = algorithm;
            this.Speed = speed;
            this.StartX = Mouse.Position.X;
            this.StartY = Mouse.Position.Y;
            var bounds = Screen.GetBounds(Mouse.Position);
            this.ScreenWidth = bounds.Width;
            this.ScreenHeight = bounds.Height;
            this.Speed = speed;
            this.EndX = endX;
            this.EndY = endY;
            this.Instant = instant;
        }

        public int StartX { get; set; }

        public int StartY { get; set; }

        public int EndX { get; set; }

        public int EndY { get; set; }

        public int ScreenWidth { get; set; }

        public int ScreenHeight { get; set; }

        public int Speed { get; set; }

        public bool Instant { get; set; }

        public MouseRouteBuildAlgorithm Algorithm { get; set; }
    }
}
