using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiOKCodeAction : ISapControl
    {
        public void Execute(ApplicationContext context)
        {
            try
            {
                GuiOkCodeField box = (GuiOkCodeField)context.session.FindById(context.path);
                box.Opened = context.opened;
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
                GuiOkCodeField gb =context.component as GuiOkCodeField;
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
                throw new Exception("GuiOKCodeAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiOkCodeField box = (GuiOkCodeField)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiOKCodeAction GetText error ", ex);
            }
        }

        public bool GetOpened(ApplicationContext context)
        {
            try
            {
                GuiOkCodeField box = (GuiOkCodeField)context.session.FindById(context.path);
                return box.Opened;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiOKCodeAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            try
            {
                GuiOkCodeField box = (GuiOkCodeField)context.session.FindById(context.path);
                box.Text = txt;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiOKCodeAction SetText error ", ex);
            }
        }
    }
}
