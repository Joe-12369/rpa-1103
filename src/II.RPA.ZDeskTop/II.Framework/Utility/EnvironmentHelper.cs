using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Security.Principal;
using System.Text;

namespace II.Framework.Utility
{
    public static class EnvironmentHelper
    {
        public static int CurrentProcessId
        {
            get;
        }

        public static int CurrentSessionId
        {
            get;
        }

        public static string CurrentUserName
        {
            get;
        }

        public static bool RunningAsLocalSystem
        {
            get
            {
                return CurrentSessionId == 0;
            }
        }
        public static bool IsAdmin
        {
            get;
        }

        static EnvironmentHelper()
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            {
                CurrentProcessId = currentProcess.Id;
                CurrentSessionId = currentProcess.SessionId;
            }
            using WindowsIdentity current = WindowsIdentity.GetCurrent();
            CurrentUserName = current.Name;
            IsAdmin = IsAdminRole();
        }

        public static string SetupJitOptimization(
            string applicationName,
            string profileName = "Startup.profile")
        {
            if (string.IsNullOrWhiteSpace(applicationName))
            {
                throw new ArgumentNullException(nameof(applicationName));
            }

            try
            {
                string profileRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    applicationName,
                    "JitProfile");

                if (!Directory.Exists(profileRoot))
                {
                    Directory.CreateDirectory(profileRoot);
                }

                ProfileOptimization.SetProfileRoot(profileRoot);
                ProfileOptimization.StartProfile(profileName);

                string successMessage = $"Pre-Jitting started and Profile available at: {profileRoot}";
                return successMessage;
            }
            catch (UnauthorizedAccessException ex)
            {
                string errorMessage = BuildErrorMessage("Pre-Jitting Failed due to access denied", ex);
                return errorMessage;
            }
            catch (IOException ex)
            {
                string errorMessage = BuildErrorMessage("Pre-Jitting Failed due to IO error", ex);
                return errorMessage;
            }
            catch (Exception ex)
            {
                string errorMessage = BuildErrorMessage("Pre-Jitting Failed with unexpected error", ex);
                return errorMessage;
            }
        }
        private static string BuildErrorMessage(string prefix, Exception ex)
        {
            var messageBuilder = new StringBuilder(prefix);
            messageBuilder.AppendLine();
            messageBuilder.AppendLine(ex.Message);

            if (ex.InnerException != null)
            {
                messageBuilder.AppendLine(ex.InnerException.Message);
            }

            messageBuilder.AppendLine(ex.StackTrace);
            return messageBuilder.ToString();
        }

        private static bool IsAdminRole()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
