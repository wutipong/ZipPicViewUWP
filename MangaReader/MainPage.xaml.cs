using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.AccessCache;
using Windows.Storage;
using Windows.Storage.Search;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MangaReader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ApplicationData ApplicationData => Windows.Storage.ApplicationData.Current;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.ApplicationData.LocalSettings.Values.TryGetValue("path token", out var token);
            if (token == null)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(SettingPage));

                return;
            }

            var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token as string);

            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".zip");
            fileTypeFilter.Add(".rar");
            fileTypeFilter.Add(".pdf");
            fileTypeFilter.Add(".7z");
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter)
            {
                FolderDepth = FolderDepth.Shallow,
            };

            var results = folder.CreateFileQueryWithOptions(queryOptions);
            var fileList = await results.GetFilesAsync();

            foreach (var f in fileList)
            {
                ItemGrid.Items.Add(new Thumbnail()
                {
                    TitleText = f.Name
                }
                );
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(SettingPage));
        }
    }
}