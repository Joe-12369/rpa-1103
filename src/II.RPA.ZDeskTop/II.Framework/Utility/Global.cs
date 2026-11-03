namespace II.Framework.Utility
{
    public static class Global
    {
        public const string Factory = "Z-Factory";
        public const string Bot = "Z-Bot";
        public const string AppName = "Zdesktop";
        public const string OutPutKey = "OutPutKey";
        public const string ErrorPutKey = "ErrorPutKey"; 
        public static string Client
        {
            get;
            set;
        }
        public static bool IsSuperAdmin
        {
            get;
            set;
        }
        public static bool IsAgent
        {
            get;
            set;
        }
        public static bool IsMiniOn
        {
            get;
            set;
        }
        public static bool IsNormalWindowMode
        {
            get;
            set;
        } = false;
    }
}
