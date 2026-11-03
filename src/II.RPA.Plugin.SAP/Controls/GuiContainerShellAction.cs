using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiContainerShellAction : ISapControl,IWindowDialog
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
                GuiContainerShell gb =context.component as GuiContainerShell;
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
                throw new Exception("GuiContainerShellAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            throw new NotImplementedException();
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new NotImplementedException();
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
                throw new Exception("GuiContainerShellAction close wnd error ", ex);
            }
        }
    }
}
