using Gma.UserActivityMonitor;
using System;
using System.Windows;
using System.Windows.Forms;

namespace II.RPA.MobileDeviceManager.Services
{
    internal class ScrcpyInputMonitor : IDisposable
    {
        private readonly Func<Rect> _getScreenRect;
        private readonly Func<bool> _shouldSuppressInput;
        private readonly Action<string> _onInputObserved;
        private DateTime _lastObservedTime = DateTime.MinValue;
        private bool _isStarted;

        public ScrcpyInputMonitor(Func<Rect> getScreenRect, Func<bool> shouldSuppressInput, Action<string> onInputObserved)
        {
            _getScreenRect = getScreenRect;
            _shouldSuppressInput = shouldSuppressInput;
            _onInputObserved = onInputObserved;
        }

        public void Start()
        {
            if (_isStarted)
            {
                return;
            }

            HookManager.MouseClickExt += HookManager_MouseClickExt;
            HookManager.MouseMoveExt += HookManager_MouseMoveExt;
            HookManager.MouseUp += HookManager_MouseUp;
            HookManager.KeyDown += HookManager_KeyDown;
            _isStarted = true;
        }

        public void Stop()
        {
            if (!_isStarted)
            {
                return;
            }

            HookManager.MouseClickExt -= HookManager_MouseClickExt;
            HookManager.MouseMoveExt -= HookManager_MouseMoveExt;
            HookManager.MouseUp -= HookManager_MouseUp;
            HookManager.KeyDown -= HookManager_KeyDown;
            _isStarted = false;
        }

        private void HookManager_MouseClickExt(object sender, MouseEventExtArgs e)
        {
            NotifyIfInScreen(new System.Drawing.Point(e.X, e.Y), "ScrcpyMouseClick");
        }

        private void HookManager_MouseMoveExt(object sender, MouseEventExtArgs e)
        {
            if (Control.MouseButtons == MouseButtons.Left || Control.MouseButtons == MouseButtons.Right)
            {
                NotifyIfInScreen(new System.Drawing.Point(e.X, e.Y), "ScrcpyMouseDrag");
            }
        }

        private void HookManager_MouseUp(object sender, MouseEventArgs e)
        {
            NotifyIfInScreen(new System.Drawing.Point(e.X, e.Y), "ScrcpyMouseUp");
        }

        private void HookManager_KeyDown(object sender, KeyEventArgs e)
        {
            System.Drawing.Point cursorPoint = Control.MousePosition;
            NotifyIfInScreen(cursorPoint, "ScrcpyKeyDown");
        }

        private void NotifyIfInScreen(System.Drawing.Point screenPoint, string reason)
        {
            if (_shouldSuppressInput?.Invoke() == true)
            {
                return;
            }

            Rect screenRect = _getScreenRect();
            if (!screenRect.Contains(new System.Windows.Point(screenPoint.X, screenPoint.Y)))
            {
                return;
            }

            DateTime now = DateTime.Now;
            if ((now - _lastObservedTime).TotalMilliseconds < 200)
            {
                return;
            }

            _lastObservedTime = now;
            _onInputObserved?.Invoke(reason);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
