using System;

namespace II.RPA.Plugin.Core.Services
{
    public class AppManager
    {
        private static readonly Lazy<AppManager> _instance = new Lazy<AppManager>(() => new AppManager());
        private AppManager() { }
        public static AppManager Default => _instance.Value;
        public IRpaApplication App { get; private set; }

        public void Initialize(IRpaApplication app)
        {
            if (App != null)
                throw new InvalidOperationException("AppManager is already initialized.");
            App = app ?? throw new ArgumentNullException(nameof(app));
        }
    }
}
