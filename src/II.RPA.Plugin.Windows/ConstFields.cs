using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Windows
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

        public const string PATTERN_MORETHAN = "morethan";
        public const string PATTERN_MORETHANOREQUAL = "morethanorequal";
        public const string PATTERN_LESSTHAN = "lessthan";
        public const string PATTERN_LESSTHANOREQUAL = "lessthanorequal";
        public const string PATTERN_EXCLUDE = "exclude";
        public const string PATTERN_NOTCONTAIN = "notcontain";
        public const string PATTERN_NOTBEGIN = "notbegin";
        public const string PATTERN_NOTEND = "notend";
        //----------------------------------------------------
        public const string SELECTOR_CONTROL_TYPE = "controltype";
        public const string SELECTOR_NAME = "name";
        public const string SELECTOR_CLASS_NAME = "class";
        public const string SELECTOR_AUTOMATION_ID = "automationid";
        public const string SELECTOR_FRAMEWORK_ID = "frameworkid";
        public const string SELECTOR_INDEX_IN_PARENT = "indexinparent";
        public const string SELECTOR_INDEX_IN_PARENT_NEW = "@index";
        public const string SELECTOR_INDEX = "index";
        public const string SELECTOR_BOUNDING_RECT = "boundingrect";
        public const string SELECTOR_ID = "identity";
        public const string SELECTOR_Custom = "Custom";
        public const string SELECTOR_NAME_DOCUMENT = "Document";
        public const string SELECTOR_TITLE = "title";

        public const string SELECTOR_NAME_WXWORK_VERTICALLAYOUTUI = "VerticalLayoutUI";

        public const string SELECTOR_NAME_WXWORK_ROOTMENU = "rootmenu";

        public const string SELECTOR_NAME_WXWORK_CHATLIST = "chatlist";
        public const string SELECTOR_NAME_WXWORK_CHATITEM = "ChatItem";

        public const string SELECTOR_NAME_WXWORK_EXTERNALS = "externals";
        public const string SELECTOR_NAME_WXWORK_ITEM = "item";

        public const string SELECTOR_NAME_WXWORK_COLLECTIONLIST = "collectionlist";
        public const string SELECTOR_NAME_WXWORK_LISTCONTAINERELEMENTUI = "ListContainerElementUI";

        public const string SELECTOR_NAME_WXWORK_SEARCH_MESSAGE_CHATLIST = "search_message_chatlist";

        public const string SELECTOR_ROOT_FILE = "filename";
        public const string SELECTOR_ROOT_SELECTOR = "selector";
        public const string SELECTOR_ROOT_SELECTOR_VALUE = "windows";
        public const string SELECTOR_ROOT_PROCESSNAME = "processname";
        public const string SELECTOR_ROOT_TOP_NODESELECTOR = "{\"name\":\"Pane\",\"type\":\"6\",\"accurate\":\"true\",\"props\":[{\"name\":\"class\",\"value\":\"Progman|WorkerW\",\"pattern\":\"equal\",\"accurate\":\"true\",\"textValue\":\"Progman\",\"expressionValue\":\"\\\"Progman\\\"\"}]}";

        public const string SELECTOR_ROOT_TOP_SHELLDLL_DefView = "{\"name\":\"Pane\",\"type\":\"6\",\"accurate\":\"true\",\"props\":[{\"name\":\"class\",\"value\":\"SHELLDLL_DefView\",\"pattern\":\"equal\",\"accurate\":\"true\",\"textValue\":\"SHELLDLL_DefView\",\"expressionValue\":\"\\\"SHELLDLL_DefView\\\"\"}]}";

        public const string PROP_ACCURATE_TURE = "true";
        public const string PROP_ACCURATE_FALSE = "false";

        public const string PROP_CLASS_NAME_WECHAT = "WeChatMainWndForPC";
        public const string PROP_NAME_WECHAT_CH = "微信";
        public const string PROP_NAME_PROCESS_NAME = "processname";
        public const string PROP_NAME_FLAG = "Flag";
        public const string PROP_NAME_SIMILARKEY = "similarKey";
        public const string PROP_NAME_SIMILARINDEX = "similarIndex";
        public const string PROP_NAME_SUBKEY = "subKey";
        public const string PROP_NAME_SUBINDEX = "subIndex";
        public const string PROP_CLASS_NAME_SYSLISTVIEW32 = "SysListView32";
        public const string PROP_CLASS_NAME_SHELLDLL_DEFVIEW = "SHELLDLL_DefView";

        public const string PROP_VALUE_PROCESS_WECHAT_NAME = "WeChat";
        public const string PROP_VALUE_NO_FOREGROUND = "NoForeground";
        public const string PROP_VALUE_CONTROL_TYPE_MENU = "Menu";
        public const string PROP_VALUE_CONTROL_TYPE_WINDOW = "Window";
        public const string PROP_VALUE_CONTROL_TYPE_UNKNOW = "Unknown";

        public const string SELECTOR_ELEMENT_NAME_TAG = "name";
        public const string SELECTOR_ELEMENT_ACCURATE_TAG = "accurate";
        public const string SELECTOR_ELEMENT_TYPE_TAG = "type";
        public const string SELECTOR_ELEMENT_PROPS_TAG = "props";
        public const string SELECTOR_ELEMENT_ROOT_TAG = "sysroot";
        public const string SELECTOR_ELEMENT_SELECTOR_TAG = "selector";

        public const string COMPARE_SYMBOL_STAR = "*";
        public const string SELECTOR_OLD_FORMAT_VERSION = "0.0";
        public const string SELECTOR_NEW_FORMAT_VERSION_1 = "1.0";


        public const string PROP_CLASS_NAME_Factory = "Chrome_WidgetWin_1";
        public const string PROP_NAME_Factory = ",运行监视,实在设计器,";

        public const string SELCETOR_IGNORE_CONTROLTYPE = ",window,pane,";

        public static readonly string[] TOP_ELEMENT_TAGS = new string[] { "WorkerW", "Progman" };


        public static bool EqualIgnoreCase(this string value1, string value2)
        {
            return value1.Equals(value2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
