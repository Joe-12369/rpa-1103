using sapfewse;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiCTextFieldAction:ISapControl
    {
        public void Execute(ApplicationContext context)
        {
            try
            {
                GuiCTextField txt = (GuiCTextField)context.session.FindById(context.path);
                txt.Text = context.text;
                
            }
            catch (Exception ex)
            {
                throw new Exception("GuiCTextFieldAction  Execute Error", ex);
            }
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = new Rectangle();
            try
            {
                GuiCTextField gb =context.component as GuiCTextField;
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
                throw new Exception("GuiCTextFieldAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiCTextField box = (GuiCTextField)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiCTextFieldAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            try
            {
                GuiCTextField box = (GuiCTextField)context.session.FindById(context.path);
                box.Text = txt;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiCTextFieldAction SetText error ", ex);
            }
        }
    }
}
