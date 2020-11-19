// <copyright file="SettingPage.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using Windows.Storage;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingPage"/> class.
        /// </summary>

        private readonly Settings settings;

        public SettingPage()
        {
            this.InitializeComponent();

            this.settings = new Settings(this.ApplicationData);
            this.cmbTheme.SelectedIndex = (int)this.settings.ApplicationTheme;
            this.cmbBackground.SelectedIndex = (int)this.settings.ImageViewBackground;
            this.cmbScaling.SelectedIndex = (int)this.settings.ImageViewInterpolationMode;
        }

        private ApplicationData ApplicationData => Windows.Storage.ApplicationData.Current;

        private void cmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.cmbTheme.SelectedIndex)
            {
                case 0:
                    this.settings.ApplicationTheme = ApplicationTheme.Default;
                    break;
                case 1:
                    this.settings.ApplicationTheme = ApplicationTheme.Light;
                    break;

                case 2:
                    this.settings.ApplicationTheme = ApplicationTheme.Dark;
                    break;
            }

            this.settings.Commit();
        }

        private void cmbBackground_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.cmbBackground.SelectedIndex)
            {
                case 0:
                    this.settings.ImageViewBackground = ZipPicViewUWP.ImageViewBackground.Transparent;
                    break;

                case 1:
                    this.settings.ImageViewBackground = ZipPicViewUWP.ImageViewBackground.Solid;
                    break;
            }

            this.settings.Commit();
        }

        private void cmbScaling_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.cmbScaling.SelectedIndex)
            {
                case 0:
                    this.settings.ImageViewInterpolationMode = Windows.Graphics.Imaging.BitmapInterpolationMode.NearestNeighbor;
                    break;

                case 1:
                    this.settings.ImageViewInterpolationMode = Windows.Graphics.Imaging.BitmapInterpolationMode.Linear;
                    break;

                case 2:
                    this.settings.ImageViewInterpolationMode = Windows.Graphics.Imaging.BitmapInterpolationMode.Cubic;
                    break;

                case 3:
                    this.settings.ImageViewInterpolationMode = Windows.Graphics.Imaging.BitmapInterpolationMode.Fant;
                    break;
            }

            this.settings.Commit();
        }
    }
}