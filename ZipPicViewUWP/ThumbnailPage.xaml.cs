namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading;
    using Windows.Foundation;
    using Windows.Foundation.Collections;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Data;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Navigation;
    using ZipPicViewUWP.Utility;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ThumbnailPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThumbnailPage"/> class.
        /// </summary>
        public ThumbnailPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Event handler in the event of item selection.
        /// </summary>
        /// <param name="source">Source of the event.</param>
        /// <param name="entry">Entry being selected.</param>
        public delegate void ItemClickedHandler(object source, string entry);

        /// <summary>
        /// An event handler when an item is clicked.
        /// </summary>
        public event ItemClickedHandler ItemClicked;

        /// <summary>
        /// Gets the thumbnails.
        /// </summary>
        public Thumbnail[] Thumbnails
        {
            get; private set;
        }

        /// <summary>
        /// Gets the cancellation token of thumbnail loading.
        /// </summary>
        public CancellationTokenSource CancellationToken { get; private set; }

        /// <summary>
        /// Set the entries to display thumbnail.
        /// </summary>
        /// <param name="entries">Entries to display.</param>
        public void SetEntries(string[] entries)
        {
            this.Thumbnails = new Thumbnail[entries.Length];
            this.ThumbnailGrid.Items.Clear();

            foreach (var entry in entries)
            {
                var thumbnail = new Thumbnail();
                thumbnail.Label.Text = entry.ExtractFilename().Ellipses(25);
                thumbnail.UserData = entry;
                thumbnail.ProgressRing.Visibility = Visibility.Collapsed;

                this.ThumbnailGrid.Items.Add(thumbnail);
            }
        }

        private void ThumbnailGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var thumbnail = e.ClickedItem as Thumbnail;
            var entry = thumbnail.UserData;

            MediaManager.CurrentEntry = entry;

            this.ItemClicked?.Invoke(this, entry);
        }
    }
}
