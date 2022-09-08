// <copyright file="Settings.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Graphics.Imaging;
    using Windows.Storage;

    /// <summary>
    /// Application Theme.
    /// </summary>
    public enum ApplicationTheme
    {
        /// <summary>
        /// OS Default theme.
        /// </summary>
        Default,

        /// <summary>
        /// Light theme.
        /// </summary>
        Light,

        /// <summary>
        /// Dark theme.
        /// </summary>
        Dark,
    }

    /// <summary>
    /// Image viewer background brush.
    /// </summary>
    public enum ImageViewBackground
    {
        /// <summary>
        /// Transparent.
        /// </summary>
        Transparent,

        /// <summary>
        /// Solid color.
        /// </summary>
        Solid,
    }

    /// <summary>
    /// Application settings.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        /// <param name="applicationData">application data.</param>
        public Settings(ApplicationData applicationData)
        {
            this.ApplicationData = applicationData;

            if (this.ApplicationData.LocalSettings.Values.TryGetValue("theme", out var theme))
            {
                try
                {
                    this.ApplicationTheme = Enum.Parse<ApplicationTheme>(theme as string);
                }
                catch (ArgumentException)
                {
                }
            }

            if (this.ApplicationData.LocalSettings.Values.TryGetValue("background", out var background))
            {
                try
                {
                    this.ImageViewBackground = Enum.Parse<ImageViewBackground>(background as string);
                }
                catch (ArgumentException)
                {
                }
            }

            if (this.ApplicationData.LocalSettings.Values.TryGetValue("image_scaling", out var scaling))
            {
                try
                {
                    this.ImageViewInterpolationMode = Enum.Parse<BitmapInterpolationMode>(scaling as string);
                }
                catch (ArgumentException)
                {
                }
            }
        }

        /// <summary>
        /// Gets or sets the application theme.
        /// </summary>
        public ApplicationTheme ApplicationTheme { get; set; } = ApplicationTheme.Default;

        /// <summary>
        /// Gets or sets image viewer background.
        /// </summary>
        public ImageViewBackground ImageViewBackground { get; set; } = ImageViewBackground.Transparent;

        /// <summary>
        /// Gets or sets image manipulation algorithm.
        /// </summary>
        public BitmapInterpolationMode ImageViewInterpolationMode { get; set; } = BitmapInterpolationMode.Fant;

        private ApplicationData ApplicationData { get; set; }

        /// <summary>
        /// Commit.
        /// </summary>
        public void Commit()
        {
            this.ApplicationData.LocalSettings.Values["theme"] = this.ApplicationTheme.ToString();
            this.ApplicationData.LocalSettings.Values["background"] = this.ImageViewBackground.ToString();
            this.ApplicationData.LocalSettings.Values["image_scaling"] = this.ImageViewInterpolationMode.ToString();
        }
    }
}
