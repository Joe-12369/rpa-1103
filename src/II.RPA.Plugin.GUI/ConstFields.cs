using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI
{
    public static class ConstFields
    {
        public const string RES_STATICFOLDER_NAME = "res";
        public const string RES_SELECTOR_APPFACEIMG_NAME = "scene.png";
        public const string RES_SELECTOR_STATICFILE_NAME = "intellectData.json";
        public const string RES_SELECTOR_ELEDESC_NAME = "ele_desc.txt";
        public const string RES_SELECTOR_IMAGE_GLOBAL = "image_global.jpg";
        public const string RES_SELECTOR_IMAGE_PART = "image_part.jpg";

        public const string RES_SELECTOR_STATICDATA_ELEMENTDATA = "ElementData";
        public const string RES_SELECTOR_STATICDATA_WINDOWSSELECTOR = "WindowsSelector";
        public const string ZBRAIN_CONFIGASYNC_PROJECT_PATH = "PROJECT_PATH";



        public const string DATACOLECTING_PAGE_TITLE = "title";
        public const string DATACOLECTING_PAGE_URL = "url";
        public const string DATACOLECTING_WINDOW_TITLE = "name";
        public const string DATACOLECTING_SELECTORPROPS_TAB = "tab";
        public const string DATACOLECTING_SELECTORPROPS_ID = "id";
        public const string DATACOLECTING_SELECTORPROPS_INDEX = "index";
        public const string DATACOLECTING_SELECTORPROPS_INDEX2 = "@index";
        public const string DATACOLECTING_SELECTORPROPS_IFRAME = "iframe";
        public const string DATACOLECTING_SELECTORPROPS_FRAME = "frame";
        public const string DATACOLECTING_SELECTORPROPS_FRAMESET = "frameset";

        public const string DATACOLECTING_EXTENED_FEILDTYPE_COL = "fieldType";
        public const string DATACOLECTING_EXTENED_FEILDTYPE_TABLE = "talbleFeildTypeList";
        public const string IIRPA_AIREPAIR_DATA_FLODERNAME = "IIRPA-AIREPAIR";
        public const string PROJECT_INFO_FILE_NAME = "szrpa.json";





        public static bool EqualIgnoreCase(this string value1, string value2)
        {
            return value1.Equals(value2, StringComparison.OrdinalIgnoreCase);
        }

    }
    internal enum CaptureImgSourceType
    {
        app,
        desktop
    }
    public class ZBrainMessage
    {
        public string message { get; set; }
        public object result { get; set; }
        public string status { get; set; }
        public string text { get; set; } = string.Empty;
    }
}
