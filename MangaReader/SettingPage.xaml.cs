// <copyright file="SettingPage.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace MangaReader
{
    using System;
    using Windows.Storage;
    using Windows.Storage.AccessCache;
    using Windows.Storage.Pickers;
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
        public SettingPage()
        {
            this.InitializeComponent();

            this.ApplicationData.LocalSettings.Values.TryGetValue("theme", out var theme);

            switch (theme as string)
            {
                case "Light":
                    this.LightRadioButton.IsChecked = true;
                    break;

                case "Dark":
                    this.DarkRadioButton.IsChecked = true;
                    break;

                default:
                    this.DefaultRadioButton.IsChecked = true;
                    break;
            }
        }

        private ApplicationData ApplicationData => Windows.Storage.ApplicationData.Current;

        private void LightRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            this.ApplicationData.LocalSettings.Values["theme"] = "Light";
        }

        private void DarkRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            this.ApplicationData.LocalSettings.Values["theme"] = "Dark";
        }

        private void DefaultRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            this.ApplicationData.LocalSettings.Values["theme"] = "Default";
        }
    }
}