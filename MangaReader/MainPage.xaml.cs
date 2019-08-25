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
using LiteDB;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MangaReader
{
    internal struct MangaData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    internal class FolderInfo
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

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

            var dbFile = await folder.CreateFileAsync(@"manga.db", CreationCollisionOption.OpenIfExists);
            using (var stream = await dbFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var db = new LiteDatabase(stream.AsStream()))
                {
                    var infoCol = db.GetCollection<FolderInfo>();
                    var info = infoCol.FindOne(Query.All());

                    var properties = await folder.GetBasicPropertiesAsync();
                    if (info == null || info.ModifiedDate != properties.DateModified.DateTime)
                    {
                        info = new FolderInfo
                        {
                            Path = folder.Path,
                            ModifiedDate = properties.DateModified.DateTime,
                        };

                        infoCol.Delete(Query.All());
                        infoCol.Insert(info);
                    }

                    var col = db.GetCollection<MangaData>("files");
                    foreach (var f in fileList)
                    {
                        var data = new MangaData()
                        {
                            Name = f.Name,
                        };
                        col.Insert(data);
                    }
                }
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(SettingPage));
        }
    }
}