using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace II.RPA.Plugin.DriverInput.WinRing0
{
    public class ControlKey
    {
        private static ILogger logger = Logger.OfContext();

        /// <summary>
        /// 默认最小30ms的输入时间间隔
        /// </summary>
        private int inputInterval = 30;
        public int InputInterval
        {
            get { return inputInterval; }
            set
            {
                inputInterval = value < 30 ? 30 : value;
            }
        }

        Dictionary<char, uint> keyCodeDic;
        List<char> specialList;
        public ControlKey()
        {
            keyCodeDic = new Dictionary<char, uint>();
            specialList = new List<char>() { '~', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '_', '+', '{', '}', '|', '"', ':', '<', '>', '?' };
            InitKeys();
        }
        void InitKeys()
        {
            keyCodeDic.Add('A', 65);
            keyCodeDic.Add('B', 66);
            keyCodeDic.Add('C', 67);
            keyCodeDic.Add('D', 68);
            keyCodeDic.Add('E', 69);
            keyCodeDic.Add('F', 70);
            keyCodeDic.Add('G', 71);
            keyCodeDic.Add('H', 72);
            keyCodeDic.Add('I', 73);
            keyCodeDic.Add('J', 74);
            keyCodeDic.Add('K', 75);
            keyCodeDic.Add('L', 76);
            keyCodeDic.Add('M', 77);
            keyCodeDic.Add('N', 78);
            keyCodeDic.Add('O', 79);
            keyCodeDic.Add('P', 80);
            keyCodeDic.Add('Q', 81);
            keyCodeDic.Add('R', 82);
            keyCodeDic.Add('S', 83);
            keyCodeDic.Add('T', 84);
            keyCodeDic.Add('U', 85);
            keyCodeDic.Add('V', 86);
            keyCodeDic.Add('W', 87);
            keyCodeDic.Add('X', 88);
            keyCodeDic.Add('Y', 89);
            keyCodeDic.Add('Z', 90);

            keyCodeDic.Add('0', 48);
            keyCodeDic.Add('1', 49);
            keyCodeDic.Add('2', 50);
            keyCodeDic.Add('3', 51);
            keyCodeDic.Add('4', 52);
            keyCodeDic.Add('5', 53);
            keyCodeDic.Add('6', 54);
            keyCodeDic.Add('7', 55);
            keyCodeDic.Add('8', 56);
            keyCodeDic.Add('9', 57);

            keyCodeDic.Add('*', 106);
            keyCodeDic.Add('=', 187);
            keyCodeDic.Add('-', 189);
            keyCodeDic.Add('.', 110);
            keyCodeDic.Add(';', 186);

            keyCodeDic.Add('`', 192);
            keyCodeDic.Add('~', 192);
            keyCodeDic.Add('@', 50);
            keyCodeDic.Add('!', 49);
            keyCodeDic.Add('#', 51);
            keyCodeDic.Add('$', 52);
            keyCodeDic.Add('%', 53);
            keyCodeDic.Add('^', 54);
            keyCodeDic.Add('&', 55);
            keyCodeDic.Add('(', 57);
            keyCodeDic.Add(')', 48);
            keyCodeDic.Add('_', 189);
            keyCodeDic.Add('+', 187);
            keyCodeDic.Add('[', 219);
            keyCodeDic.Add(']', 221);
            keyCodeDic.Add('\\', 220);
            keyCodeDic.Add(':', 186);
        }

        /// <summary>
        /// winring0操作对象
        /// </summary>
        PressKeyByWinRing0 mPressKey = null;

        /// <summary>
        /// 初始化运行环境
        /// </summary>
        /// <param name="CInputInterval">输入间隔</param>
        /// <param name="CKeyDownDuration">按键按压时间</param>
        /// <param name="CKeyupDuration">按键抬起后停顿时间</param>
        /// <returns></returns>
        public bool InitEnvironment(int CInputInterval = 30, int CKeyDownDuration = 100, int CKeyupDuration = 10)
        {
            InputInterval = CInputInterval;
            mPressKey = new PressKeyByWinRing0(CKeyDownDuration, CKeyupDuration);
            bool bInitResult = mPressKey.Initialize(EnumWindowsType.Win32);
            if (bInitResult == false)
            {
                DeinitEnvironment();
                Console.WriteLine("组件初始化失败");
                logger.LogError($"异常:组件初始化失败");
                return false;
            }
            return true;
        }

        public void DeinitEnvironment()
        {
            try
            {
                if (mPressKey != null)
                {
                    mPressKey.DeinitializeOls();
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex,"释放资源异常");
            }
        }

        public bool InputText(string _Content)
        {
            try
            {
                logger.LogDebug($"当前输入内容: {_Content}");
               
                if (_Content == "{enter}" || _Content == "{ENTER}")
                {
                    mPressKey.KeyDown(Keys.Enter);
                    mPressKey.KeyUp(Keys.Enter);
                    return true;
                }

                foreach (var chr in _Content)
                {
                    if (keyCodeDic.ContainsKey(chr))
                    {
                        if (specialList.Contains(chr))
                        {
                            mPressKey.KeyDown(Keys.ShiftKey);
                            mPressKey.KeyPress(keyCodeDic[chr]);
                            mPressKey.KeyUp(Keys.ShiftKey);
                        }
                        else
                        {
                            if (chr >= 'A' && chr <= 'Z')
                            {
                                mPressKey.KeyDown(Keys.ShiftKey);
                                mPressKey.KeyPress(keyCodeDic[chr]);
                                mPressKey.KeyUp(Keys.ShiftKey);
                            }
                            else
                            {
                                mPressKey.KeyPress(keyCodeDic[chr]);
                            }
                        }
                    }
                    else if (keyCodeDic.ContainsKey(chr.ToString().ToUpper().ToCharArray()[0]))
                    {
                        char matchKey = chr.ToString().ToUpper().ToCharArray()[0];
                        mPressKey.KeyPress(matchKey);
                    }

                    Thread.Sleep(InputInterval);
                }
               
                return true;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e,"输入异常");
                return false;
            }
        }

     
    }
}

