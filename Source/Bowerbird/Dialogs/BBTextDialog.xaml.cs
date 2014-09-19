using System.Windows;
using Bowerbird.Dialogs.Converters;

namespace Bowerbird.Dialogs
{
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    public partial class BBTextDialog : Window
    {
        public BBTextDialog()
        {
            InitializeComponent();

            var i = new ParamToIntConverter();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


        public static readonly DependencyProperty BoldProperty = DependencyProperty.Register(
            "Bold", typeof (bool), typeof (BBTextDialog), new PropertyMetadata(default(bool)));

        public bool Bold
        {
            get { return (bool) GetValue(BoldProperty); }
            set { SetValue(BoldProperty, value); }
        }

        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            "Size", typeof (double), typeof (BBTextDialog), new PropertyMetadata(10.0));

        public double Size
        {
            get { return (double) GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof (string), typeof (BBTextDialog), new PropertyMetadata(default(string)));

        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        
        public static readonly DependencyProperty HAlignProperty = DependencyProperty.Register(
            "HAlign", typeof(int), typeof(BBTextDialog), new PropertyMetadata(0));

        public int HAlign
        {
            get { return (int)GetValue(HAlignProperty); }
            set { SetValue(HAlignProperty, value); }
        }

        public static readonly DependencyProperty VAlignProperty = DependencyProperty.Register(
            "VAlign", typeof(int), typeof(BBTextDialog), new PropertyMetadata(0));

        public int VAlign
        {
            get { return (int)GetValue(VAlignProperty); }
            set { SetValue(VAlignProperty, value); }
        }
    }
}
