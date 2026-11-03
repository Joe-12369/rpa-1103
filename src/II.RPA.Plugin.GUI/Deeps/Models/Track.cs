namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public struct Track
    {
        public int X { get; set; }
        public int Y { get; set; }
        public static Track Default { get; } = new Track() { X = -1, Y = -1};
    }
}
