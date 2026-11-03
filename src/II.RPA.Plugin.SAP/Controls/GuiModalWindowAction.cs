using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiModalWindowAction : ISapControl,IWindowDialog
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
                GuiModalWindow gb =context.component as GuiModalWindow;
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
                throw new Exception("GuiModalWindowAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiModalWindow box = (GuiModalWindow)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiModalWindowAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            try
            {
                GuiModalWindow box = (GuiModalWindow)context.session.FindById(context.path);
                box.Text = txt;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiModalWindowAction SetText error ", ex);
            }
        }
        public void CloseWindow(ApplicationContext context)
        {
            try
            {
                GuiModalWindow box = (GuiModalWindow)context.session.FindById(context.path);
                box.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("GuiModalWindowAction close wnd error ", ex);
            }
        }
    }
}
