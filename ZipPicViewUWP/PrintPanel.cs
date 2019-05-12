namespace ZipPicViewUWP
{
    using Windows.Foundation;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;

    public class PrintPanel : Panel
    {
        public PrintPanel()
        {
            this.Children.Add(this.Image);
        }

        public enum LayoutOption
        {
            Centered,
            AlignLeftOrTop,
            AlignRightOrBottom,
        }

        public BitmapImage BitmapImage
        {
            get { return (BitmapImage)this.Image.Source; }
            set { this.Image.Source = value; }
        }

        public LayoutOption Layout { get; set; } = LayoutOption.Centered;

        private Image Image { get; set; } = new Image();

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
