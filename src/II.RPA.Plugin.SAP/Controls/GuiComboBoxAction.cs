using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sapfewse;

namespace II.RPA.Plugin.SAP.Controls
{
    class GuiComboBoxAction : ISapControl
    {
        public void Execute(ApplicationContext context)
        {
            try
            {
                GuiComboBox box = (GuiComboBox)context.session.FindById(context.path);
                box.Value = context.text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiComboBoxAction GetText error ", ex);
            }
        }

        public Rectangle GetRectangle(ApplicationContext context)
        {
            Rectangle pr = new Rectangle();
            try
            {
                GuiComboBox gb =context.component as GuiComboBox;
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
                throw new Exception("GuiComboBoxAction throw ex", ex);
            }
            return pr;
        }

        public string GetText(ApplicationContext context)
        {
            try
            {
                GuiComboBox box = (GuiComboBox)context.session.FindById(context.path);
                return box.Text;
            }
            catch (Exception ex)
            {
                throw new Exception("GuiComboBoxAction GetText error ", ex);
            }
        }

        public void SetText(ApplicationContext context, string txt)
        {
            throw new NotImplementedException();
        }
    }
}
