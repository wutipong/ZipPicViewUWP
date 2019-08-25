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

        private async Task<AbstractMediaProvider> OpenFile(StorageFile selected)
        {
            if (selected == null)
            {
                this.IsEnabled = true;
                return null;
            }

            if (selected.FileType == ".pdf")
            {
                var provider = new PdfMediaProvider(selected);
                await provider.Load();

                return provider;
            }
            else
            {
                Stream stream = null;
                try
                {
                    stream = await selected.OpenStreamForReadAsync();

                    var archive = ArchiveMediaProvider.TryOpenArchive(stream, null, out bool isEncrypted);
                    if (isEncrypted)
                    {
                        return null;
                    }

                    return ArchiveMediaProvider.Create(stream, archive);
                }
                catch (Exception err)
                {
                    return null;
                }
            }
        }

        private async void RefreshMangaData()
        {
            StorageFolder folder = await GetLibraryFolder();

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
                var col = access.DB.GetCollection<MangaData>();
                foreach (var f in fileList)
                {
                    var row = col.FindOne(r => r.Name == f.Name);
                    if (row != null)
                        continue;

                    row = new MangaData()
                    {
                        Name = f.Name,
                    };

                    var provider = await OpenFile(f);

                    var (files, error) = await provider.GetAllFileEntries();
                    if (error != null)
                        continue;

                    var coverName = provider.FileFilter.FindCoverPage(files);
                    IRandomAccessStream stream;
                    (stream, error) = await provider.OpenEntryAsRandomAccessStreamAsync(coverName);

                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    var bitmap = ImageHelper.CreateResizedBitmap(decoder, 127, 188);
                    var outputIrs = new InMemoryRandomAccessStream();

                    var propertySet = new BitmapPropertySet();
                    var qualityValue = new BitmapTypedValue(0.5, Windows.Foundation.PropertyType.Single);
                    propertySet.Add("ImageQuality", qualityValue);

                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outputIrs, propertySet);
                    encoder.SetSoftwareBitmap(await bitmap);
                    await encoder.FlushAsync();

                    var id = f.Name.GetHashCode().ToString();
                    var outputStream = outputIrs.AsStream();
                    outputStream.Flush();

                    access.DB.FileStorage.Upload(id, f.Name + ".jpg", outputStream);
                    row.ThumbID = id;
                    col.Insert(row);
                }

                foreach (var data in col.FindAll())
                {
                    if (fileList.FirstOrDefault(f => f.Name == data.Name) == null)
                    {
                        col.Delete(data.Id);
                        continue;
                    }

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

                    thumbnail.RatingChanged += Thumbnail_RatingChanged;

                    ItemGrid.Items.Add(thumbnail);
                }
            }
        }

        private async void Thumbnail_RatingChanged(Thumbnail sender, object args)
        {
            StorageFolder folder = await GetLibraryFolder();

            using (var access = await GetDBAccess(folder))
            {
                var col = access.DB.GetCollection<MangaData>();
                var row = col.FindOne(r => r.Name == sender.TitleText);
                if (row != null)
                {
                    row.Rating = sender.Rating;
                }

                col.Update(row);
            }
        }

        private async Task<StorageFolder> GetLibraryFolder()
        {
            this.ApplicationData.LocalSettings.Values.TryGetValue("path token", out var token);
            var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token as string);
            return folder;
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