using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickShare.Helpers
{
    public class CustomHelper
    {
        /// <summary>
        /// 获取所有子目录下的文件和文件夹总数。
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public static int GetFileCount(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return 0;

            try
            {
                return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).Count();
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 转换Bitmap到BitmapImage，供给WPF显示。
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }

        public static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            T? parent = parentObject as T;
            if (parent != null)
                return parent;

            return FindVisualParent<T>(parentObject);
        }
    }
}
