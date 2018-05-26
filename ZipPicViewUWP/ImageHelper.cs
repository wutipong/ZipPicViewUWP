using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace ZipPicViewUWP
{
    public enum ImageOrientation { Portrait, Landscape };
    class ImageHelper
    {
        public static async Task<SoftwareBitmap> CreateResizedBitmap(BitmapDecoder decoder, uint expectedWidth, uint expectedHeight)
        {
            var expectedOrientation = expectedWidth > expectedHeight ? ImageOrientation.Landscape : ImageOrientation.Portrait;

            var width = decoder.PixelWidth;
            var height = decoder.PixelHeight;
            var imageOrientation = width > height ? ImageOrientation.Landscape : ImageOrientation.Portrait;

            if (expectedOrientation != imageOrientation)
            {
                if (imageOrientation == ImageOrientation.Landscape)
                {
                    (width, height) = ResizeToWidth(expectedWidth, width, height);
                }
                else
                {
                    (width, height) = ResizeToHeight(expectedHeight, width, height);
                }
            }
            else
            {
                if (imageOrientation == ImageOrientation.Landscape)
                {
                    (width, height) = ResizeToHeight(expectedHeight, width, height);
                }
                else
                {
                    (width, height) = ResizeToWidth(expectedWidth, width, height);
                }
            }

            var transform = new BitmapTransform
            {
                InterpolationMode = BitmapInterpolationMode.Fant,
                ScaledWidth = width,
                ScaledHeight = height
            };

            return await decoder.GetSoftwareBitmapAsync(
                      BitmapPixelFormat.Bgra8,
                      BitmapAlphaMode.Premultiplied,
                      transform,
                      ExifOrientationMode.RespectExifOrientation,
                      ColorManagementMode.ColorManageToSRgb);
        }

        private static (uint width, uint height) ResizeToHeight(uint expectedHeight, uint width, uint height)
        {
            return ((expectedHeight * width) / height, expectedHeight);
        }

        private static (uint width, uint height) ResizeToWidth(uint expectedWidth, uint width, uint height)
        {
            return (expectedWidth, (expectedWidth * height) / width);
        }

        public static Size ResizeToFill(int actualWidth, int actualHeight, double expectedWidth, double expectedHeight)
        {
            double width = actualWidth;
            double height = actualHeight;

            var imageOrientation = width > height ?
                ImageOrientation.Landscape :
                ImageOrientation.Portrait;

            switch (imageOrientation)
            {
                case ImageOrientation.Portrait:
                    (width, height) = ResizeToWidth((uint)expectedWidth, (uint)width, (uint)height);
                    break;

                case ImageOrientation.Landscape:
                    (width, height) = ResizeToHeight((uint)expectedHeight, (uint)width, (uint)height);
                    break;
            }

            return new Size(width, height);
        }
    }
}
