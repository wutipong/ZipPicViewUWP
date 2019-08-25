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
using Windows.Storage.Streams;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MangaReader
{
    internal class MangaData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Rating { get; set; } = -1;
    }

    internal class DBAccess : IDisposable
    {
        public LiteDatabase DB { get; internal set; }
        internal IRandomAccessStream Stream { get; set; }

        public void Dispose()
        {
            DB.Dispose();
            Stream.Dispose();
        }
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.ApplicationData.LocalSettings.Values.TryGetValue("path token", out var token);
            if (token == null)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(SettingPage));

                return;
            }

            RefreshMangaData();
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(SettingPage));
        }

        private async void RefreshMangaData()
        {
            this.ApplicationData.LocalSettings.Values.TryGetValue("path token", out var token);
            var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token as string);

            List<string> fileTypeFilter = new List<string>
            {
                ".cbz",
                ".cbr",
                ".zip",
                ".rar",
                ".pdf",
                ".7z"
            };
            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter)
            {
                FolderDepth = FolderDepth.Shallow,
            };

            var results = folder.CreateFileQueryWithOptions(queryOptions);
            var fileList = await results.GetFilesAsync();

            using (var access = await GetDBAccess(folder))
            {
                var col = access.DB.GetCollection<MangaData>("files");
                foreach (var f in fileList)
                {
                    var row = col.FindOne(r => r.Name == f.Name);
                    if (row == null)
                    {
                        row = new MangaData()
                        {
                            Name = f.Name,
                        };
                        col.Insert(row);
                    }
                }

                foreach (var data in col.FindAll())
                {
                    if (fileList.FirstOrDefault(f => f.Name == data.Name) == null)
                    {
                        col.Delete(data.Id);
                        continue;
                    }

                    ItemGrid.Items.Add(new Thumbnail()
                    {
                        TitleText = data.Name,
                        Rating = data.Rating,
                    });
                }
            }
        }

        private async Task<DBAccess> GetDBAccess(StorageFolder folder)
        {
            DBAccess access = new DBAccess();

            var dbFile = await folder.CreateFileAsync(@"manga.db", CreationCollisionOption.OpenIfExists);
            access.Stream = await dbFile.OpenAsync(FileAccessMode.ReadWrite);
            access.DB = new LiteDatabase(access.Stream.AsStream());

            return access;
        }
    }
}