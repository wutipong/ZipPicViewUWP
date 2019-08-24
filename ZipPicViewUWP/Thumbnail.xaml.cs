// <copyright file="Thumbnail.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using ZipPicViewUWP.MediaProvider;
    using System;
    using System.Collections.Generic;
    using Microsoft.Toolkit.Uwp.UI.Controls;
    using Windows.Storage.Pickers;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;
    using ZipPicViewUWP.Utility;

    /// <summary>
    /// A thumbnail control.
    /// </summary>
    public sealed partial class Thumbnail : UserControl
    {
        private SoftwareBitmapSource source;

        /// <summary>
        /// Initializes a new instance of the <see cref="Thumbnail"/> class.
        /// </summary>
        public Thumbnail()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the thumbnail label.
        /// </summary>
        public TextBlock Label => this.label;

        /// <summary>
        /// Gets the progress ring.
        /// </summary>
        public ProgressRing ProgressRing => this.loading;

        /// <summary>
        /// Gets or sets the entry assciated with the thumbnail.
        /// </summary>
        public string Entry { get; set; }

        /// <summary>
        /// Gets or sets the print helper object.
        /// </summary>
        public PrintHelper PrintHelper { get; set; }

        /// <summary>
        /// Gets or sets the notification object.
        /// </summary>
        public InAppNotification Notification { get; set; }

        /// <summary>
        /// Gets or sets the image source for the thumbnail.
        /// </summary>
        public SoftwareBitmapSource ImageSource
        {
            get => this.source;
            set
            {
                this.source = value;
                this.image.Source = value;
            }
        }

        /// <summary>
        /// Show the thumbnail Image.
        /// </summary>
        public void ShowImage()
        {
            this.ThumbnailShowStoryBoard.Begin();
        }

        /// <summary>
        /// Release the resources used by this control.
        /// </summary>
        public void Release()
        {
            this.source?.Dispose();
        }

        private void UserControl_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.Highlight.Opacity = 100;
        }

        private void UserControl_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.Highlight.Opacity = 0;
        }

        private void UserControl_ContextRequested(UIElement sender, Windows.UI.Xaml.Input.ContextRequestedEventArgs args)
        {
            this.CommandBarFlyout.ShowAt(this);
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var error = await MediaManager.CopyToClipboard(this.Entry);

            if (error == null)
            {
                this.Notification.Show(string.Format("The image {0} has been copied to the clipboard.", this.Entry.ExtractFilename()), 1000);
            }
            else
            {
                var dialog = new MessageDialog(string.Format("Cannot copy image from file: {0}.", this.Entry.ExtractFilename()), "Error");
                await dialog.ShowAsync();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var entry = this.Entry;
            var suggestedFileName = MediaManager.Provider.SuggestFileNameToSave(entry);

            var picker = new FileSavePicker
            {
                SuggestedFileName = suggestedFileName,
            };

            picker.FileTypeChoices.Add("All", new List<string>() { "." });
            var file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                return;
            }

            var error = await MediaManager.SaveFileAs(entry, file);

            if (error != null)
            {
                var dialog = new MessageDialog(string.Format("Cannot save image file: {0}.", file.Name), "Error");
                await dialog.ShowAsync();
            }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var error = await MediaManager.Print(this.PrintHelper, this.Entry);

            if (error != null)
            {
                var dialog = new MessageDialog(string.Format("Cannot copy image from file: {0}.", this.Entry.ExtractFilename()), "Error");
                await dialog.ShowAsync();
            }
        }
    }
}