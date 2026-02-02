using QuickShare.Services;
using System.IO;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;
using Wpf.Ui.Controls;

namespace QuickShare.Views.Windows
{
    public partial class UpdateWindow : FluentWindow
    {
        public UpdateWindow(
            string version,
            string githubDownloadLink,
            string sourceForgeDownloadLink,
            string UpdateLog,
            TranslationService translationService)
        {
            Owner = App.Current.MainWindow;

            InitializeComponent();

            VersionTextBlock.Text = $"{translationService.Translate("UpdateWindow_Text_NewVersion")} {version}";
            if (string.IsNullOrWhiteSpace(githubDownloadLink))
            {
                GithubDownloadButton.Visibility = Visibility.Collapsed;
            }
            if (string.IsNullOrWhiteSpace(sourceForgeDownloadLink))
            {
                SourceForgeDownloadButton.Visibility = Visibility.Collapsed;
            }
            GithubDownloadButton.NavigateUri = githubDownloadLink;
            SourceForgeDownloadButton.NavigateUri = sourceForgeDownloadLink;
            FlowDocument? updateLogDocument = XamlToFlowDocumentConverter(UpdateLog);
            if (updateLogDocument != null)
            {
                UpdateLogViewer.Document = updateLogDocument;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private FlowDocument? XamlToFlowDocumentConverter(string xamlString)
        {
            try
            {
                if (string.IsNullOrEmpty(xamlString))
                    return null;

                using (StringReader stringReader = new StringReader(xamlString))
                using (XmlReader xmlReader = XmlReader.Create(stringReader))
                {
                    return (FlowDocument)XamlReader.Load(xmlReader);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
