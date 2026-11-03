using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiButtonAction : ISapControl
    {
        public void Execute(ApplicationContext context)
        {
            try
            {
                GuiButton button = (GuiButton)context.session.FindById(context.path);
                button?.Press();
            }
            catch (Exception ex)
            {
                throw new Exception("GuiButtonAction  Execute Error", ex);
            }
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = new Rectangle();
            try
            {
                GuiButton gb = context.component as GuiButton;
                if (gb != null)
                {
                    pr.X = gb.ScreenLeft;
                    pr.Y = gb.ScreenTop;
                    pr.Width = gb.Width;
                    pr.Height = gb.Height;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GuiButtonAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiButton box = (GuiButton)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiButtonAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new NotImplementedException();
        }
    }
}
