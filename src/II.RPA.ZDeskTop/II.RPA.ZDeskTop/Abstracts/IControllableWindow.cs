using II.Framework.Models;
using II.Framework.PInvoker;
using II.RPA.ZDeskTop.Models;
using II.RPA.ZDeskTop.Services;
using System;

namespace II.RPA.ZDeskTop.Abstracts
{
    public interface IControllableWindow
    {
        void SetMiniMode(ParentWindowModel parent);
        void CancelMiniMode();
        void SetWindowRegion(int x, int y, int w, int h);
        void Minimize();
        void Maximize();
        void AllowOperation(bool isAllow);
        void SetTopmost(bool isTopmost);
        void ActivateWindow();
        IObservable<SnapshotModel> Capture(int intervalms);
        WindowPInvoker PInvoker
        {
            get;
        }
        Rect Rect 
        { 
            get; 
        }
        Status Status 
        { 
            get; 
        }
        OverlayService OverlayService 
        { 
            get; 
        }
        bool IsMiniOn
        {
            get;
        }
    }
}
