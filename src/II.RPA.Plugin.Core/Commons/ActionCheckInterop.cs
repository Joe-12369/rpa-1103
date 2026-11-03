using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.Commons
{
    public static class ActionCheckInterop
    {

        [DllImport("actionCheck.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_capture_rect(int left, int top, int width, int height);

        [DllImport("actionCheck.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_comp_rate(double comp_rate);

        [DllImport("actionCheck.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void action_before();

        [DllImport("actionCheck.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int action_after();

        [DllImport("actionCheck.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_time_min(int tick);

        [DllImport("actionCheck.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void remove_log_file(int day_num);
    }
}
