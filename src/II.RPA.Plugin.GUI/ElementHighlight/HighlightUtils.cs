using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ElementHighlight
{
    public static class HighlightUtils
    {
        public static Rectangle Downscale(this Rectangle rectangle, double factor)
        {
            Rectangle result = new Rectangle(rectangle.Location, rectangle.Size);
            result.X = (int)((double)result.X / factor);
            result.Y = (int)((double)result.Y / factor);
            result.Height = (int)((double)result.Height / factor);
            result.Width = (int)((double)result.Width / factor);
            return result;
        }

        public static Rectangle UpScale(this Rectangle rectangle, double factor)
        {
            Rectangle result = new Rectangle(rectangle.Location, rectangle.Size);
            result.X = (int)((double)result.X * factor);
            result.Y = (int)((double)result.Y * factor);
            result.Height = (int)((double)result.Height * factor);
            result.Width = (int)((double)result.Width * factor);
            return result;
        }


        public static Rectangle ResizeRetangle(this Rectangle _Rectangle, int _Delta)
        {
            //Rectangle result = new Rectangle(_Rectangle.X-_Delta, _Rectangle.Y - _Delta, _Rectangle.Width + _Delta*2, _Rectangle.Height + _Delta * 2);
            Rectangle result = new Rectangle(_Rectangle.X +  2 * _Delta, _Rectangle.Y + 2 * _Delta, _Rectangle.Width -  2 *_Delta, _Rectangle.Height - 2 * _Delta) ;
            return result;
        }


        public struct Dpi
        {
            public double X
            {
                get; set;
            }
            public double Y
            {
                get; set;
            }
            public Dpi(double aX, double aY)
            {
                X = aX;
                Y = aY;
            }
        }
    }
}
