using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenCapture
{
    public static class HelpersOptions
    {
        public const int RecentColorsMax = 32;

        public static ProxyInfo CurrentProxy { get; set; } = new ProxyInfo();
        public static bool AcceptInvalidSSLCertificates { get; set; } = false;
        public static bool DefaultCopyImageFillBackground { get; set; } = true;
        public static bool UseAlternativeClipboardCopyImage { get; set; } = false;
        public static bool UseAlternativeClipboardGetImage { get; set; } = false;
        public static bool RotateImageByExifOrientationData { get; set; } = true;
        public static string BrowserPath { get; set; } = "";
        public static List<Color> RecentColors { get; set; } = new List<Color>();
        public static string LastSaveDirectory { get; set; } = "";
        public static bool URLEncodeIgnoreEmoji { get; set; } = false;
        public static Dictionary<string, string> ShareXSpecialFolders { get; set; } = new Dictionary<string, string>();
        public static bool DevMode { get; set; } = false;
    }
}
