using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Observable.States.OsStates
{
    public enum ConnectionState
    {
#pragma warning disable SA1602 // Enumeration items should be documented
        Active,
        Connected,
        ConnectQuery,
        Shadowing,
        Disconnected,
        Idle,
        Listening,
        Reset,
        Down,
        Initializing,
#pragma warning restore SA1602 // Enumeration items should be documented
    }
}
