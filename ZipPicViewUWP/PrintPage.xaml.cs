// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ZipPicViewUWP
{
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PrintPage : Page
    {
        public PrintPage()
        {
            this.InitializeComponent();
        }

        public PrintPanel.LayoutOption LayoutOption
        {
            get { return this.PrintPanel.Layout; }
            set { this.PrintPanel.Layout = value; }
        }

        public BitmapImage ImageSource
        {
            get
            {
                return this.PrintPanel.BitmapImage;
            }

            set
            {
                this.PrintPanel.BitmapImage = value;
            }
        }
    }
}
