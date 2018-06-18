using System;
using Windows.Foundation;
using ZipPicViewUWP.Utility;

namespace ZipPicViewUWP
{
    public static class ImageSizeHelper
    {
        public static Size ResizeToFill(this Size s, Size expectedSize)
        {
            switch (s.GetOrientation())
            {
                case ImageOrientation.Portrait:
                    return s.ResizeToWidth(expectedSize.Width);

                case ImageOrientation.Landscape:
                    return s.ResizeToHeight(expectedSize.Height);

                default:
                    throw new ArgumentException();
            }
        }

        public static Size ResizeToFit(this Size size, Size expectedSize)
        {
            var imageOrientation = size.GetOrientation();
            var expectedOrientation = expectedSize.GetOrientation();

            if (expectedOrientation != imageOrientation)
            {
                if (imageOrientation == ImageOrientation.Landscape)
                {
                    return size.ResizeToWidth(expectedSize.Width);
                }
                else
                {
                    return size.ResizeToHeight(expectedSize.Height);
                }
            }
            else
            {
                if (imageOrientation == ImageOrientation.Landscape)
                {
                    return size.ResizeToHeight(expectedSize.Height);
                }
                else
                {
                    return size.ResizeToWidth(expectedSize.Width);
                }
            }
        }

        public static Size ResizeToWidth(this Size s, double expectedWidth)
        {
            var (width, height) = ImageSizeExtension.ResizeToWidth(expectedWidth, s.Width, s.Height);
            return new Size(width, height);
        }

        public static Size ResizeToHeight(this Size s, double expectedHeight)
        {
            var (width, height) = ImageSizeExtension.ResizeToHeight(expectedHeight, s.Width, s.Height);
            return new Size(width, height);
        }

        public static ImageOrientation GetOrientation(this Size s)
        {
            return s.Width > s.Height ? ImageOrientation.Landscape : ImageOrientation.Portrait;
        }
    }
}