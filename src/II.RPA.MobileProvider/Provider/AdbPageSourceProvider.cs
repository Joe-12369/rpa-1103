using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace II.RPA.MobileProvider.Provider
{
    internal sealed class AdbPageSourceProvider
    {
        private const int StateTimeoutMilliseconds = 5000;
        private const int DumpTimeoutMilliseconds = 15000;
        private const int ReadTimeoutMilliseconds = 10000;
        private readonly static ILogger _logger = Logger.OfContext();

        public async Task<string> GetPageSourceAsync(string udid)
        {
            string requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
            string remotePath = $"/sdcard/ii_rpa_window_dump_{requestId}.xml";
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                string state = await AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync($"-s {Quote(udid)} get-state", StateTimeoutMilliseconds);
                if (!state.Trim().Equals("device", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"ADB设备状态不可用[{state.Trim()}]");
                }

                await TryDeleteAsync(udid, remotePath);
                Stopwatch dumpStopwatch = Stopwatch.StartNew();
                await AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync(
                    $"-s {Quote(udid)} shell uiautomator dump --compressed {remotePath}", DumpTimeoutMilliseconds);
                dumpStopwatch.Stop();
                Stopwatch readStopwatch = Stopwatch.StartNew();
                string rawXml = await AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync(
                    $"-s {Quote(udid)} exec-out cat {remotePath}", ReadTimeoutMilliseconds);
                readStopwatch.Stop();
                string wmSize = await TryGetWindowSizeAsync(udid);
                Stopwatch normalizeStopwatch = Stopwatch.StartNew();
                string xml = NormalizeAndValidate(rawXml, wmSize);
                normalizeStopwatch.Stop();
                stopwatch.Stop();
                _logger.LogInformation($"ADB PageSource获取成功，请求[{requestId}]，dump耗时[{dumpStopwatch.ElapsedMilliseconds}ms]，读取耗时[{readStopwatch.ElapsedMilliseconds}ms]，规范化耗时[{normalizeStopwatch.ElapsedMilliseconds}ms]，总耗时[{stopwatch.ElapsedMilliseconds}ms]，XML长度[{xml.Length}]，设备[{udid}]");
                return xml;
            }
            finally
            {
                await TryDeleteAsync(udid, remotePath);
            }
        }

        private static async Task<string> TryGetWindowSizeAsync(string udid)
        {
            try
            {
                return await AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync(
                    $"-s {Quote(udid)} shell wm size", StateTimeoutMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"读取手机wm size失败，将使用元素bounds推算页面尺寸，设备[{udid}]，原因[{ex.Message}]");
                return string.Empty;
            }
        }

        private static string NormalizeAndValidate(string rawXml, string wmSize)
        {
            if (string.IsNullOrWhiteSpace(rawXml)) throw new XmlException("ADB PageSource为空");
            int start = rawXml.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
            if (start < 0) start = rawXml.IndexOf("<hierarchy", StringComparison.OrdinalIgnoreCase);
            if (start < 0) throw new XmlException("ADB PageSource未找到XML根节点");

            string xml = rawXml.Substring(start).Trim().TrimStart('\uFEFF');
            var document = new XmlDocument();
            document.LoadXml(xml);
            XmlElement root = document.DocumentElement;
            if (root == null || !root.Name.Equals("hierarchy", StringComparison.OrdinalIgnoreCase))
                throw new XmlException("ADB PageSource根节点不是hierarchy");

            XmlNodeList nodes = document.SelectNodes("//*[@bounds]");
            if (nodes == null || nodes.Count == 0) throw new XmlException("ADB PageSource不包含有效元素节点");
            EnsureDimensions(root, nodes, wmSize);
            return document.OuterXml;
        }

        private static void EnsureDimensions(XmlElement root, XmlNodeList nodes, string wmSize)
        {
            int width;
            int height;
            bool hasWidth = int.TryParse(root.GetAttribute("width"), out width) && width > 0;
            bool hasHeight = int.TryParse(root.GetAttribute("height"), out height) && height > 0;
            if (hasWidth && hasHeight) return;

            MatchCollection sizeMatches = Regex.Matches(wmSize ?? string.Empty, @"(\d+)x(\d+)");
            if (sizeMatches.Count > 0)
            {
                Match sizeMatch = sizeMatches.Cast<Match>().Last();
                width = int.Parse(sizeMatch.Groups[1].Value);
                height = int.Parse(sizeMatch.Groups[2].Value);
                int rotation;
                if (int.TryParse(root.GetAttribute("rotation"), out rotation) && (rotation == 1 || rotation == 3))
                {
                    int originalWidth = width;
                    width = height;
                    height = originalWidth;
                }
            }
            else
            {
                GetDimensionsFromBounds(nodes, out width, out height);
            }

            if (!hasWidth && width > 0) root.SetAttribute("width", width.ToString());
            if (!hasHeight && height > 0) root.SetAttribute("height", height.ToString());
        }

        private static void GetDimensionsFromBounds(XmlNodeList nodes, out int width, out int height)
        {
            width = 0;
            height = 0;
            var boundsRegex = new Regex(@"\[(-?\d+),(-?\d+)\]\[(-?\d+),(-?\d+)\]", RegexOptions.Compiled);
            foreach (XmlNode node in nodes)
            {
                Match match = boundsRegex.Match(node.Attributes?["bounds"]?.Value ?? string.Empty);
                if (!match.Success) continue;
                int right;
                int bottom;
                if (int.TryParse(match.Groups[3].Value, out right)) width = Math.Max(width, right);
                if (int.TryParse(match.Groups[4].Value, out bottom)) height = Math.Max(height, bottom);
            }
        }

        private static async Task TryDeleteAsync(string udid, string remotePath)
        {
            try
            {
                await AdbDeviceHelper.ExecuteAdbCommandWithTimeoutAsync(
                    $"-s {Quote(udid)} shell rm -f {remotePath}", StateTimeoutMilliseconds);
            }
            catch { }
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }
    }
}