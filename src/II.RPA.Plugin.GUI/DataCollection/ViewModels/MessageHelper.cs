using II.RPA.Plugin.GUI.DataCollection.Windows;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace II.RPA.Plugin.GUI.DataCollection.ViewModels
{
    internal class MessageHelper
    {
        #region 父窗体对象
        public static Window ParentWnd = null;
        public static Window CurrentDialogWnd = null;
        public static void SetParentWnd(Window _parentWnd)
        {
            ParentWnd = _parentWnd;
        }

        /// <summary>
        /// 保存当前二次弹窗的对象
        /// </summary>
        /// <param name="_parentWnd"></param>
        public static void SetCurrentDialogWnd(Window _CurrentDialogWnd)
        {
            CurrentDialogWnd = _CurrentDialogWnd;        }

        public static Window GetParentWnd()
        {
            return ParentWnd;
        }

        public static void DoCancle()
        {
            ParentWnd = null;
            if (CurrentDialogWnd != null && CurrentDialogWnd.IsLoaded == true)
            {
                if (IsModal(CurrentDialogWnd))
                {
                    CurrentDialogWnd.DialogResult = false;
                    CurrentDialogWnd?.Close();
                }
                else
                {
                    CurrentDialogWnd?.Close();
                }
            }
        }


        public static bool IsModal(Window window)
        {
            return (bool)typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
        }

        public static void ShowShadow(bool ShadowActive)
        {
            GlobalData.GetInstance().VM_MainWindowViewModel.ShowShadow(ShadowActive);
        }

        public static void Dipose()
        {
            ParentWnd = null;
        }
        #endregion

        #region 报警
        /// <summary>
        /// 异步报警
        /// </summary>
        public static bool ShowMessage(string strMessage, string btnConfirmContent = "")
        {
            if (string.IsNullOrEmpty(btnConfirmContent))
            {
                btnConfirmContent = eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage();
            }

            bool bRet = false;
            WarningWnd.ShowWarningMessage(strMessage, (s, e) =>
            {
                if (e.Result.IsYes)
                {
                    bRet = true;
                }
                else
                {
                    //选择了No
                    bRet = false;
                }
            }, btnConfirmContent);
            return bRet;
        }

        public static bool ShowDialog(string strMessage, string btnConfirmContent = "")
        {
            if (string.IsNullOrEmpty(btnConfirmContent))
            {
                btnConfirmContent = eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage();
            }
            var _Result = ShowWarningMessageDialog(strMessage, btnConfirmContent, ParentWnd);
            //DataCollectingManager.ShowLoadWaiting(false); 
            if (_Result == null)
            {
                return false;
            }

            if (_Result.IsYes)
            {
                //选择了Yes
                return true;
            }
            else
            {
                //选择了No
                return false;
            }
        }
        public static MessageResult ShowWarningMessageDialog(string strContent, string strConfirmButtonContent = "", Window parentWnd = null)
        {
            if (string.IsNullOrEmpty(strConfirmButtonContent))
            {
                strConfirmButtonContent = eCommonMessageType.CVScreenWnd_Designer_cs_QueDing.GetMessage();
            }

            object ret = DataCollectingManager.UIThreadRunAction(() =>
            {
                MessageResult _MessageResult = null;
                WarningWnd WarningWnd = new WarningWnd();
                WarningWnd.Context = strContent;
                WarningWnd.ConfirmContent = strConfirmButtonContent;
                CurrentDialogWnd = WarningWnd;
                if (parentWnd != null)
                {
                    MessageHelper.ShowShadow(true);
                    WarningWnd.Owner = parentWnd;
                }

                WarningWnd.Result += (s, e) =>
                {
                    _MessageResult = e.Result;
                };
                WarningWnd.ShowDialog();
                MessageHelper.ShowShadow(false);
                return _MessageResult;
            });
            return (MessageResult)ret;
        }
        #endregion
        #region 报错
        private static ErrorMessageWnd ErrorMessageWnd = null;
        private static int ErrorMessageCount = 0;
        //object objLock = new object();

        internal static void ShowWeakError(Exception ex, string strMessage = null)
        {
            if (ex is RPAException)
            {
                strMessage = ((RPAException)ex).ErrorMessageContent;
            }

            if (!string.IsNullOrEmpty(strMessage))
            {
                ShowWeakError(strMessage);
            }
        }

        /// <summary>
        /// SetShowWeakError 屏幕显示弱提示
        /// </summary>
        public static void SetShowWeakError(Action<string> ParamShowWeakError)
        {
            _ShowErrorHelper = ParamShowWeakError;
        }

        /// <summary>
        /// SetShowWeakError 看板显示弱提示
        /// </summary>
        public static void SetShowWeakErrorOnPanel(Action<string> ParamShowWeakErrorOnPanel)
        {
            _ShowErrorHelperOnPanel = ParamShowWeakErrorOnPanel;
        }

        static Action<string> _ShowErrorHelper = null;
        static Action<string> _ShowErrorHelperOnPanel = null;

        /// <summary>
        /// 报错 显示报警信息
        /// </summary>
        /// <param name="strMessage"></param>
        /// <returns></returns>
        internal static void ShowWeakError(string strMessage,bool isOnPanel= false)
        {
            if (!isOnPanel)
            {
                if (_ShowErrorHelper == null)
                {
                    return;
                }
                else
                {
                    _ShowErrorHelper(strMessage);
                }
            }
            else
            {
                if (_ShowErrorHelperOnPanel == null)
                {
                    return;
                }
                else
                {
                    _ShowErrorHelperOnPanel(strMessage);
                }
            }
        }
        #endregion
    }
}
