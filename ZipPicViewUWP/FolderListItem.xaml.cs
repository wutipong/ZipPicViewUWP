namespace ZipPicViewUWP
{
    using System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FolderListItem : UserControl
    {
        public FolderListItem()
        {
            this.InitializeComponent();
        }

        public string Text
        {
            get { return this.name.Text; }
            set { this.name.Text = value; }
        }

        public string Value { get; set; }

        public ImageSource ImageSource
        {
            set
            {
                if (value == null)
                {
                    this.image.Visibility = Visibility.Collapsed;
                    this.folderIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    this.image.Source = value;
                    this.image.Visibility = Visibility.Visible;
                    this.folderIcon.Visibility = Visibility.Collapsed;
                }
            }
        }

        public async void SetImageSourceAsync(ImageSource source)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.ImageSource = source;
            });
        }
    }
}