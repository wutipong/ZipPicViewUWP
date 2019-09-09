using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MangaReader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        public StartPage()
        {
            this.InitializeComponent();
        }

        private ApplicationData ApplicationData => Windows.Storage.ApplicationData.Current;

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();

            if (folder == null)
                return;

            this.ApplicationData.LocalSettings.Values.TryGetValue("path token", out var token);

            if (token == null)
            {
                token = StorageApplicationPermissions.FutureAccessList.Add(folder);
                this.ApplicationData.LocalSettings.Values["path token"] = token;
            }
            else
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(token as string, folder);
            }

            this.ApplicationData.LocalSettings.Values["path"] = folder.Path;

            DBManager.Release();

            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage));
        }
    }
}