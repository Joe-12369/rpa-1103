using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiCheckBoxAction : ISapControl
    {
        public void Execute(ApplicationContext context)
        {
            try
            {
                GuiCheckBox button = (GuiCheckBox)context.session.FindById(context.path);
                button.Selected = true;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiCheckBoxAction  Execute Error", ex);
            }
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = new Rectangle();
            try
            {
                GuiCheckBox gb =context.component as GuiCheckBox;
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
                throw new Exception("GuiCheckBoxAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiCheckBox box = (GuiCheckBox)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiCheckBoxAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new NotImplementedException();
        }

        public bool GetSelected(ApplicationContext context)
        {
            try
            {
                GuiCheckBox box = (GuiCheckBox)context.session.FindById(context.path);
                return box.Selected;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiCheckBoxAction GetSelected error ", ex);
            }
        }

        public bool SetSelected(ApplicationContext context,bool isCheck)
        {
            try
            {
                GuiCheckBox box = (GuiCheckBox)context.session.FindById(context.path);
                box.Selected = isCheck;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiCheckBoxAction SetSelected error ", ex);
            }
        }
    }
}
