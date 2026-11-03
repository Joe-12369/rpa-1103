namespace II.Framework.Models
{
    public static class Extensions
    {
        public static Rect ToRect(this System.Windows.Rect wpfRect)
        {
            return new Rect 
            {
                X = (int)wpfRect.X,
                Y = (int)wpfRect.Y,
                H = (int)wpfRect.Height,
                W = (int)wpfRect.Width,
            };
        }
        public static System.Windows.Rect ToWpfRect(this Rect  rect)
        {
            return new System.Windows.Rect
            {
                X = (double)rect.X,
                Y = (double)rect.Y,
                Height = (double)rect.H,
                Width = (double)rect.W,
            };
        }
    }
}
