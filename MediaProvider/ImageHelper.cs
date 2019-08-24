// <copyright file="ImageHelper.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP.MediaProvider
{
    using System;
    using System.Threading.Tasks;
    using Windows.Foundation;
    using Windows.Graphics.Imaging;

    /// <summary>
    /// Image orientation.
    /// </summary>
    public enum ImageOrientation
    {
        /// <summary>
        /// Portrait.
        /// </summary>
        Portrait,

        /// <summary>
        /// Landscape
        /// </summary>
        Landscape,
    }

    /// <summary>
    /// Image helper class.
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Create a new image with smallest size that is larger than the input dimension.
        /// </summary>
        /// <param name="decoder">Bitmap decoder.</param>
        /// <param name="expectedWidth"> Expected width.</param>
        /// <param name="expectedHeight">Expected height.</param>
        /// <param name="mode">Interpolation mode.</param>
        /// <returns>a new image with smallest size that is larger than the input dimension.</returns>
        public static async Task<SoftwareBitmap> CreateResizedBitmap(BitmapDecoder decoder, uint expectedWidth, uint expectedHeight, BitmapInterpolationMode mode = BitmapInterpolationMode.Fant, bool shrinkingOnly = false)
        {
            var expectedSize = new Size(expectedWidth, expectedHeight);

            var size = new Size(decoder.PixelWidth, decoder.PixelHeight);

            expectedSize = size.ResizeToFill(expectedSize);

            if (shrinkingOnly)
            {
                if (expectedSize.Width > decoder.OrientedPixelWidth && expectedSize.Height > decoder.OrientedPixelHeight)
                {
                    size = expectedSize;
                }
            }
            else
            {
                size = expectedSize;
            }

            var transform = new BitmapTransform
            {
                InterpolationMode = mode,
                ScaledWidth = (uint)size.Width,
                ScaledHeight = (uint)size.Height,
            };

            return await decoder.GetSoftwareBitmapAsync(
                      BitmapPixelFormat.Bgra8,
                      BitmapAlphaMode.Premultiplied,
                      transform,
                      ExifOrientationMode.RespectExifOrientation,
                      ColorManagementMode.ColorManageToSRgb);
        }

        /// <summary>
        /// Create a thumbnail image from the input. The thumbnail is taken from the image file in the case that the file has one.
        /// Otherwise it will be a new image that is created from the image file.
        /// </summary>
        /// <param name="decoder">Bitmap decoder.</param>
        /// <param name="expectedWidth"> Expected width.</param>
        /// <param name="expectedHeight">Expected height.</param>
        /// <returns>a new image with smallest size that is larger than the input dimension.</returns>
        public static async Task<SoftwareBitmap> CreateThumbnail(BitmapDecoder decoder, uint expectedWidth, uint expectedHeight)
        {
            BitmapDecoder decoder2;
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
                catch
                {
                    decoder2 = null;
                }
            }

            if (decoder2 == null)
            {
                decoder2 = decoder;
            }

            return await CreateResizedBitmap(decoder2, expectedWidth, expectedHeight, BitmapInterpolationMode.Linear);
        }
    }
}