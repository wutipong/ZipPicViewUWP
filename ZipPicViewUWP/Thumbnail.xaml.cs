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
        /// Show the thumbnail Image.
        /// </summary>
        public void ShowImage()
        {
            this.ThumbnailShowStoryBoard.Begin();
        }

        /// <summary>
        /// Event handler that will be called when the control is clicked.
        /// </summary>
        public event RoutedEventHandler Click;

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
        /// Gets or sets the custom user data assciated with the thumbnail.
        /// </summary>
        public string UserData { get; set; }
    }
}