using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiMainWindowAction : ISapControl,IWindowDialog
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
                GuiMainWindow gb =context.component as GuiMainWindow;
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
                throw new Exception("GuiMainWindowAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiMainWindow box = (GuiMainWindow)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiMainWindowAction GetText error ", ex);
            }
        }

        public void CloseWindow(ApplicationContext context)
        {
            try
            {
                GuiMainWindow box = (GuiMainWindow)context.session.FindById(context.path);
                box.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("GuiMainWindowAction close wnd error ", ex);
            }
        }

        public void SetStatusVisible(ApplicationContext context, bool visible)
        {
            try
            {
                GuiMainWindow box = (GuiMainWindow)context.session.FindById(context.path);
                box.StatusbarVisible = visible;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiMainWindowAction SetStatusVisible wnd error ", ex);
            }
        }

        public void SetButtonbarVisible(ApplicationContext context, bool visible)
        {
            try
            {
                GuiMainWindow box = (GuiMainWindow)context.session.FindById(context.path);
                box.ButtonbarVisible = visible;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiMainWindowAction SetButtonbarVisible wnd error ", ex);
            }
        }
        public void SetTitlebarVisible(ApplicationContext context, bool visible)
        {
            try
            {
                GuiMainWindow box = (GuiMainWindow)context.session.FindById(context.path);
                box.TitlebarVisible = visible;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiMainWindowAction SetTitlebarVisible wnd error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            try
            {
                GuiMainWindow box = (GuiMainWindow)context.session.FindById(context.path);
                box.Text = txt;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiMainWindowAction SetText error ", ex);
            }
        }
    }
}
