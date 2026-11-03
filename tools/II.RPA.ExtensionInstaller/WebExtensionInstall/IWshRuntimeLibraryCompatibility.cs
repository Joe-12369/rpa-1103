using System;

namespace IWshRuntimeLibrary
{
    public interface IWshShortcut
    {
        string TargetPath { get; }
    }

    public sealed class WshShell
    {
        private readonly dynamic shell;

        public WshShell()
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            shell = shellType == null ? null : Activator.CreateInstance(shellType);
        }

        public IWshShortcut CreateShortcut(string path)
        {
            return new WshShortcut(shell?.CreateShortcut(path));
        }

        private sealed class WshShortcut : IWshShortcut
        {
            private readonly dynamic shortcut;

            public WshShortcut(dynamic shortcut)
            {
                this.shortcut = shortcut;
            }

            public string TargetPath => shortcut?.TargetPath;
        }
    }
}
