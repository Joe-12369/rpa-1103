using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiPasswordFieldAction : ISapControl
    {
        public void Execute(ApplicationContext context)
        {
            try
            {
                GuiTextField box = (GuiTextField)context.session.FindById(context.path);
                box.Text = context.text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiPasswordFieldAction GetText error ", ex);
            }
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = new Rectangle();
            try
            {
                GuiPasswordField gb =context.component as GuiPasswordField;
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
                throw new Exception("GuiPasswordFieldAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiPasswordField box = (GuiPasswordField)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiPasswordFieldAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            try
            {
                GuiPasswordField box = (GuiPasswordField)context.session.FindById(context.path);
                box.Text = txt;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiPasswordFieldAction SetText error ", ex);
            }
        }
    }
}
