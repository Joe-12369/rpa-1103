using FlaUI.Core.Input;

namespace II.RPA.Plugin.Core
{
    public class KeyboardSimulate
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
