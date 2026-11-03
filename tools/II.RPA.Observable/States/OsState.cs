using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Observable.Reactives;
using II.RPA.Observable.States.OsStates;

namespace II.RPA.Observable.Contracts.States
{
    public class OsState : ReactiveObject<OsState>
    {
        #region 当前用户

        public string UserName { get; set; } = Environment.UserName;

        #endregion

        #region Rx-会话信息

        private OsSession osSession = OsSession.Empty;

        public OsSession OsSession
        {
            get { return osSession; }
            set { }
        }

        #endregion

        #region Rx-屏幕信息

        private ScreenInfo screenInfo = ScreenInfo.Empty;

        public ScreenInfo ScreenInfo
        {
            get { return screenInfo; }
            set { SetProperty(ref screenInfo, value); }
        }

        #endregion

        #region Rx-锁屏信息

        private bool isScreenLocked;

        public bool IsScreenLocked
        {
            get { return isScreenLocked; }
            set { SetProperty(ref isScreenLocked, value); }
        }

        #endregion

        public override string ToString()
        {
            return $@"{nameof(UserName)}:{UserName}
{nameof(ScreenInfo)}:{ScreenInfo}
{nameof(OsSession)}:{OsSession}
{nameof(IsScreenLocked)}:{IsScreenLocked}";
        }
    }
}
