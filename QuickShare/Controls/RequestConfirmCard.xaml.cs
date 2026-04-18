using Wpf.Ui.Controls;

namespace QuickShare.Controls
{
    /// <summary>
    /// RequestConfirmCard.xaml 的交互逻辑
    /// </summary>
    public partial class RequestConfirmCard : CardControl
    {
        public event EventHandler Agreed = delegate { };

        public event EventHandler Refused = delegate { };

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(RequestConfirmCard),
                new PropertyMetadata("Title"));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(RequestConfirmCard),
                new PropertyMetadata("Description"));

        public static readonly DependencyProperty AgreeButtonTextProperty =
            DependencyProperty.Register(nameof(AgreeButtonText), typeof(string), typeof(RequestConfirmCard),
                new PropertyMetadata("AgreeButtonText"));

        public static readonly DependencyProperty RefuseButtonTextProperty =
            DependencyProperty.Register(nameof(RefuseButtonText), typeof(string), typeof(RequestConfirmCard),
                new PropertyMetadata("RefuseButtonText"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public string AgreeButtonText
        {
            get => (string)GetValue(AgreeButtonTextProperty);
            set => SetValue(AgreeButtonTextProperty, value);
        }

        public string RefuseButtonText
        {
            get => (string)GetValue(RefuseButtonTextProperty);
            set => SetValue(RefuseButtonTextProperty, value);
        }

        public RequestConfirmCard()
        {
            InitializeComponent();
        }

        private void Agree_Button_Click(object sender, RoutedEventArgs e)
        {
            Agreed?.Invoke(this, e);
        }

        private void Refuse_Button_Click(object sender, RoutedEventArgs e)
        {
            Refused?.Invoke(this, e);
        }
    }
}
