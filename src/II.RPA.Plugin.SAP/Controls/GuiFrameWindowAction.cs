using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiFrameWindowAction : ISapControl
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
                GuiFrameWindow gb =context.component as GuiFrameWindow;
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
                throw new Exception("GuiFrameWindowAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiFrameWindow box = (GuiFrameWindow)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiFrameWindowAction GetText error ", ex);
            }
        }
        public void CloseWindow(ApplicationContext context)
        {
            try
            {
                GuiFrameWindow box = (GuiFrameWindow)context.session.FindById(context.path);
                box.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("GuiFrameWindowAction close wnd error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            try
            {
                GuiFrameWindow box = (GuiFrameWindow)context.session.FindById(context.path);
                box.Text = txt;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiFrameWindowAction SetText error ", ex);
            }
        }
    }
}
