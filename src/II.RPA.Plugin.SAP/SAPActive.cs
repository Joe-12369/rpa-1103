using sapfewse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.SAP
{
    /// <summary>
    /// SAP GUI 主动连接与登录辅助类（遗留 API）。
    /// 内部通过 SapNode.ConnectionManager 借用共享连接，避免重复创建 COM 对象。
    /// </summary>
    public class SAPActive
    {
        private static SapConnectionManager ConnectionManager => SapNode.ConnectionManager;

        public static GuiApplication SapGuiApp
        {
            get { return ConnectionManager.App; }
        }

        public static GuiConnection SapConnection
        {
            get { return ConnectionManager.Connection; }
        }

        public static GuiSession SapSession
        {
            get { return ConnectionManager.CurrentSession; }
        }

        public static void openSap(string env)
        {
            ConnectionManager.Connect();

            string connectString = null;
            if (env.ToUpper().Equals("DEFAULT"))
            {
                connectString = "1.0 Test ERP (DEFAULT)";
            }
            else
            {
                connectString = env;
            }

            try
            {
                ConnectionManager.App?.OpenConnection(connectString, Sync: true);
            }
            catch
            {
                // 可能已有目标连接，继续使用现有连接
            }
        }

        public static void login(string myclient, string mylogin, string mypass, string mylang)
        {
            var session = ConnectionManager.CurrentSession;
            if (session == null)
                throw new InvalidOperationException("SAP Session 不可用，请先调用 openSap。");

            GuiTextField client = (GuiTextField)session.ActiveWindow.FindByName("RSYST-MANDT", "GuiTextField");
            GuiTextField login = (GuiTextField)session.ActiveWindow.FindByName("RSYST-BNAME", "GuiTextField");
            GuiTextField pass = (GuiTextField)session.ActiveWindow.FindByName("RSYST-BCODE", "GuiPasswordField");
            GuiTextField language = (GuiTextField)session.ActiveWindow.FindByName("RSYST-LANGU", "GuiTextField");

            client.SetFocus();
            client.Text = myclient;
            login.SetFocus();
            login.Text = mylogin;
            pass.SetFocus();
            pass.Text = mypass;
            language.SetFocus();
            language.Text = mylang;

            //Press the green checkmark button which is about the same as the enter key
            GuiButton btn = (GuiButton)session.FindById("/app/con[0]/ses[0]/wnd[0]/tbar[0]/btn[0]");
            btn.SetFocus();
            btn.Press();
        }
    }
}
