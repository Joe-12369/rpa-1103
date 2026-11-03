using Gma.UserActivityMonitor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
    public class ElementTracker11
    {
        private const Int32 HoverElementHilighterTimeoutInMs = 5000;
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly IElementHighlighter _elementHighlighter;
        private readonly Object _getElementLocker = new Object();
        //private readonly IKeyboardMouseEvents _hook;
        private readonly ConcurrentItemContainer<Point?> _nextPoint = new ConcurrentItemContainer<Point?>();
        private Boolean _captureSingleElement;
        private Boolean _capturingScreen;
        private Boolean _isLeftCtrlKeyDown;
        private Boolean _isTracking;
        private Guid? _preferredScreenId;
        public ElementTracker11(IElementHighlighter elementHighlighter)
        {
            //this._hook = keyboardMouseEvents;
            this._elementHighlighter = elementHighlighter;
        }

        private void CaptureControl(Point location)
        {
            Screen screen;
            Control control;
            try
            {
                this._elementHighlighter.Pause();
                //todo
            }
            finally
            {
                this._elementHighlighter.Resume();
                if (this._captureSingleElement)
                {
                    this.EndTrackingSession();
                }
            }
        }

        private void CleanUpTrackingSession()
        {
            EventHandler eventHandler = this.SessionEnding;
            if (eventHandler != null)
            {
                eventHandler.Invoke(this, EventArgs.Empty);
            }
            else
            {
            }
            //this._hook.KeyDown -= new KeyEventHandler(this.Hook_KeyDown);
            //this._hook.KeyUp -= new KeyEventHandler(this.Hook_KeyUp);
            //this._hook.MouseDownExt -= new EventHandler<MouseEventExtArgs>(Hook_MouseDownExt);
            //this._hook.MouseMove -= new MouseEventHandler(this.Hook_MouseMove);
            //this._hook.MouseWheelExt -= new EventHandler<MouseEventExtArgs>(Hook_MouseWheelExt);
            this._isTracking = false;
            this._isLeftCtrlKeyDown = false;
            this._captureSingleElement = false;
            this._capturingScreen = false;
            this._preferredScreenId = null;
            this._elementHighlighter.Pause();
        }

        public void EndTrackingSession()
        {
            this._isTracking = false;
        }

        private void GetElementFromPoint(Point pt)
        {
            lock (this._getElementLocker)
            {
                //todo
                this.HighlightElement(null, true);
            }
        }

        private void HighlightElement(dynamic element, Boolean useTimeout = true)
        {
            if (element == null)
            {
                this._elementHighlighter.Close();
                return;
            }
            this._elementHighlighter.Show(element.VisualRectangle, element.ElementTypeName, (useTimeout ? 5000 : 2147483647));
        }

        private void Hook_KeyDown(Object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.EndTrackingSession();
            }
            if (e.KeyCode == Keys.LControlKey)
            {
                this._isLeftCtrlKeyDown = true;
            }
        }

        private void Hook_KeyUp(Object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey)
            {
                this._isLeftCtrlKeyDown = false;
            }
        }

        private void Hook_MouseDownExt(Object sender, MouseEventExtArgs e)
        {
            //ElementTracker.<> c__DisplayClass44_0 variable = null;
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            if (this._isLeftCtrlKeyDown)
            {
                e.Handled = true;
                Point position = Cursor.Position;
                this._dispatcher.InvokeAsync(new Action(() => CaptureControl(position)));
            }
        }

        private void Hook_MouseMove(Object sender, MouseEventArgs e)
        {
            this._nextPoint.SetData(new Point?(e.Location));
        }

        private void Hook_MouseWheelExt(Object sender, MouseEventExtArgs e)
        {
            if (this._capturingScreen)
            {
                return;
            }
            e.Handled = this._isLeftCtrlKeyDown;
            if (this._isLeftCtrlKeyDown)
            {
                Task.Run(new Action(() => {
                    HighlightElement(null, true);
                }));
                return;
            }
            this._nextPoint.SetData(new Point?(e.Location));
        }

        private void SetupTrackingSession()
        {
            EventHandler eventHandler = this.SessionStarting;
            if (eventHandler != null)
            {
                eventHandler.Invoke(this, EventArgs.Empty);
            }
            else
            {
            }
            //this._hook.KeyDown += new KeyEventHandler(this.Hook_KeyDown);
            //this._hook.KeyUp += new KeyEventHandler(this.Hook_KeyUp);
            //this._hook.MouseDownExt += new EventHandler<MouseEventExtArgs>(Hook_MouseDownExt);
            //this._hook.MouseMove += new MouseEventHandler(this.Hook_MouseMove);
            //this._hook.MouseWheelExt += new EventHandler<MouseEventExtArgs>(Hook_MouseWheelExt);
            this._elementHighlighter.Resume();
        }

        private void TrackingLoop()
        {
            Boolean flag = true;
            while (this._isTracking)
            {
                if (!this._nextPoint.TakeData().HasValue)
                {
                    if (this._nextPoint.WaitForData(100) || !this._isTracking || !flag)
                    {
                        continue;
                    }
                    flag = false;
                    this.GetElementFromPoint(Cursor.Position);
                }
                else
                {
                    this.GetElementFromPoint(Cursor.Position);
                    flag = true;
                }
            }
        }

        public event EventHandler ElementCaptured;

        public event EventHandler ScreenCaptured;

        public event EventHandler SessionEnding;

        public event EventHandler SessionStarting;
    }
    public class ConcurrentItemContainer<T>
    {
        private readonly AutoResetEvent _dataUpdated;

        private readonly Object _locker;

        private T _data;

        public ConcurrentItemContainer()
        {
        }

        public void SetData(T data)
        {
            lock (this._locker)
            {
                this._data = data;
                this._dataUpdated.Set();
            }
        }

        public T TakeData()
        {
            T t;
            lock (this._locker)
            {
                this._data = default(T);
                t = this._data;
            }
            return t;
        }

        public Boolean WaitForData(Int32 ms)
        {
            return this._dataUpdated.WaitOne(ms);
        }
    }
}