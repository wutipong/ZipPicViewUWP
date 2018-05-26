using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ZipPicViewUWP
{
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
            get { return PrintPanel.Layout; }
            set { PrintPanel.Layout = value; }
        }

        public BitmapImage ImageSource
        {
            get
            {
                return PrintPanel.BitmapImage;
            }

            set
            {
                PrintPanel.BitmapImage = value;
            }
        }
    }
}
