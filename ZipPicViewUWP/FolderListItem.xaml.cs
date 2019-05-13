// <copyright file="FolderListItem.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    /// <summary>
    /// Folder list item control.
    /// </summary>
    public sealed partial class FolderListItem : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FolderListItem"/> class.
        /// </summary>
        public FolderListItem()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the displaying folder name.
        /// </summary>
        public string Text
        {
            get { return this.name.Text; }
            set { this.name.Text = value; }
        }

        /// <summary>
        /// Gets or sets the actual folder value.
        /// </summary>
        public string Value { get; set; }

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