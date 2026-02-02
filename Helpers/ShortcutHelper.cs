using IWshRuntimeLibrary;
using Serilog;
using System.IO;
using File = System.IO.File;

namespace QuickShare.Helpers
{
    public static class ShortcutHelper
    {
        public static bool EnsureQuickShareShortcutInSendTo(string quickShareExePath)
        {
            try
            {
                string sendToPath = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
                if (string.IsNullOrEmpty(sendToPath))
                {
                    Log.Error("Unable to obtain the path of the \"SendTo\" directory");
                    return false;
                }

                string shortcutPath = Path.Combine(sendToPath, "Quick Share.lnk");

                if (File.Exists(shortcutPath))
                {
                    return true;
                }

                if (!File.Exists(quickShareExePath))
                {
                    Log.Error($"The QuickShare file does not exist: {quickShareExePath}");
                    return false;
                }

                return CreateShortcut(shortcutPath, quickShareExePath);
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred while creating the shortcut: {ex.Message}");
                return false;
            }
        }

        private static bool CreateShortcut(string shortcutPath, string targetPath)
        {
            try
            {
                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.Description = "快速分享文件";
                shortcut.Save();

                Log.Information($"Shortcut created: {shortcutPath}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create shortcut: {ex.Message}");
                return false;
            }
        }
    }
}
