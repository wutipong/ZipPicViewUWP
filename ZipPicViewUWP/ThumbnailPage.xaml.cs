// <copyright file="ThumbnailPage.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Toolkit.Uwp.UI.Controls;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;
    using ZipPicViewUWP.Utility;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ThumbnailPage : Page
    {
        private SoftwareBitmapSource coverBitmapSource;

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
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get => this.FolderName.Text;
            set
            {
                this.FolderName.Text = value;
                var toolTip = new ToolTip { Content = value };
                ToolTipService.SetToolTip(this.FolderName, toolTip);
            }
        }

        /// <summary>
        /// Gets or sets the style of title text.
        /// </summary>
        public Windows.UI.Text.FontStyle TitleStyle
        {
            get => this.FolderName.FontStyle;
            set => this.FolderName.FontStyle = value;
        }

        /// <summary>
        /// Gets or sets the notification control.
        /// </summary>
        public InAppNotification Notification { get; set; }

        /// <summary>
        /// Gets the cancellation token of thumbnail loading.
        /// </summary>
        public CancellationTokenSource CancellationToken { get; private set; }

        /// <summary>
        /// Gets or sets the print helper.
        /// </summary>
        public PrintHelper PrintHelper { get; set; }

        /// <summary>
        /// Gets the entry value.
        /// </summary>
        public string Entry { get; private set; }

        /// <summary>
        /// Set the current folder entry to display thumbnail.
        /// </summary>
        /// <param name="folder">Entries to display.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SetFolderEntry(string folder)
        {
            var entries = await MediaManager.Provider.GetChildEntries(folder);

            this.Entry = folder;

            var fileNames = entries as string[] ?? entries.ToArray();
            this.ImageCount.Text = fileNames.Length == 1 ?
                "1 image." :
                string.Format($"{fileNames.Length} images.");

            this.Thumbnails = new Thumbnail[fileNames.Length];
            this.ThumbnailGrid?.Items?.Clear();

            for (var i = 0; i < fileNames.Length; i++)
            {
                var entry = fileNames[i];
                var thumbnail = new Thumbnail();
                var filename = entry.ExtractFilename();

                thumbnail.Label.Text = filename.Ellipses(25);
                thumbnail.Entry = entry;
                thumbnail.ProgressRing.Visibility = Visibility.Collapsed;
                thumbnail.Notification = this.Notification;
                thumbnail.PrintHelper = this.PrintHelper;

                var toolTip = new ToolTip { Content = filename };
                ToolTipService.SetToolTip(thumbnail, toolTip);

                this.Thumbnails[i] = thumbnail;
                this.ThumbnailGrid?.Items?.Add(thumbnail);
            }

            var cover = MediaManager.Provider.FileFilter.FindCoverPage(fileNames);
            if (!string.IsNullOrEmpty(cover))
            {
                var (bitmap, _, _) = await MediaManager.CreateImage(cover, 200, 200, Windows.Graphics.Imaging.BitmapInterpolationMode.Linear);

                this.coverBitmapSource = new SoftwareBitmapSource();
                await this.coverBitmapSource.SetBitmapAsync(bitmap);

                this.CoverImage.Source = this.coverBitmapSource;
            }
        }

        /// <summary>
        /// Resume the thumbnail loading operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ResumeLoadThumbnail()
        {
            this.CancellationToken = new CancellationTokenSource();
            var token = this.CancellationToken.Token;
            this.ProgressBorderShowStoryBoard.Begin();
            try
            {
                for (var i = 0; i < this.Thumbnails.Length; i++)
                {
                    var thumbnail = this.Thumbnails[i];
                    if (thumbnail.ImageSource != null)
                    {
                        continue;
                    }

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    this.ThumbnailItemLoading?.Invoke(this, i, this.Thumbnails.Length);
                    thumbnail.ProgressRing.Visibility = Visibility.Visible;
                    var bitmap = await MediaManager.CreateThumbnail(thumbnail.Entry, 250, 200);

                    var source = new SoftwareBitmapSource();
                    await source.SetBitmapAsync(bitmap);

                    thumbnail.ImageSource = source;
                    thumbnail.ProgressRing.Visibility = Visibility.Collapsed;
                    thumbnail.ShowImage();
                }

                this.ThumbnailItemLoading?.Invoke(this, this.Thumbnails.Length, this.Thumbnails.Length);
            }
            catch (Exception)
            {
                // Ignore error if any.
            }
        }

        /// <summary>
        /// Release resources used by this control.
        /// </summary>
        public void Release()
        {
            this.coverBitmapSource?.Dispose();
            foreach (var thumbnail in this.Thumbnails)
            {
                thumbnail.Release();
            }
        }

        private void ThumbnailGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var thumbnail = e.ClickedItem as Thumbnail;
            var entry = thumbnail?.Entry;

            MediaManager.CurrentEntry = entry;

            this.ItemClicked?.Invoke(this, entry);
        }

        private void ThumbnailPage_ThumbnailItemLoading(object source, int current, int count)
        {
            this.Progress.IsIndeterminate = false;
            this.Progress.Maximum = count;
            this.Progress.Value = current;

            if (current == count)
            {
                this.ProgressBorderHideStoryBoard.Begin();
                this.ProgressBorderHideStoryBoard.Completed += (_, __) => this.ProgressBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.ProgressText.Text = $"Loading Thumbnails {current + 1}/{count}.";
            }
        }
    }
}