// <copyright file="AboutDialog.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using Windows.ApplicationModel;
    using Windows.Storage;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// A dialog class to display information about the application.
    /// </summary>
    public sealed partial class AboutDialog : ContentDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AboutDialog"/> class.
        /// </summary>
        public AboutDialog()
        {
            this.InitializeComponent();

            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            this.Version.Text = (package.IsDevelopmentMode ? "(Debug)" : string.Empty) +
                                $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            this.Initialize();
        }

        /// <summary>
        /// Initialize controls.
        /// </summary>
        public async void Initialize()
        {
            this.ReleaseNote.Text =
                await FileIO.ReadTextAsync(await Package.Current.InstalledLocation.GetFileAsync(@"Assets\Release.md"));
        }
    }
}