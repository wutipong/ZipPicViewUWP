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
using ZipPicViewUWP.MediaProvider;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MangaReader
{
    internal class MangaData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Rating { get; set; } = -1;
        public string ThumbID { get; set; }

        public DateTime DateCreated { get; set; }
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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (await GetLibraryFolder() == null)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(SettingPage));

                return;
            }

            DBManager.Folder = await GetLibraryFolder();

            await DBManager.RefreshMangaData();
            await RefreshMangaData();

            ItemGrid.ItemClick += ItemGrid_ItemClick;
        }

        private void ItemGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as Thumbnail;

            this.ApplicationData.LocalSettings.Values["selected file"] = item.TitleText;
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(ViewerPage));

            return;
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(SettingPage));
        }

        private async Task RefreshMangaData()
        {
            if (ItemGrid == null)
                return;

            ItemGrid.Items.Clear();

            using (var access = await DBManager.GetDBAccess())
            {
                var col = access.DB.GetCollection<MangaData>();
                var list =
                    SortByDropDown.SelectedIndex == 0 ? col.FindAll().OrderBy((m) => m.Name) :
                    SortByDropDown.SelectedIndex == 1 ? col.FindAll().OrderBy((m) => m.DateCreated) :
                    SortByDropDown.SelectedIndex == 2 ? col.FindAll().OrderByDescending((m) => m.DateCreated) :
                    col.FindAll().OrderByDescending(m => m.Rating);

                foreach (var data in list)
                {
                    SoftwareBitmapSource source = new SoftwareBitmapSource();
                    using (var irs = new InMemoryRandomAccessStream())
                    {
                        var stream = irs.AsStream();

                        var id = data.ThumbID;
                        access.DB.FileStorage.Download(id, stream);
                        stream.Flush();

                        irs.Seek(0);

                        if (irs.Size > 0)
                        {
                            var decoder = await BitmapDecoder.CreateAsync(irs);

                            var bitmap = await decoder.GetSoftwareBitmapAsync();
                            bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                            await source.SetBitmapAsync(bitmap);
                        }
                    }

                    var thumbnail = new Thumbnail()
                    {
                        TitleText = data.Name,
                        Rating = data.Rating,
                        Source = source,
                    };

                    ItemGrid.Items.Add(thumbnail);
                }
            }
        }

        private async void Thumbnail_RatingChanged(Thumbnail sender, object args)
        {
            await DBManager.SetRating(sender.TitleText, sender.Rating);
        }

        private async Task<StorageFolder> GetLibraryFolder()
        {
            try
            {
                this.ApplicationData.LocalSettings.Values.TryGetValue("path token", out var token);
                var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token as string);

                return folder;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private async void SortByDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await RefreshMangaData();
        }
    }
}