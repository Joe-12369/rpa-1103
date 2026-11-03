using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.CV
{
    public static class ConstFields
    {
        public const string PATTERN_EQUAL = "equal";
        public const string PATTERN_CONTAIN = "contain";
        public const string PATTERN_BEGIN = "begin";
        public const string PATTERN_END = "end";
        public const string PATTERN_REGULAR = "regular";
        public const string PATTERN_BIGGER = "bigger";
        public const string PATTERN_SMALLER = "smaller";
        //----------------------------------------------------
        public const string SELECTOR_CONTROL_TYPE = "controltype";
        public const string SELECTOR_NAME = "name";
        public const string SELECTOR_CLASS_NAME = "class";
        public const string SELECTOR_AUTOMATION_ID = "automationid";
        public const string SELECTOR_FRAMEWORK_ID = "frameworkid";
        public const string SELECTOR_INDEX_IN_PARENT = "indexinparent";
        public const string SELECTOR_INDEX_IN_PARENT_NEW = "@index";
        public const string SELECTOR_INDEX = "index";
        public const string SELECTOR_ID = "identity";
        public const string SELECTOR_ELEMENT_OBJ = "ElementObj";
        public const string SELECTOR_ANCHOR_OBJ = "AnchorObj";
        public const string SELECTOR_CV_ELEMENT = "cvelement";

        public const string SELECTOR_ROOT_FILE = "filename";
        public const string SELECTOR_ROOT_SELECTOR = "selector";
        public const string SELECTOR_ROOT_SELECTOR_VALUE = "windows";
        public const string SELECTOR_ROOT_PROCESSNAME = "processname"; 

        public const string PROP_ACCURATE_TURE = "true";
        public const string PROP_ACCURATE_FALSE = "false";

        public const string PROP_CLASS_NAME_WECHAT = "WeChatMainWndForPC";
        public const string PROP_NAME_WECHAT_CH = "微信";
        public const string PROP_NAME_PROCESS_NAME = "processname";
        public const string PROP_NAME_FLAG = "Flag";
        public const string PROP_NAME_RESDIRPATH = "resDirPath";
        public const string PROP_NAME_ISSMARTCONTROL = "isSmartControl";
        
        public const string PROP_VALUE_PROCESS_WECHAT_NAME = "WeChat";
        public const string PROP_VALUE_NO_FOREGROUND = "NoForeground";
        public const string PROP_VALUE_CONTROL_TYPE_MENU = "Menu";
        public const string PROP_VALUE_CONTROL_TYPE_WINDOW = "Window";
        public const string PROP_VALUE_CONTROL_TYPE_UNKNOW = "Unknown";

        public const string SELECTOR_ELEMENT_NAME_TAG = "name";
        public const string SELECTOR_ELEMENT_TYPE_TAG = "type";
        public const string SELECTOR_ELEMENT_PROPS_TAG = "props";
        public const string SELECTOR_ELEMENT_ROOT_TAG = "sysroot"; 
        public const string SELECTOR_ELEMENT_SELECTOR_TAG = "selector";

        public const string COMPARE_SYMBOL_STAR = "*";

        public const string ALGORITHM_OCR = "ocr_recognition";

        public const string PROP_LOCATION_X = "LocationX";
        public const string PROP_LOCATION_Y = "LocationY";
        public const string PROP_LOCATION_WIDTH = "Width";
        public const string PROP_LOCATION_HEIGHT = "Height";

        public const string PROP_X = "x";
        public const string PROP_Y = "y";
        public const string PROP_WIDTH = "width";
        public const string PROP_HEIGHT = "height";
        public static bool EqualIgnoreCase(this string value1, string value2)
        {
            return value1.Equals(value2, StringComparison.OrdinalIgnoreCase);
        }

    }
}
