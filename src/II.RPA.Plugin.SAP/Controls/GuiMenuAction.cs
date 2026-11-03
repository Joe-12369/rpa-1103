using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiMenuAction : ISapControl
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
                GuiMenu gb =context.component as GuiMenu;
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
                throw new Exception("GuiMenuAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiMenu box = (GuiMenu)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiMenuAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            try
            {
                GuiMenu box = (GuiMenu)context.session.FindById(context.path);
                box.Text = txt;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiMenuAction SetText error ", ex);
            }
        }
    }
}
