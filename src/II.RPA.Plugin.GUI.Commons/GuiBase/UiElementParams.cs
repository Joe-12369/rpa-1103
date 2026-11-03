using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.WindowsAPI;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    /// <summary>
    ///元素节点类型
    /// </summary>
    [Flags]
    public enum SelectorPropsType
    {
        None = 0,
        UIA2 = 1 << 0,
        UIA3 = 1 << 1,
        RawElement = 1 << 2,
        ControlElement = 1 << 3,
        ContentElement = 1 << 4
    }
    public enum ClickType
    {
        Single,
        Double,
        Down,
        Up,
    }
    //public enum MouseButton
    //{
    //    Left,

    //    Middle,

    //    Right,

    //    //扩展Button
    //    XButton1,

    //    XButton2
    //}
    public enum VirtualKey : ushort
    {
        //

        //     Left mouse button
        LBUTTON = 1,
        //

        //     Right mouse button
        RBUTTON = 2,
        //

        //     Control-break processing
        CANCEL = 3,
        //

        //     Middle mouse button (three-button mouse)
        MBUTTON = 4,
        //

        //     Windows 2000/XP: X1 mouse button
        XBUTTON1 = 5,
        //

        //     Windows 2000/XP: X2 mouse button
        XBUTTON2 = 6,
        //

        //     BACKSPACE key
        BACK = 8,
        //

        //     TAB key
        TAB = 9,
        //

        //     CLEAR key
        CLEAR = 12,
        //

        //     ENTER key
        ENTER = 13,
        RERETURN = 13,
        //

        //     SHIFT key
        SHIFT = 16,
        //

        //     CTRL key
        CONTROL = 17,
        //

        //     ALT key
        ALT = 18,
        //

        //     PAUSE key
        PAUSE = 19,
        //

        //     CAPS LOCK key
        // CAPITAL = 20,
        CAPSLOCK = 20,
        //

        //     Input Method Editor (IME) Kana mode
        KANA = 21,
        //

        //     IME Hangul mode
        HANGUL = 21,
        //

        //     IME Junja mode
        JUNJA = 23,
        //

        //     IME final mode
        FINAL = 24,
        //

        //     IME Hanja mode
        HANJA = 25,
        //

        //     IME Kanji mode
        KANJI = 25,
        //

        //     ESC key
        //ESCAPE = 27,
        ESC = 27,
        //

        //     IME convert
        CONVERT = 28,
        //

        //     IME nonconvert
        NONCONVERT = 29,
        //

        //     IME accept
        ACCEPT = 30,
        //

        //     IME mode change request
        MODECHANGE = 31,
        //

        //     SPACEBAR
        SPACE = 32,
        //

        //     PAGE UP key
        PAGE_UP = 33,
        //

        //     PAGE DOWN key
        PAGE_DOWN = 34,
        //

        //     END key
        END = 35,
        //

        //     HOME key
        HOME = 36,
        //

        //     LEFT ARROW key
        LEFT = 37,
        //

        //     UP ARROW key
        UP = 38,
        //

        //     RIGHT ARROW key
        RIGHT = 39,
        //

        //     DOWN ARROW key
        DOWN = 40,
        //

        //     SELECT key
        SELECT = 41,
        //

        //     PRINT key
        PRINT = 42,
        //

        //     EXECUTE key
        EXECUTE = 43,
        //

        //     PRINT SCREEN key
        SNAPSHOT = 44,
        //

        //     INS key
        INSERT = 45,
        //

        //     DEL key
        DELETE = 46,
        //

        //     HELP key
        HELP = 47,
        //

        //     0 key
        KEY_0 = 48,
        //

        //     1 key
        KEY_1 = 49,
        //

        //     2 key
        KEY_2 = 50,
        //

        //     3 key
        KEY_3 = 51,
        //

        //     4 key
        KEY_4 = 52,
        //

        //     5 key
        KEY_5 = 53,
        //

        //     6 key
        KEY_6 = 54,
        //

        //     7 key
        KEY_7 = 55,
        //

        //     8 key
        KEY_8 = 56,
        //

        //     9 key
        KEY_9 = 57,
        //

        //     A key
        KEY_A = 65,
        //

        //     B key
        KEY_B = 66,
        //

        //     C key
        KEY_C = 67,
        //

        //     D key
        KEY_D = 68,
        //

        //     E key
        KEY_E = 69,
        //

        //     F key
        KEY_F = 70,
        //

        //     G key
        KEY_G = 71,
        //

        //     H key
        KEY_H = 72,
        //

        //     I key
        KEY_I = 73,
        //

        //     J key
        KEY_J = 74,
        //

        //     K key
        KEY_K = 75,
        //

        //     L key
        KEY_L = 76,
        //

        //     M key
        KEY_M = 77,
        //

        //     N key
        KEY_N = 78,
        //

        //     O key
        KEY_O = 79,
        //

        //     P key
        KEY_P = 80,
        //

        //     Q key
        KEY_Q = 81,
        //

        //     R key
        KEY_R = 82,
        //

        //     S key
        KEY_S = 83,
        //

        //     T key
        KEY_T = 84,
        //

        //     U key
        KEY_U = 85,
        //

        //     V key
        KEY_V = 86,
        //

        //     W key
        KEY_W = 87,
        //

        //     X key
        KEY_X = 88,
        //

        //     Y key
        KEY_Y = 89,
        //

        //     Z key
        KEY_Z = 90,
        //

        //     Left Windows key (Microsoft Natural keyboard)
        LWIN = 91,
        //

        //     Right Windows key (Natural keyboard)
        RWIN = 92,
        //

        //     Applications key (Natural keyboard)
        APPS = 93,
        //

        //     Computer Sleep key
        SLEEP = 95,
        //

        //     Numeric keypad 0 key
        NUMPAD0 = 96,
        //

        //     Numeric keypad 1 key
        NUMPAD1 = 97,
        //

        //     Numeric keypad 2 key
        NUMPAD2 = 98,
        //

        //     Numeric keypad 3 key
        NUMPAD3 = 99,
        //

        //     Numeric keypad 4 key
        NUMPAD4 = 100,
        //

        //     Numeric keypad 5 key
        NUMPAD5 = 101,
        //

        //     Numeric keypad 6 key
        NUMPAD6 = 102,
        //

        //     Numeric keypad 7 key
        NUMPAD7 = 103,
        //

        //     Numeric keypad 8 key
        NUMPAD8 = 104,
        //

        //     Numeric keypad 9 key
        NUMPAD9 = 105,
        //

        //     Multiply key
        MULTIPLY = 106,
        //

        //     Add key
        ADD = 107,
        //

        //     Separator key
        SEPARATOR = 108,
        //

        //     Subtract key
        SUBTRACT = 109,
        //

        //     Decimal key
        DECIMAL = 110,
        //

        //     Divide key
        DIVIDE = 111,
        //

        //     F1 key
        F1 = 112,
        //

        //     F2 key
        F2 = 113,
        //

        //     F3 key
        F3 = 114,
        //

        //     F4 key
        F4 = 115,
        //

        //     F5 key
        F5 = 116,
        //

        //     F6 key
        F6 = 117,
        //

        //     F7 key
        F7 = 118,
        //

        //     F8 key
        F8 = 119,
        //

        //     F9 key
        F9 = 120,
        //

        //     F10 key
        F10 = 121,
        //

        //     F11 key
        F11 = 122,
        //

        //     F12 key
        F12 = 123,
        //

        //     F13 key
        F13 = 124,
        //

        //     F14 key
        F14 = 125,
        //

        //     F15 key
        F15 = 126,
        //

        //     F16 key
        F16 = 127,
        //

        //     F17 key
        F17 = 128,
        //

        //     F18 key
        F18 = 129,
        //

        //     F19 key
        F19 = 130,
        //

        //     F20 key
        F20 = 131,
        //

        //     F21 key
        F21 = 132,
        //

        //     F22 key, (PPC only) Key used to lock device.
        F22 = 133,
        //

        //     F23 key
        F23 = 134,
        //

        //     F24 key
        F24 = 135,
        //

        //     NUM LOCK key
        NUMLOCK = 144,
        //

        //     SCROLL LOCK key
        SCROLL = 145,
        //

        //     Left SHIFT key
        LSHIFT = 160,
        //

        //     Right SHIFT key
        RSHIFT = 161,
        //

        //     Left CONTROL key
        LCONTROL = 162,
        //

        //     Right CONTROL key
        RCONTROL = 163,
        //

        //     Left MENU key
        LMENU = 164,
        //

        //     Right MENU key
        RMENU = 165,
        //

        //     Windows 2000/XP: Browser Back key
        BROWSER_BACK = 166,
        //

        //     Windows 2000/XP: Browser Forward key
        BROWSER_FORWARD = 167,
        //

        //     Windows 2000/XP: Browser Refresh key
        BROWSER_REFRESH = 168,
        //

        //     Windows 2000/XP: Browser Stop key
        BROWSER_STOP = 169,
        //

        //     Windows 2000/XP: Browser Search key
        BROWSER_SEARCH = 170,
        //

        //     Windows 2000/XP: Browser Favorites key
        BROWSER_FAVORITES = 171,
        //

        //     Windows 2000/XP: Browser Start and Home key
        BROWSER_HOME = 172,
        //

        //     Windows 2000/XP: Volume Mute key
        VOLUME_MUTE = 173,
        //

        //     Windows 2000/XP: Volume Down key
        VOLUME_DOWN = 174,
        //

        //     Windows 2000/XP: Volume Up key
        VOLUME_UP = 175,
        //

        //     Windows 2000/XP: Next Track key
        MEDIA_NEXT_TRACK = 176,
        //

        //     Windows 2000/XP: Previous Track key
        MEDIA_PREV_TRACK = 177,
        //

        //     Windows 2000/XP: Stop Media key
        MEDIA_STOP = 178,
        //

        //     Windows 2000/XP: Play/Pause Media key
        MEDIA_PLAY_PAUSE = 179,
        //

        //     Windows 2000/XP: Start Mail key
        LAUNCH_MAIL = 180,
        //

        //     Windows 2000/XP: Select Media key
        LAUNCH_MEDIA_SELECT = 181,
        //

        //     Windows 2000/XP: Start Application 1 key
        LAUNCH_APP1 = 182,
        //

        //     Windows 2000/XP: Start Application 2 key
        LAUNCH_APP2 = 183,
        //

        //     Used for miscellaneous characters; it can vary by keyboard.
        OEM_1 = 186,
        //

        //     Windows 2000/XP: For any country/region, the '+' key
        OEM_PLUS = 187,
        //

        //     Windows 2000/XP: For any country/region, the ',' key
        OEM_COMMA = 188,
        //

        //     Windows 2000/XP: For any country/region, the '-' key
        OEM_MINUS = 189,
        //

        //     Windows 2000/XP: For any country/region, the '.' key
        OEM_PERIOD = 190,
        //

        //     Used for miscellaneous characters; it can vary by keyboard.
        OEM_2 = 191,
        //

        //     Used for miscellaneous characters; it can vary by keyboard.
        OEM_3 = 192,
        //

        //     Used for miscellaneous characters; it can vary by keyboard.
        OEM_4 = 219,
        //

        //     Used for miscellaneous characters; it can vary by keyboard.
        OEM_5 = 220,
        //

        //     Used for miscellaneous characters; it can vary by keyboard.
        OEM_6 = 221,
        //

        //     Used for miscellaneous characters; it can vary by keyboard.
        OEM_7 = 222,
        //

        //     Used for miscellaneous characters; it can vary by keyboard.
        OEM_8 = 223,
        //

        //     Windows 2000/XP: Either the angle bracket key or the backslash key on the RT
        //     102-key keyboard
        OEM_102 = 226,
        //

        //     Windows 95/98/Me, Windows NT 4.0, Windows 2000/XP: IME PROCESS key
        PROCESSKEY = 229,
        //

        //     Windows 2000/XP: Used to pass Unicode characters as if they were keystrokes.
        //     The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard
        //     input methods. For more information, see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN,
        //     and WM_KEYUP
        PACKET = 231,

        //     Attn key
        ATTN = 246,

        //     CrSel key
        CRSEL = 247,

        //     ExSel key
        EXSEL = 248,

        //     Erase EOF key
        EREOF = 249,

        //     Play key
        PLAY = 250,

        //     Zoom key
        ZOOM = 251,

        //     Reserved
        NONAME = 252,

        //     PA1 key
        PA1 = 253,

        //     Clear key
        OEM_CLEAR = 254
    }

    public enum RPAMouseButtons
    {
        Left,
        Middle,
        Right
    }
    public enum RPAMousePresses
    {
        Click,
        DoubleClick,
        ThreeClick,
        Up,
        Down,
    }
    public class UiElementClickParams
    {
        public RPAMouseButtons RPAMouseButtons { get; set; }
        //初始赋值
        public RPAMousePresses RPAMousePresses = RPAMousePresses.Click;
        /// <summary>
        /// 是否在操作过程中移动鼠标
        /// </summary>
        public bool moveMouse = false;//暂时改回原来的写法
        //操作类型；0：后台操作，1：模拟操作；
        public int operateType;
        /*- label: "中心"
        value: '0'
      - label: "左上"
        value: '1'
      - label: "右上"
        value: '2'
      - label: "左下"
        value: '3'
      - label: "右下"
        value: '4'*/
        public int cursorPos;
        /// <summary>
        /// 是否使用模拟
        /// </summary>
        public bool isUsePerson { get; set; }
        public UiElementClickParams()
        {

        }

    }
    public class UiElementHoverParams
    {
        /// <summary>
        /// 是否在操作过程中移动鼠标
        /// </summary>
        public bool moveMouse = false;
        public int offsetX;
        public int offsetY;
        public int cursorPos;

        /// <summary>
        /// 是否使用模拟
        /// </summary>
        public bool isUsePerson { get; set; }
        public UiElementHoverParams()
        {

        }
    }

    public class GetElementFromSelectorParams
    {
        public string Selector { get; set; }
        public string SelectorType { get; set; }
        public bool FindOnce { get; set; } = false;
        public string CVExecutionServiceURL { get; set; } = string.Empty;
        public object ExtendParam { get; set; } = null;
        public bool isMove { get; set; } = true;
        public bool isHighlighting { get; set; } = false;
        public bool isFromOCRTable { get; set; } = false;
        public ElementMatchType MatchType { get; set; }
        public RunParamModel RunParam { get; set; }
        public string webData { get; set; }
        public T GetExtendParamValue<T>(string paramName)
        {
            if (ExtendParam == null)
            {
                return default(T);
            }
            if (!(ExtendParam is Dictionary<string, object>))
            {
                throw new NotSupportedException("此方法只使用于ExtendParam数据类型为Dictionary<string,object>的情况".I18n("controlType_UiElementParams_47068B41"));
            }
            var dicParam = (Dictionary<string, object>)ExtendParam;
            if (dicParam.ContainsKey(paramName))
            {
                return (T)dicParam[paramName];
            }
            else
            {
                return default(T);
            }
        }
        /// <summary>
        /// /获取扩展参数对象中的值，但只支持ExtendParam 类型为：Dictionary<string, object>、JObject两种数据类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paramName"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public T GetExtendParamValue<T>(string paramName, T defaultVal)
        {
            if (ExtendParam == null)
            {
                return defaultVal;
            }
            if (ExtendParam is Dictionary<string, object>)
            {
                if (((Dictionary<string, object>)ExtendParam).TryGetValue(paramName, out var pVal))
                {
                    return (T)pVal;
                }
                return defaultVal;
            }
            else if (ExtendParam is JObject)
            {
                if (((JObject)ExtendParam).TryGetValue(paramName, StringComparison.OrdinalIgnoreCase, out var pVal))
                {
                    return pVal.ToString().ToObject<T>();
                }

                return defaultVal;
            }
            else
            {
                NLogger.Default.Error($"扩展参数类型传递异常，请检查程序");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
    public class GetElementsAllParams
    {
        public int TabId { get; set; }
        public string TagNames { get; set; }
        public string AttrKeywords { get; set; }
        /// <summary>
        /// 需要获取元素的窗口信息
        /// </summary>
        public IntPtr WindowHandle { get; set; }
        public string ClsNames { get; set; }
        public string PrevCVSelector = string.Empty;
        public bool CVUseLastData = false;
        public string SpecialOperation { get; set; } = string.Empty;
        public string ViewElementsData { get; set; } = "{}";
        public string SourceType { get; set; }

        public object TaskInfo { get; set; }

        public bool IsFirstRecognition { get; set; }
        public string ActionName { get; set; }

    }

    public class GetElementByIdParams
    {
        public int TabId { get; set; } = -1;
        public string ElementId { get; set; }
    }
    public class ViewElementsDataModel
    {
        public string uniqueId { get; set; }
        public string tagName { get; set; }
        //public List<string> path { get; set; }
        public JObject specific { get; set; }
        public RpaRect bounding { get; set; } = null;
        public List<ViewElementsDataModel> children { get; set; } = new List<ViewElementsDataModel>();
    }
}
