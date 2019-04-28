using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace ZipPicViewUWP
{
    public enum ImageOrientation { Portrait, Landscape };

    public static class ImageHelper
    {
        public static async Task<SoftwareBitmap> CreateResizedBitmap(BitmapDecoder decoder, uint expectedWidth, uint expectedHeight)
        {
            var expectedSize = new Size(expectedWidth, expectedHeight);

            var size = new Size(decoder.PixelWidth, decoder.PixelHeight);

            size = size.ResizeToFill(expectedSize);

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

        public static async Task<SoftwareBitmap> CreateThumbnail(BitmapDecoder decoder, uint expectedWidth, uint expectedHeight)
        {
            BitmapDecoder decoder2 = null;
            try
            {
                var thumbnail = await decoder.GetThumbnailAsync();
                decoder2 = await BitmapDecoder.CreateAsync(thumbnail);
            }
            catch
            {
                decoder2 = null;
            }

            if (decoder2 == null)
            {
                try
                {
                    var preview = await decoder.GetPreviewAsync();
                    decoder2 = await BitmapDecoder.CreateAsync(preview);
                }
                catch { decoder2 = null; }
            }

            if (decoder2 == null)
                decoder2 = decoder;

            return await CreateResizedBitmap(decoder2, expectedWidth, expectedHeight);
        }
    }
}
