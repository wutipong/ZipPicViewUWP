using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace ZipPicViewUWP
{
    public class PrintPanel : Panel
    {
        public enum LayoutOption { Centered, AlignLeftOrTop, AlignRightOrBottom };

        public LayoutOption Layout { get; set; } = LayoutOption.Centered;

        private Image Image { get; set; } = new Image();
        public BitmapImage BitmapImage
        {
            get { return (BitmapImage)Image.Source; }
            set { Image.Source = value; }
        }

        public PrintPanel()
        {
            Children.Add(Image);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var finalSizeRatio = finalSize.Width / finalSize.Height;
            var imageSizeRatio = Image.DesiredSize.Width / Image.DesiredSize.Height;

            if (finalSizeRatio > imageSizeRatio)
                LayoutVertically(Image, finalSize);
            else
                LayoutHorizontally(Image, finalSize);

            return finalSize;
        }

        private void LayoutHorizontally(Image image, Size finalSize)
        {
            var anchorPoint = new Point();

            switch (Layout)
            {
                case LayoutOption.AlignLeftOrTop:
                    anchorPoint.X = 0;
                    break;
                case LayoutOption.AlignRightOrBottom:
                    anchorPoint.X = (finalSize.Width - image.DesiredSize.Width);
                    break;

                case LayoutOption.Centered:
                    anchorPoint.X = (finalSize.Width - image.DesiredSize.Width) / 2;
                    break;
            }

            image.Arrange(new Rect(anchorPoint, image.DesiredSize));
        }

        private void LayoutVertically(Image image, Size finalSize)
        {
            var anchorPoint = new Point();

            switch (Layout)
            {
                case LayoutOption.AlignLeftOrTop:
                    anchorPoint.Y = 0;
                    break;
                case LayoutOption.AlignRightOrBottom:
                    anchorPoint.Y = (finalSize.Height - image.DesiredSize.Height);
                    break;

                case LayoutOption.Centered:
                    anchorPoint.Y = (finalSize.Height - image.DesiredSize.Height) / 2;
                    break;
            }

            image.Arrange(new Rect(anchorPoint, image.DesiredSize));
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = new Size(BitmapImage.PixelWidth, BitmapImage.PixelHeight);
            Image.Measure(size.ResizeToFill(availableSize));

            return availableSize;
        }

        
    }
}
