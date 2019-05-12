namespace ZipPicViewUWP
{
    using System;
    using Windows.Foundation;
    using ZipPicViewUWP.Utility;

    public static class ImageSizeHelper
    {
        public static Size ResizeToFill(this Size s, Size expectedSize)
        {
            var sizeRatio = s.Width / s.Height;
            var expectSizeRatio = expectedSize.Width / expectedSize.Height;

            if (sizeRatio < expectSizeRatio)
            {
                return s.ResizeToWidth(expectedSize.Width);
            }
            else
            {
                return s.ResizeToHeight(expectedSize.Height);
            }
        }

        public static Size ResizeToFit(this Size s, Size expectedSize)
        {
            var sizeRatio = s.Width / s.Height;
            var expectSizeRatio = expectedSize.Width / expectedSize.Height;

            if (sizeRatio > expectSizeRatio)
            {
                return s.ResizeToWidth(expectedSize.Width);
            }
            else
            {
                return s.ResizeToHeight(expectedSize.Height);
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