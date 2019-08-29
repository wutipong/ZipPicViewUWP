using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using ZipPicViewUWP.MediaProvider;

namespace MangaReader
{
    internal static class DBManager
    {
        private static SemaphoreSlim Semaphore { get; set; } = new SemaphoreSlim(1, 1);

        public static StorageFolder Folder { get; set; }

        public static async Task<DBAccess> GetDBAccess()
        {
            DBAccess access = new DBAccess();

            var dbFile = await Folder.CreateFileAsync(@"manga.db", CreationCollisionOption.OpenIfExists);
            access.Stream = await dbFile.OpenAsync(FileAccessMode.ReadWrite);
            access.DB = new LiteDatabase(access.Stream.AsStream());

            return access;
        }

        /*
        public static async Task SetRating(string name, int rating)
        {
            Semaphore.Wait();
            try
            {
                using (var access = await GetDBAccess())
                {
                    var col = access.DB.GetCollection<MangaData>();
                    var row = col.FindOne(r => r.Name == name);
                    if (row != null)
                    {
                        row.Rating = rating;
                    }

                    col.Update(row);
                }
            }
            finally
            {
                Semaphore.Release();
            }
        } */

        public static async Task<MangaData> GetData(string name)
        {
            Semaphore.Wait();
            try
            {
                using (var access = await GetDBAccess())
                {
                    var col = access.DB.GetCollection<MangaData>();
                    var row = col.FindOne(r => r.Name == name);
                    if (row != null)
                    {
                        return row;
                    }
                }
            }
            finally
            {
                Semaphore.Release();
            }
            return null;
        }

        public static async Task UpdateData(MangaData data)
        {
            Semaphore.Wait();
            try
            {
                using (var access = await GetDBAccess())
                {
                    var col = access.DB.GetCollection<MangaData>();
                    col.Update(data);
                }
            }
            finally
            {
                Semaphore.Release();
            }
        }

        private static async Task<AbstractMediaProvider> OpenFile(StorageFile selected)
        {
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

        public static async Task RefreshMangaData()
        {
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

            var results = Folder.CreateFileQueryWithOptions(queryOptions);
            var fileList = await results.GetFilesAsync();

            Semaphore.Wait();
            try
            {
                using (var access = await GetDBAccess())
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
                            DateCreated = f.DateCreated.DateTime
                        };

                        var provider = await OpenFile(f);
                        if (provider == null)
                        {
                            continue;
                        }

                        var (files, error) = await provider.GetAllFileEntries();
                        if (error != null)
                        {
                            continue;
                        }

                        string id;

                        var outputStream = new InMemoryRandomAccessStream();
                        error = await CreateThumbnail(f, provider, files, outputStream);
                        if (error != null)
                        {
                            continue;
                        }

                        id = f.Name.GetHashCode().ToString();

                        access.DB.FileStorage.Upload(id, f.Name + ".jpg", outputStream.AsStream());
                        row.ThumbID = id;
                        col.Insert(row);
                    }

                    foreach (var data in col.FindAll())
                    {
                        if (fileList.FirstOrDefault(f => f.Name == data.Name) == null)
                        {
                            access.DB.FileStorage.Delete(data.ThumbID);
                            col.Delete(data.Id);
                        }
                    }
                }
            }
            finally
            {
                Semaphore.Release();
            }
        }

        private static async Task<Exception> CreateThumbnail(StorageFile f, AbstractMediaProvider provider, string[] files, IRandomAccessStream outputStream)
        {
            var coverName = provider.FileFilter.FindCoverPage(files);
            var (stream, error) = await provider.OpenEntryAsRandomAccessStreamAsync(coverName);
            if (error != null)
            {
                return error;
            }
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = ImageHelper.CreateResizedBitmap(decoder, 127, 188);

            var propertySet = new BitmapPropertySet();
            var qualityValue = new BitmapTypedValue(0.5, Windows.Foundation.PropertyType.Single);
            propertySet.Add("ImageQuality", qualityValue);

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outputStream, propertySet);
            encoder.SetSoftwareBitmap(await bitmap);
            await encoder.FlushAsync();

            await outputStream.FlushAsync();
            return error;
        }
    }
}