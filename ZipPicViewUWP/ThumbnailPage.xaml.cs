namespace ZipPicViewUWP
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Graphics.Imaging;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;
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
            this.ThumbnailItemLoading += this.ThumbnailPage_ThumbnailItemLoading;
        }

        /// <summary>
        /// Event handler in the event of item selection.
        /// </summary>
        /// <param name="source">Source of the event.</param>
        /// <param name="entry">Entry being selected.</param>
        public delegate void ItemClickedHandler(object source, string entry);

        /// <summary>
        /// Event handler in the event of item load progress.
        /// </summary>
        /// <param name="source">source of the event.</param>
        /// <param name="current">current progress, equals to count when done.</param>
        /// <param name="count">total item to load.</param>
        public delegate void ItemLoadingHandler(object source, int current, int count);

        /// <summary>
        /// An event handler when an item is clicked.
        /// </summary>
        public event ItemClickedHandler ItemClicked;

        /// <summary>
        /// Event handler when thumbnail item is loading.
        /// </summary>
        public event ItemLoadingHandler ThumbnailItemLoading;

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
        /// Set the current folder entry to display thumbnail.
        /// </summary>
        /// <param name="folder">Entries to display.</param>
        public async Task SetFolderEntry(string folder)
        {
            var (entries, error) = await MediaManager.Provider.GetChildEntries(folder);
            if (error != null)
            {
                throw error;
            }

            this.FolderName.Text = (folder == MediaManager.Provider.Root) ? "<Root>" : folder.ExtractFilename();
            this.ImageCount.Text = entries.Length == 1 ? "1 image." : string.Format("{0} images.", entries.Length);

            this.Thumbnails = new Thumbnail[entries.Length];
            this.ThumbnailGrid.Items.Clear();

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var thumbnail = new Thumbnail();
                thumbnail.Label.Text = entry.ExtractFilename().Ellipses(25);
                thumbnail.UserData = entry;
                thumbnail.ProgressRing.Visibility = Visibility.Collapsed;

                this.Thumbnails[i] = thumbnail;
                this.ThumbnailGrid.Items.Add(thumbnail);
            }

            var cover = MediaManager.Provider.FileFilter.FindCoverPage(entries);
            if (cover != null && cover != string.Empty)
            {
                IRandomAccessStream stream;
                (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(cover);
                if (error != null)
                {
                    stream = await MediaManager.CreateErrorImageStream();
                }

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var bitmap = await ImageHelper.CreateResizedBitmap(decoder, 200, 200);

                var source = new SoftwareBitmapSource();
                await source.SetBitmapAsync(bitmap);

                this.CoverImage.Source = source;
            }
        }

        private void ThumbnailGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var thumbnail = e.ClickedItem as Thumbnail;
            var entry = thumbnail.UserData;

            MediaManager.CurrentEntry = entry;

            this.ItemClicked?.Invoke(this, entry);
        }

        /// <summary>
        /// Resume the thumbnail loading operation.
        /// </summary>
        /// <returns></returns>
        public async Task ResumeLoadThumbnail()
        {
            this.CancellationToken = new CancellationTokenSource();
            CancellationToken token = this.CancellationToken.Token;
            this.ProgressBorderShowStoryBoard.Begin();
            try
            {
                for (int i = 0; i < this.Thumbnails.Length; i++)
                {
                    var thumbnail = this.Thumbnails[i];
                    if (thumbnail.Image.Source != null)
                    {
                        continue;
                    }

                    var (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(thumbnail.UserData);
                    if (error != null)
                    {
                        stream = await MediaManager.CreateErrorImageStream();
                    }

                    this.ThumbnailItemLoading?.Invoke(this, i, this.Thumbnails.Length);
                    thumbnail.ProgressRing.Visibility = Visibility.Visible;

                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    SoftwareBitmap bitmap = await ImageHelper.CreateThumbnail(decoder, 250, 200);
                    token.ThrowIfCancellationRequested();

                    var source = new SoftwareBitmapSource();
                    await source.SetBitmapAsync(bitmap);

                    thumbnail.Image.Source = source;
                    thumbnail.ProgressRing.Visibility = Visibility.Collapsed;
                    thumbnail.ShowImage();
                }

                this.ThumbnailItemLoading?.Invoke(this, this.Thumbnails.Length, this.Thumbnails.Length);
            }
            catch (Exception)
            {
            }
        }

        private void ThumbnailPage_ThumbnailItemLoading(object source, int current, int count)
        {
            this.Progress.Maximum = count;
            this.Progress.Value = current;

            if (current == count)
            {
                this.ProgressBorderHideStoryBoard.Begin();
                this.ProgressBorderHideStoryBoard.Completed += (_, __) => this.ProgressBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.ProgressText.Text = string.Format("Loading Thumbnails {0}/{1}.", current, count);
            }
        }
    }
}