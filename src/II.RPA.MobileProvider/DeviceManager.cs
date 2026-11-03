using II.RPA.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace II.RPA.MobileProvider
{
    public class DeviceManager
    {
        private readonly static ILogger _logger = Logger.OfContext();
        public static List<DeviceInfo> GetAllDevice()
        {
            var allDevices = new List<DeviceInfo>();
            try
            {
                string deviceFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Factory/mobileDeviceList.json");
                if (File.Exists(deviceFilePath))
                {
                    string deviceContent = File.ReadAllText(deviceFilePath);
                    if (!string.IsNullOrEmpty(deviceContent))
                    {
                        allDevices = JsonConvert.DeserializeObject<List<DeviceInfo>>(deviceContent);
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"获取手机管理设备信息异常");
            }
            return allDevices;
        }
        public static List<DeviceInfo> GetAllReadyDevice(List<DeviceInfo> currentList)
        {
            var devices = new List<DeviceInfo>();
            try
            {
                var currentDevices = AdbDeviceHelper.GetConnectedDevices();
                var allDevices = currentList;// GetAllDevice();
                devices = allDevices.Where(a => currentDevices.Any(b => b.IsReady && b.Udid == a.Udid)).ToList();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"获取手机管理设备信息中已连接的手机异常");
            }
            return devices;
        }
        public static DeviceInfo GetAnyOneReadyDevice(List<DeviceInfo> currentList)
        {
            DeviceInfo device = null;
            try
            {
                var currentDevices = AdbDeviceHelper.GetConnectedDevices();
                var allDevices = currentList; // GetAllDevice();
                device = allDevices.FirstOrDefault(a => currentDevices.Any(b => b.IsReady && b.Udid == a.Udid));
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(_logger, ex, $"获取手机管理设备信息中已连接任意一台手机异常");
            }
            return device;
        }

        /// <summary>
        /// 根据起始点、滑动距离、角度计算结束位置
        /// </summary>
        /// <param name="startX">起始点X坐标</param>
        /// <param name="startY">起始点Y坐标</param>
        /// <param name="distance">滑动距离（像素）</param>
        /// <param name="angle">角度（0°=右，90°=上，180°=左，270°=下）</param>
        /// <returns>结束位置坐标 (endX, endY)</returns>
        public static (double endX, double endY) CalculateEndPosition(double startX, double startY, double distance, double angle)
        {
            // 将角度转换为弧度
            double radians = angle * Math.PI / 180.0;

            // 计算坐标增量
            double dx = distance * Math.Cos(radians);
            double dy = distance * Math.Sin(radians);

            // 屏幕坐标系中Y轴向下，"向上"时Y应减小
            double endX = startX + dx;
            double endY = startY - dy;

            return (endX, endY);
        }
        /// <summary>
        /// 计算结束位置（带边界限制）
        /// </summary>
        /// <param name="startX">起始点X坐标</param>
        /// <param name="startY">起始点Y坐标</param>
        /// <param name="distance">滑动距离（像素）</param>
        /// <param name="angle">角度（度）</param>
        /// <param name="screenWidth">屏幕宽度</param>
        /// <param name="screenHeight">屏幕高度</param>
        /// <returns>限制后的结束位置坐标</returns>
        public static (double endX, double endY) CalculateEndPositionWithBounds(
            double startX, double startY,
            double distance, double angle,
            int screenWidth, int screenHeight)
        {
            // 计算未限制的位置
            var (endX, endY) = CalculateEndPosition(startX, startY, distance, angle);

            // 限制在屏幕范围内
            endX = Math.Max(0, Math.Min(endX, screenWidth));
            endY = Math.Max(0, Math.Min(endY, screenHeight));

            return (endX, endY);
        }
    }
    public class DeviceInfo
    {
        public string Udid { get; set; }
        public bool IsReady { get; set; }
        public string Model { get; set; }
        public string Version { get; set; }
        public string Brand { get; set; }
        public string Name { get; set; }
        public string PlatformName { get; set; } = "android";
    }
}

