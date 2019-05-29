// <copyright file="FileSystemMediaProvider.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

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

    /// <summary>
    /// Media provider class for reading media from directory.
    /// </summary>
    internal class FileSystemMediaProvider : AbstractMediaProvider
    {
        private readonly StorageFolder folder;
        private string[] folderEntries = null;
        private Dictionary<string, string[]> fileEntries = new Dictionary<string, string[]>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemMediaProvider"/> class.
        /// </summary>
        /// <param name="folder">Folder to read media from.</param>
        public FileSystemMediaProvider(StorageFolder folder)
        {
            this.folder = folder;
            this.FileFilter = new PhysicalFileFilter();
            this.Separator = Path.DirectorySeparatorChar;
        }

        /// <inheritdoc/>
        public override async Task<(Stream stream, string suggestedFileName, Exception error)> OpenEntryAsync(string entry)
        {
            try
            {
                return (await this.folder.OpenStreamForReadAsync(entry), entry.ExtractFilename(), null);
            }
            catch (Exception err)
            {
                return (null, string.Empty, err);
            }
        }

        /// <inheritdoc/>
        public override async Task<(string[], Exception error)> GetChildEntries(string entry)
        {
            var folderIndex = Array.IndexOf(this.folderEntries, entry);
            if (this.fileEntries.ContainsKey(entry))
            {
                return (this.fileEntries[entry], null);
            }

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

                this.fileEntries[entry] = output.ToArray();
                return (this.fileEntries[entry], null);
            }
            catch (Exception e)
            {
                return (null, e);
            }
        }

        /// <inheritdoc/>
        public override async Task<(IRandomAccessStream, Exception error)> OpenEntryAsRandomAccessStreamAsync(string entry)
        {
            var (results, _, error) = await this.OpenEntryAsync(entry);
            if (error != null)
            {
                return (null, error);
            }

            return (results.AsRandomAccessStream(), null);
        }

        /// <inheritdoc/>
        protected override async Task<(string[], Exception error)> DoGetFolderEntries()
        {
            if (this.folderEntries != null)
            {
                return (this.folderEntries, null);
            }

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

                this.folderEntries = output.ToArray();

                return (this.folderEntries, null);
            }
            catch (Exception e)
            {
                return (null, e);
            }
        }
    }
}