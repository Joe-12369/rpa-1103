using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.HighLight
{
    public class AiLoadingRect
    {
        public Rectangle AiRectangle = new Rectangle();
        public bool IsAiLoadingRunning = false;
        public bool HasSetValue = false;

        public bool DiffrentRect(Rectangle _Rectangle,int Delta = 20)
        {
            if (Math.Abs(AiRectangle.X - _Rectangle.X) > Delta
                || Math.Abs(AiRectangle.Y - _Rectangle.Y) > Delta
                || Math.Abs(AiRectangle.Width - _Rectangle.Width) > Delta
                || Math.Abs(AiRectangle.Height - _Rectangle.Height) > Delta)
            {
                return true;
            }

            return false;
        }

        public bool HasSameRect(Rectangle _Rectangle)
        {
            if (AiRectangle.X != _Rectangle.X
                || AiRectangle.Y != _Rectangle.Y
                || AiRectangle.Width != _Rectangle.Width
                || AiRectangle.Height != _Rectangle.Height)
            {
                return false;
            }

            return true;
        }

        public void Init()
        {
            this.AiRectangle = new Rectangle();
            this.IsAiLoadingRunning = false;
            this.HasSetValue = false;
        }

        public void RecordCurrentRect(Rectangle _Rectangle)
        {
            this.AiRectangle = _Rectangle;
            this.HasSetValue = true;
        }
    }
}
