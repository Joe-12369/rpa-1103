using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiDialogShellAction : ISapControl,IWindowDialog
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
                GuiDialogShell gb =context.component as GuiDialogShell;
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
                throw new Exception("GuiDialogShellAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiDialogShell box = (GuiDialogShell)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiDialogShellAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            try
            {
                GuiDialogShell box = (GuiDialogShell)context.session.FindById(context.path);
                box.Text = txt;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiDialogShellAction GetText error ", ex);
            }
        }
        public void CloseWindow(ApplicationContext context)
        {
            try
            {
                GuiDialogShell box = (GuiDialogShell)context.session.FindById(context.path);
                box.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("GuiDialogShellAction close wnd error ", ex);
            }
        }
    }
}
