// <copyright file="PrintPage.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PrintPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrintPage"/> class.
        /// </summary>
        public PrintPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the layout option.
        /// </summary>
        public PrintPanel.LayoutOption LayoutOption
        {
            get { return this.PrintPanel.Layout; }
            set { this.PrintPanel.Layout = value; }
        }

        /// <summary>
        /// Gets or sets the image to be printed.
        /// </summary>
        public BitmapImage Image
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