using System.Runtime.CompilerServices;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class Movement
    {
        public int DestX { get; set; }

        public int DestY { get; set; }

        public double Distance { get; set; }

        public int XDistance { get; set; }

        public int YDistance { get; set; }

        public long Time { get; set; }

        public Flow Flow { get; set; }

        public override string ToString()
        {
            return $"Movement{{destX={DestX},DestY={DestY},XDistance={XDistance},YDistance={YDistance},time={Time}";
        }
    }
}
