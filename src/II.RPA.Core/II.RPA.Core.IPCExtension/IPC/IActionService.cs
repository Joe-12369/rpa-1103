using System;
using II.RPA.IPC;

namespace II.RPA.Core.IPCExtension.IPC
{
    public interface IActionService
    {
        IResponseResult Request(IRequestParams request);
    }
}