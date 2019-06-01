// <copyright file="Thumbnail.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// A thumbnail control.
    /// </summary>
    public sealed partial class Thumbnail : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Thumbnail"/> class.
        /// </summary>
        public Thumbnail()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the thumbnail image.
        /// </summary>
        public Image Image => this.image;

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
        /// Show the thumbnail Image.
        /// </summary>
        public void ShowImage()
        {
            this.ThumbnailShowStoryBoard.Begin();
        }

        private void UserControl_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.Highlight.Opacity = 100;
        }

        private void UserControl_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.Highlight.Opacity = 0;
        }
    }
}