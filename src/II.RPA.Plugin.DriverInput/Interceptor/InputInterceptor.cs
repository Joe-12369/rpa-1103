using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

using Context = System.IntPtr;
using Device = System.Int32;
using Filter = System.UInt16;
using Precedence = System.Int32;

namespace InputInterceptorNS {

    public static class InputInterceptor
    {

        private static DllWrapper DllWrapper;

        public static Boolean Initialized = DllWrapper != null;
        public static Boolean Disposed => DllWrapper?.Disposed ?? true;

        public static Context CreateContext() => DllWrapper.CreateContext();
        public static void DestroyContext(Context context) => DllWrapper.DestroyContext(context);
        public static Precedence GetPrecedence(Context context, Device device) => DllWrapper.GetPrecedence(context, device);
        public static void SetPrecedence(Context context, Device device, Precedence precedence) => DllWrapper.SetPrecedence(context, device, precedence);
        public static Filter GetFilter(Context context, Device device) => DllWrapper.GetFilter(context, device);
        public static void SetFilter(Context context, Predicate interception_predicate, Filter filter) => DllWrapper.SetFilter(context, interception_predicate, filter);
        public static Device Wait(Context context) => DllWrapper.Wait(context);
        public static Device WaitWithTimeout(Context context, UInt64 milliseconds) => DllWrapper.WaitWithTimeout(context, milliseconds);
        public static Int32 Send(Context context, Device device, ref Stroke stroke, UInt32 nstroke) => DllWrapper.Send(context, device, ref stroke, nstroke);
        public static Int32 Receive(Context context, Device device, ref Stroke stroke, UInt32 nstroke) => DllWrapper.Receive(context, device, ref stroke, nstroke);
        public static UInt32 GetHardwareId(Context context, Device device, IntPtr hardware_id_buffer, UInt32 buffer_size) => DllWrapper.GetHardwareId(context, device, hardware_id_buffer, buffer_size);
        public static Boolean IsInvalid(Device device) => DllWrapper.IsInvalid(device) != 0;
        public static Boolean IsKeyboard(Device device) => DllWrapper.IsKeyboard(device) != 0;
        public static Boolean IsMouse(Device device) => DllWrapper.IsMouse(device) != 0;

        static InputInterceptor()
        {
            DllWrapper = null;
        }

        public static Boolean CheckDriverInstalled()
        {
            RegistryKey baseRegistryKey = Registry.LocalMachine.OpenSubKey("SYSTEM").OpenSubKey("CurrentControlSet").OpenSubKey("Services");
            RegistryKey keyboardRegistryKey = baseRegistryKey.OpenSubKey("keyboard");
            RegistryKey mouseRegistryKey = baseRegistryKey.OpenSubKey("mouse");
            if (keyboardRegistryKey == null || mouseRegistryKey == null)
                return false;
            if ((String)keyboardRegistryKey.GetValue("DisplayName", String.Empty) != "Keyboard Upper Filter Driver")
                return false;
            if ((String)mouseRegistryKey.GetValue("DisplayName", String.Empty) != "Mouse Upper Filter Driver")
                return false;
            return true;
        }

        public static Boolean Initialize()
        {
            if (Initialized)
                return true;

            var dllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "interception.dll");
            DllWrapper = new DllWrapper(dllPath);
            return true;
        }

        public static Boolean Dispose()
        {
            if (Disposed)
                return true;

            DllWrapper.Dispose();
            DllWrapper = null;
            return true;
        }

        /// <summary>
        /// 获取当前输入设备驱动列表
        /// </summary>
        /// <returns></returns>
        public static List<DeviceData> GetDeviceDatas()
        {
            Context context = InputInterceptor.CreateContext();
            Predicate predicate = InputInterceptor.IsKeyboard;
            return GetDeviceList(context, predicate);
        }

        public static Device SetRandomDevice(string strDeviceName)
        {
            List<DeviceData> deviceDatas = GetDeviceDatas();
            int index = II.RPA.Plugin.DriverInput.Utils.DeviceChoiceHelper.GetDeviceIndex(deviceDatas, strDeviceName);
            if (index == -1)
                return -1;
            return deviceDatas[index].Device;
        }

        public static List<DeviceData> GetDeviceList(Context context, Predicate predicate = null)
        {
            List<DeviceData> result = new List<DeviceData>();
            Char[] buffer = new Char[1024];
            GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr bufferPtr = gcHandle.AddrOfPinnedObject();
            for (Device device = 1; device <= 20; device += 1)
            {
                if (predicate == null ? !IsInvalid(device) : predicate(device))
                {
                    UInt32 length = GetHardwareId(context, device, bufferPtr, (UInt32)buffer.Length);
                    if (length > 0)
                    {
                        String name = new String(buffer, 0, (Int32)length);
                        result.Add(new DeviceData(device, name));
                    }
                }
            }
            gcHandle.Free();
            return result;
        }
    }
}
