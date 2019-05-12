namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.Storage.Search;
    using Windows.Storage.Streams;
    using ZipPicViewUWP.Utility;

    internal class FileSystemMediaProvider : AbstractMediaProvider
    {
        private StorageFolder folder;

        public FileSystemMediaProvider(StorageFolder folder)
        {
            this.folder = folder;
            this.FileFilter = new PhysicalFileFilter();
        }

        public override async Task<(Stream stream, string suggestedFileName, Exception error)> OpenEntryAsync(string entry)
        {
            return (await this.folder.OpenStreamForReadAsync(entry), entry.ExtractFilename(), null);
        }

        public override async Task<(string[], Exception error)> GetChildEntries(string entry)
        {
            try
            {
                var subFolder = (entry == this.Root) ? this.folder : await this.folder.GetFolderAsync(entry);

                var files = await subFolder.GetFilesAsync();

                var output = new List<string>(files.Count);

                var startIndex = this.folder.Path.Length == 3 ?
                    this.folder.Path.Length :
                    this.folder.Path.Length + 1;

                foreach (var path in
                    from f in files
                    where this.FilterImageFileType(f.Name)
                    select f.Path)
                {
                    output.Add(path.Substring(startIndex));
                }

                return (output.ToArray(), null);
            }
            catch (Exception e)
            {
                return (null, e);
            }
        }

        public override async Task<(string[], Exception error)> GetFolderEntries()
        {
            try
            {
                var options = new QueryOptions(CommonFolderQuery.DefaultQuery)
                {
                    FolderDepth = FolderDepth.Deep,
                };

                var subFolders = await this.folder.CreateFolderQueryWithOptions(options).GetFoldersAsync();

                var output = new List<string>(subFolders.Count) { this.Root };

                var startIndex = (this.folder.Path.Length == 3) ?
                    this.folder.Path.Length :
                    this.folder.Path.Length + 1;

                foreach (var folder in subFolders)
                {
                    output.Add(folder.Path.Substring(startIndex));
                }

                return (output.ToArray(), null);
            }
            catch (Exception e)
            {
                return (null, e);
            }
        }

        public override async Task<(IRandomAccessStream, Exception error)> OpenEntryAsRandomAccessStreamAsync(string entry)
        {
            var (results, suggested, error) = await this.OpenEntryAsync(entry);
            if (error != null)
            {
                return (null, error);
            }

            return (results.AsRandomAccessStream(), null);
        }
    }
}