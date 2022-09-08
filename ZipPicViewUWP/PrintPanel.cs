// <copyright file="PrintPanel.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

using ZipPicViewUWP.MediaProvider;

namespace ZipPicViewUWP
{
    using Windows.Foundation;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    ///  A Placeholder panel holding an image that will be printed.
    /// </summary>
    public class PrintPanel : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrintPanel"/> class.
        /// </summary>
        public PrintPanel()
        {
            this.Children.Add(this.Image);
        }

        /// <summary>
        /// Layout option.
        /// </summary>
        public enum LayoutOption
        {
            /// <summary>
            /// Print image is in the center.
            /// </summary>
            Centered,

            /// <summary>
            /// Print image is aligned to the left, or to the top, depends on the page size and the image size.
            /// </summary>
            AlignLeftOrTop,

            /// <summary>
            /// Print image is aligned to the right, or to the bottom, depends on the page size and the image size.
            /// </summary>
            AlignRightOrBottom,
        }

        /// <summary>
        /// Gets or sets image to be printed.
        /// </summary>
        public BitmapImage BitmapImage
        {
            get { return (BitmapImage)this.Image.Source; }
            set { this.Image.Source = value; }
        }

        /// <summary>
        /// Gets or sets the layout option.
        /// </summary>
        public LayoutOption Layout { get; set; } = LayoutOption.Centered;

        private Image Image { get; set; } = new Image();

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var finalSizeRatio = finalSize.Width / finalSize.Height;
            var imageSizeRatio = this.Image.DesiredSize.Width / this.Image.DesiredSize.Height;

            if (finalSizeRatio > imageSizeRatio)
            {
                this.LayoutVertically(this.Image, finalSize);
            }
            else
            {
                this.LayoutHorizontally(this.Image, finalSize);
            }

            return finalSize;
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var size = new Size(this.BitmapImage.PixelWidth, this.BitmapImage.PixelHeight);
            this.Image.Measure(size.ResizeToFill(availableSize));

            return availableSize;
        }

        private void LayoutHorizontally(Image image, Size finalSize)
        {
            var anchorPoint = default(Point);

            switch (this.Layout)
            {
                case LayoutOption.AlignLeftOrTop:
                    anchorPoint.X = 0;
                    break;

                case LayoutOption.AlignRightOrBottom:
                    anchorPoint.X = finalSize.Width - image.DesiredSize.Width;
                    break;

                case LayoutOption.Centered:
                    anchorPoint.X = (finalSize.Width - image.DesiredSize.Width) / 2;
                    break;
            }

            image.Arrange(new Rect(anchorPoint, image.DesiredSize));
        }

        private void LayoutVertically(Image image, Size finalSize)
        {
            var anchorPoint = default(Point);

            switch (this.Layout)
            {
                case LayoutOption.AlignLeftOrTop:
                    anchorPoint.Y = 0;
                    break;

                case LayoutOption.AlignRightOrBottom:
                    anchorPoint.Y = finalSize.Height - image.DesiredSize.Height;
                    break;

                case LayoutOption.Centered:
                    anchorPoint.Y = (finalSize.Height - image.DesiredSize.Height) / 2;
                    break;
            }

            image.Arrange(new Rect(anchorPoint, image.DesiredSize));
        }
    }
}