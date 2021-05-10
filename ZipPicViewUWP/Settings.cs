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

    public enum ApplicationTheme
    {
        Default, Light, Dark
    }

    public enum ImageViewBackground
    {
        Transparent, Solid
    }

    public class Settings
    {
        public ApplicationTheme ApplicationTheme { get; set; } = ApplicationTheme.Default;

        public ImageViewBackground ImageViewBackground { get; set; } = ImageViewBackground.Transparent;

        public BitmapInterpolationMode ImageViewInterpolationMode { get; set; } = BitmapInterpolationMode.Fant;

        private ApplicationData ApplicationData { get; set; }

        public Settings(ApplicationData applicationData)
        {
            this.ApplicationData = applicationData;

            if (this.ApplicationData.LocalSettings.Values.TryGetValue("theme", out var theme))
            {
                try
                {
                    this.ApplicationTheme = Enum.Parse<ApplicationTheme>(theme as string);
                }
                catch (ArgumentException) { }
            }

            if (this.ApplicationData.LocalSettings.Values.TryGetValue("background", out var background))
            {
                try
                {
                    this.ImageViewBackground = Enum.Parse<ImageViewBackground>(background as string);
                }
                catch (ArgumentException) { }
            }

            if (this.ApplicationData.LocalSettings.Values.TryGetValue("image_scaling", out var scaling))
            {
                try
                {
                    this.ImageViewInterpolationMode = Enum.Parse<BitmapInterpolationMode>(scaling as string);
                }
                catch (ArgumentException) { }
            }
        }

        public void Commit()
        {
            this.ApplicationData.LocalSettings.Values["theme"] = this.ApplicationTheme.ToString();
            this.ApplicationData.LocalSettings.Values["background"] = this.ImageViewBackground.ToString();
            this.ApplicationData.LocalSettings.Values["image_scaling"] = this.ImageViewInterpolationMode.ToString();
        }
    }
}
