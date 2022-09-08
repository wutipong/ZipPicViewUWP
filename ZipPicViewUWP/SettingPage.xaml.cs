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
        private readonly Settings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingPage"/> class.
        /// </summary>
        public SettingPage()
        {
            this.InitializeComponent();

            this.settings = new Settings(ApplicationData);
            this.ThemeComboBox.SelectedIndex = (int)this.settings.ApplicationTheme;
            this.BackgroundComboBox.SelectedIndex = (int)this.settings.ImageViewBackground;
            this.ImageManipulationComboBox.SelectedIndex = (int)this.settings.ImageViewInterpolationMode;
        }

        private static ApplicationData ApplicationData => Windows.Storage.ApplicationData.Current;

        private void ThemeComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.ThemeComboBox.SelectedIndex)
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

        private void BackgroundComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.BackgroundComboBox.SelectedIndex)
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

        private void ImageManipulationComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.ImageManipulationComboBox.SelectedIndex)
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