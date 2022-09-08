// <copyright file="ImageSizeHelper.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using Windows.Foundation;
    using ZipPicViewUWP.Utility;

    /// <summary>
    /// A helper class for calculating image size.
    /// </summary>
    public static class ImageSizeHelper
    {
        /// <summary>
        /// Calculate the image size that will fill the given size.
        /// </summary>
        /// <param name="inputSize">Input size.</param>
        /// <param name="expectedSize">Expected size.</param>
        /// <returns>The smallest image size (with the same aspect ratio) that is larger than or equal to the expected size.</returns>
        public static Size ResizeToFill(this Size inputSize, Size expectedSize)
        {
            var sizeRatio = inputSize.Width / inputSize.Height;
            var expectSizeRatio = expectedSize.Width / expectedSize.Height;

            if (sizeRatio < expectSizeRatio)
            {
                return inputSize.ResizeToWidth(expectedSize.Width);
            }
            else
            {
                return inputSize.ResizeToHeight(expectedSize.Height);
            }
        }

        /// <summary>
        /// Calculate the image size that fits the given size.
        /// </summary>
        /// <param name="inputSize">Input size.</param>
        /// <param name="expectedSize">Expected size.</param>
        /// <returns>The largest image size (with the same aspect ratio) that is smaller
        /// than or equal to the expected size.</returns>
        public static Size ResizeToFit(this Size inputSize, Size expectedSize)
        {
            var sizeRatio = inputSize.Width / inputSize.Height;
            var expectSizeRatio = expectedSize.Width / expectedSize.Height;

            if (sizeRatio > expectSizeRatio)
            {
                return inputSize.ResizeToWidth(expectedSize.Width);
            }
            else
            {
                return inputSize.ResizeToHeight(expectedSize.Height);
            }
        }

        /// <summary>
        /// Calculate an image size with the same width and aspect ratio.
        /// </summary>
        /// <param name="inputSize">Input size.</param>
        /// <param name="expectedWidth">Expected Width.</param>
        /// <returns>an image size with the same width and aspect ratio.</returns>
        public static Size ResizeToWidth(this Size inputSize, double expectedWidth)
        {
            var (width, height) = ImageSizeExtension.ResizeToWidth(
                expectedWidth,
                inputSize.Width,
                inputSize.Height);

            return new Size(width, height);
        }

        /// <summary>
        /// Calculate an image size with the same height and aspect ratio.
        /// </summary>
        /// <param name="inputSize">Input size.</param>
        /// <param name="expectedHeight">Expected Width.</param>
        /// <returns>an image size with the same width and aspect ratio.</returns>
        public static Size ResizeToHeight(this Size inputSize, double expectedHeight)
        {
            var (width, height) = ImageSizeExtension.ResizeToHeight(
                expectedHeight,
                inputSize.Width,
                inputSize.Height);

            return new Size(width, height);
        }

        /// <summary>
        /// Find the orientation of the given size.
        /// </summary>
        /// <param name="s">Size.</param>
        /// <returns>Orientation, landscape or portrait.</returns>
        public static ImageOrientation GetOrientation(this Size s)
        {
            return s.Width > s.Height ? ImageOrientation.Landscape : ImageOrientation.Portrait;
        }
    }
}