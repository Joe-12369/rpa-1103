using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiTabAction : ISapControl
    {
        public void Execute(ApplicationContext context)
        {
            throw new NotImplementedException();
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = new Rectangle();
            try
            {
                GuiTab gb = context.component as GuiTab;
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
                throw new Exception("GuiTabAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiTab box = (GuiTab)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiTabAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            try
            {
                GuiTab box = (GuiTab)context.session.FindById(context.path);
                box.Text = txt;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiTabAction GetText error ", ex);
            }
        }
    }
}
