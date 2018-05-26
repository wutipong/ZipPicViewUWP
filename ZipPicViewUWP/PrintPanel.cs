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
            var orientation = finalSize.Width > finalSize.Height ?
                ImageOrientation.Landscape :
                ImageOrientation.Portrait;

            var imageOrientation = Image.DesiredSize.Width > Image.DesiredSize.Height ?
                ImageOrientation.Landscape :
                ImageOrientation.Portrait;

            if (orientation == imageOrientation)
            {
                switch (orientation)
                {
                    case ImageOrientation.Portrait:
                        LayoutVertically(Image, finalSize);
                        break;
                    case ImageOrientation.Landscape:
                        LayoutHorizontally(Image, finalSize);
                        break;
                }
            }

            else
            {
                switch (orientation)
                {
                    case ImageOrientation.Portrait:
                        LayoutHorizontally(Image, finalSize);
                        break;
                    case ImageOrientation.Landscape:
                        LayoutVertically(Image, finalSize);
                        break;
                }
            }

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
            Image.Measure(ImageHelper.ResizeToFill(BitmapImage.PixelWidth, BitmapImage.PixelHeight, availableSize.Width, availableSize.Height));

            return availableSize;
        }

        
    }
}
