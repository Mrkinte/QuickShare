using Microsoft.Win32;
using System.Diagnostics;

namespace QuickShare.Helpers
{
    public class StartupHelper
    {
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "Quick Share";

        public static bool SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
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
                        key.SetValue(AppName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
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
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
            {
                if (key == null) return false;
                return key.GetValue(AppName) != null;
            }
        }
    }
}
