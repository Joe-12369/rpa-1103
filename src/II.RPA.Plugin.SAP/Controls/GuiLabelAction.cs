using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiLabelAction : ISapControl
    {
        public void Execute(ApplicationContext context)
        {
            try
            {
                GuiLabel box = (GuiLabel)context.session.FindById(context.path);
                box?.SetFocus();
            }
            catch (Exception ex)
            {
                throw new Exception("GuiLabelAction Execute error ", ex);
            }
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = new Rectangle();
            try
            {
                GuiLabel gb = context.component as GuiLabel;
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
                throw new Exception("GuiLabelAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiLabel box = (GuiLabel)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiLabelAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new NotImplementedException();
        }
    }
}
