using II.RPA.Logging;
using II.RPA.Plugin.DriverInput.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

using Context = System.IntPtr;
using Device = System.Int32;
using Filter = System.UInt16;

namespace InputInterceptorNS {

    public abstract class Hook<TCallbackStroke> : IDisposable {
        private static ILogger logger = Logger.OfContext();
        public delegate void CallbackAction(ref TCallbackStroke stroke);

        public Context Context { get; private set; }
        public Device Device { get; private set; }
        public Device RandomDevice { get;  set; }
        public Filter FilterMode { get; private set; }
        public Predicate Predicate { get; private set; }
        public CallbackAction Callback { get; private set; }
        public Exception Exception { get; private set; }
        public Boolean Active { get; private set; }
        public Thread Thread { get; private set; }

        public Boolean IsInitialized => this.Context != Context.Zero && this.Device != -1;
        public Boolean CanSimulateInput => this.Context != Context.Zero && (this.Device != -1 || this.RandomDevice != -1);
        public Boolean HasException => this.Exception != null;

        protected Device AnyDevice => this.Device != -1 ? this.Device : this.RandomDevice;

        protected abstract void CallbackWrapper(ref Stroke stroke);

        #region 驱动选择对话框 使用
        int CurrentDeviceIndex = 0;

        public Func<List<object>, int> funcDeviceChoice;
        public void FuncDeviceChoice(int deviceIndex)
        {
            CurrentDeviceIndex = deviceIndex;
            logger.LogDebug($"CurrentDeviceIndex:{CurrentDeviceIndex}");
        }
        #endregion


        public Hook(Filter filterMode, Predicate predicate, CallbackAction callback)
        {
            Context context = InputInterceptor.CreateContext();
            List<DeviceData> devices = InputInterceptor.GetDeviceList(context, predicate);
            this.Context = context;
            this.Device = -1;
            //DeviceChoiceHelper.RecordDeviceList(devices);
            //var deviceIndex = DeviceChoiceHelper.GetDeviceIndex(devices);
            //logger.LogInformation($"当前驱动序号:{deviceIndex}");
            //if (deviceIndex > -1)
            //{
            //    this.RandomDevice = devices[deviceIndex].Device;
            //}
            //else
            {
                this.RandomDevice = devices.Count > 0 ? devices[0].Device : -1;
            }
           
            this.FilterMode = filterMode;
            this.Predicate = predicate;
            this.Callback = callback;
            this.Exception = null;
            if (this.Context != Context.Zero) {
                this.Active = filterMode != 0 || callback != null;
                this.Thread = new Thread(this.InterceptionMain);
#if !NETSTANDARD1_3
                this.Thread.Priority = this.Callback != null ? ThreadPriority.Highest : ThreadPriority.Normal;
#endif
                this.Thread.IsBackground = true;
                this.Thread.Start();
            } else {
                this.Active = false;
                this.Thread = null;
            }
        }

       

        private void InterceptionMain() {
            InputInterceptor.SetFilter(this.Context, this.Predicate, this.FilterMode);
            Device device;
            Stroke stroke = new Stroke();
            while (this.Active) {
                device = InputInterceptor.WaitWithTimeout(this.Context, 100);
                if (InputInterceptor.Receive(this.Context, device, ref stroke, 1) > 0) {
                    this.Device = device;
                    if (this.Active && this.Callback != null) {
                        try {
                            this.CallbackWrapper(ref stroke);
                        } catch (Exception exception) {
                            this.Exception = exception;
                            this.Active = false;
                        }
                    }
                    InputInterceptor.Send(this.Context, device, ref stroke, 1);
                }
            }
        }

        public void Dispose() {
            if (this.Context != Context.Zero) {
                if (this.Active) {
                    this.Active = false;
                    this.Thread.Join();
                }
                InputInterceptor.DestroyContext(this.Context);
                this.Context = Context.Zero;
            }
        }

    }

}
