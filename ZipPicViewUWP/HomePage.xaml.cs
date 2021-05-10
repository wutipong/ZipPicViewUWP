// <copyright file="HomePage.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.ApplicationModel;
    using Windows.Foundation;
    using Windows.Foundation.Collections;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Data;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HomePage"/> class.
        /// </summary>
        public HomePage()
        {
            this.InitializeComponent();
            Package package = Package.Current;

            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            this.VersionText.Text = (package.IsDevelopmentMode ? "(Debug)" : string.Empty) +

                string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        /// <summary>
        /// Event handler when open file button is clicked.
        /// </summary>
        public event RoutedEventHandler OpenFileClick;

        /// <summary>
        /// Event handler when open folder button is clicked.
        /// </summary>
        public event RoutedEventHandler OpenFolderClick;

        private void AdaptiveGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem == this.OpenFile)
            {
                this.OpenFileClick?.Invoke(this, e);
            } 
            else if (e.ClickedItem == this.OpenFolder)
            {
                this.OpenFolderClick?.Invoke(this, e);
            }
        }
    }
}