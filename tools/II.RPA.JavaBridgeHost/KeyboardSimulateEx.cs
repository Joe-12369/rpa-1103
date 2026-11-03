using FlaUI.Core.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.JavaBridgeHost
{
    public class KeyboardSimulateEx
    {
        private static readonly object _locker = new object();
        public static void KeyboardSimulateInput(string mouseInput)
        {
            lock (_locker)
            {
                Keyboard.SendInput(mouseInput);
            }
        }
    }
}
