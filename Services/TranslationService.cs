using Serilog;

namespace QuickShare.Services
{
    public class TranslationService(ILogger logger)
    {
        public string Translate(string key)
        {
            return Application.Current.FindResource(key) as string ?? key;
        }

        public void SwitchLanguage(string cultureName)
        {
            var app = Application.Current;
            var dictionaries = app.Resources.MergedDictionaries;

            // Remove the old language resources.
            var oldDict = dictionaries.FirstOrDefault(d =>
                d.Source?.OriginalString.Contains("/Resources/Language/") == true);
            if (oldDict != null)
                dictionaries.Remove(oldDict);

            // Add new language resources.
            var newDictPath = cultureName == "zh-CN"
                ? "pack://application:,,,/QuickShare;component/Resources/Language/Zh-CN.xaml"
                : $"pack://application:,,,/QuickShare;component/Resources/Language/{cultureName}.xaml";

            try
            {
                var newDict = new ResourceDictionary
                {
                    Source = new Uri(newDictPath),
                };
                dictionaries.Add(newDict);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);

                // Return to the default resources.
                dictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/QuickShare;component/Resources/Language/Zh-CN.xaml")
                });
            }
        }
    }
}
