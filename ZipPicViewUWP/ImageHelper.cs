using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using ZipPicViewUWP.Utility;

namespace ZipPicViewUWP
{
    public enum ImageOrientation { Portrait, Landscape };

    public static class ImageHelper
    {
        public static async Task<SoftwareBitmap> CreateResizedBitmap(BitmapDecoder decoder, uint expectedWidth, uint expectedHeight)
        {
            var expectedSize = new Size(expectedWidth, expectedHeight);
         
            var size = new Size(decoder.PixelWidth, decoder.PixelHeight);

            size = size.ResizeToFit(expectedSize);

            var transform = new BitmapTransform
            {
                InterpolationMode = BitmapInterpolationMode.Fant,
                ScaledWidth = (uint)size.Width,
                ScaledHeight = (uint)size.Height
            };

            return await decoder.GetSoftwareBitmapAsync(
                      BitmapPixelFormat.Bgra8,
                      BitmapAlphaMode.Premultiplied,
                      transform,
                      ExifOrientationMode.RespectExifOrientation,
                      ColorManagementMode.ColorManageToSRgb);
        }
    }
}