// <copyright file="FolderListItem.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Linq;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using ZipPicViewUWP.Utility;

    /// <summary>
    /// Folder list item control.
    /// </summary>
    public sealed partial class FolderListItem : UserControl
    {
        private string entry;

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderListItem"/> class.
        /// </summary>
        public FolderListItem()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the actual folder value.
        /// </summary>
        public string FolderEntry
        {
            get => this.entry;
            set
            {
                this.entry = value;

                if (this.entry == MediaManager.Provider.Root)
                {
                    this.name.Text = "<ROOT>";
                    this.name.FontStyle = Windows.UI.Text.FontStyle.Oblique;
                }
                else
                {
                    int prefixCount = this.entry.Count(c => c == MediaManager.Provider.Separator);
                    this.name.Padding = new Thickness() { Left = 10 * (prefixCount + 1) };
                    this.name.Text = "├  " + value.ExtractFilename();
                }
            }
        }

        /// <summary>
        /// Sets the thumbnail image source.
        /// </summary>
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

        /// <summary>
        /// Set the thumbnail image source asynchronously.
        /// </summary>
        /// <param name="source">image source.</param>
        public async void SetImageSourceAsync(ImageSource source)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.ImageSource = source;
            });
        }
    }
}