using System.Threading;
using System.Windows.Forms;

namespace DriverInput
{
    public class PressKeyByWinRing0
    {
        private int IntervalKeyDown = 100;
        private int IntervalKeyUp = 10;

        public bool Initialize(EnumWindowsType winType)
        {
            return WinRing0.init();
        }
        #region 输入char
        public void KeyDown(char key)
        {
            WinRing0.KeyDown(key);//按下
            Thread.Sleep(IntervalKeyDown);
        }
        public void KeyUp(char key)
        {
            WinRing0.KeyUp(key);//松开
            Thread.Sleep(IntervalKeyUp);
        }
        public void KeyPress(char key)
        {
            KeyDown(key);//按下
            KeyUp(key);//松开
        }
        #endregion


        #region 输入Uint
        public void KeyDown(uint key)
        {
            WinRing0.KeyDown(key);//按下
            Thread.Sleep(IntervalKeyDown);
        }
        public void KeyUp(uint key)
        {
            WinRing0.KeyUp(key);//松开
            Thread.Sleep(IntervalKeyUp);
        }
        public void KeyPress(uint key)
        {
            KeyDown(key);//按下
            KeyUp(key);//松开
        }
        #endregion

        #region 输入Keys
        public void KeyDown(Keys vKeyCoad)
        {
            WinRing0.KeyDown(vKeyCoad);
            Thread.Sleep(IntervalKeyDown);
        }

        public void KeyUp(Keys vKeyCoad)
        {
            WinRing0.KeyUp(vKeyCoad);
            Thread.Sleep(IntervalKeyUp);
        }

        public void KeyPress(Keys vKeyCoad)
        {
            KeyDown(vKeyCoad);
            KeyUp(vKeyCoad);
        }
        #endregion

        public void DeinitializeOls() {
            WinRing0.DeinitializeOls();
        }
    }
}
