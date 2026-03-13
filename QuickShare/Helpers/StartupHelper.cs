using Microsoft.Win32;
using System.Diagnostics;

namespace QuickShare.Helpers
{
    public class StartupHelper
    {
        private const string registryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string appName = "QuickShare";

        public static bool SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKeyPath, true))
                {
                    if (key == null) return false;

                    if (enable)
                    {
                        // Obtain the path of the current executable file.
                        var processModel = Process.GetCurrentProcess().MainModule;
                        if (processModel == null) return false;

                        string exePath = processModel.FileName;
#if DEBUG
                        exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
#endif
                        key.SetValue(appName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(appName, false);
                    }
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        public static bool IsStartupEnabled()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKeyPath, false))
            {
                if (key == null) return false;
                return key.GetValue(appName) != null;
            }
        }
    }
}
