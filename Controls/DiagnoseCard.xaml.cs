using System.Windows.Media;
using Wpf.Ui.Controls;

namespace QuickShare.Controls
{
    public partial class DiagnoseCard : CardControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(DiagnoseCard),
                new PropertyMetadata("Title"));

        public static readonly DependencyProperty DiagnoseDetailProperty =
            DependencyProperty.Register(nameof(DiagnoseDetail), typeof(string), typeof(DiagnoseCard),
                new PropertyMetadata("DiagnoseDetail"));

        public static readonly DependencyProperty DiagnoseStatusProperty =
            DependencyProperty.Register(nameof(DiagnoseStatus), typeof(string), typeof(DiagnoseCard),
                new PropertyMetadata("DiagnoseStatus"));

        public static readonly DependencyProperty DiagnoseColorProperty =
            DependencyProperty.Register(nameof(DiagnoseColor), typeof(Brush), typeof(DiagnoseCard),
                new PropertyMetadata(Brushes.Gray, new PropertyChangedCallback(UpdateIcon)));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string DiagnoseDetail
        {
            get => (string)GetValue(DiagnoseDetailProperty);
            set => SetValue(DiagnoseDetailProperty, value);
        }

        public string DiagnoseStatus
        {
            get => (string)GetValue(DiagnoseStatusProperty);
            set => SetValue(DiagnoseStatusProperty, value);
        }

        public Brush DiagnoseColor
        {
            get => (Brush)GetValue(DiagnoseColorProperty);
            set => SetValue(DiagnoseColorProperty, value);
        }

        public DiagnoseCard()
        {
            InitializeComponent();
            Icon = new SymbolIcon { Symbol = SymbolRegular.QuestionCircle24 };
        }

        private static void UpdateIcon(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DiagnoseCard diagnoseCard = (DiagnoseCard)depObj;
            if (diagnoseCard.DiagnoseColor == Brushes.Green)
            {
                diagnoseCard.Icon = new SymbolIcon { Symbol = SymbolRegular.CheckmarkCircle24 };
            }
            else if (diagnoseCard.DiagnoseColor == Brushes.Red)
            {
                diagnoseCard.Icon = new SymbolIcon { Symbol = SymbolRegular.DismissCircle24 };
            }
        }
    }
}
