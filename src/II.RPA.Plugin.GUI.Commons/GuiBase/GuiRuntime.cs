using System;

namespace II.RPA.Plugin.GUI.Commons
{
    public static class GuiRuntime
    {
        private static GuiRuntimeServices services;

        public static GuiRuntimeServices Services => services
            ?? throw new InvalidOperationException("GUI Runtime 尚未初始化。");

        public static IGuiWindowsOperator WindowsOperator => Services.WindowsOperator;

        public static void Initialize(GuiRuntimeServices runtimeServices)
        {
            if (runtimeServices == null) throw new ArgumentNullException(nameof(runtimeServices));
            if (services != null && !ReferenceEquals(services, runtimeServices))
            {
                throw new InvalidOperationException("GUI Runtime 已使用另一实例初始化。");
            }

            services = runtimeServices;
        }
    }
}
