// <copyright file="FileSystemMediaProvider.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP.MediaProvider
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
    public class FileSystemMediaProvider : AbstractMediaProvider
    {
        private readonly StorageFolder folder;

        private Dictionary<string, IEnumerable<string>> folderFileEntries = new Dictionary<string, IEnumerable<string>>();

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
        public override async Task<Stream> OpenEntryAsync(string entry)
        {
            return await this.folder.OpenStreamForReadAsync(entry);
        }

        /// <inheritdoc/>
        public override async Task<IEnumerable<string>> GetChildEntries(string entry)
        {
            if (this.folderFileEntries.ContainsKey(entry))
            {
                return this.folderFileEntries[entry];
            }

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

            this.folderFileEntries[entry] = output;
            return this.folderFileEntries[entry];
        }

        /// <inheritdoc/>
        public override async Task<IRandomAccessStream> OpenEntryAsRandomAccessStreamAsync(string entry)
        {
            var results = await this.OpenEntryAsync(entry);
            return results.AsRandomAccessStream();
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<string>> DoGetFolderEntries()
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

            foreach (var subFolder in subFolders)
            {
                output.Add(subFolder.Path.Substring(startIndex));
            }

            await this.CreateFileList(output);

            return output;
        }

        private async Task CreateFileList(IEnumerable<string> folderEntries)
        {
            var options = new QueryOptions(CommonFolderQuery.DefaultQuery)
            {
                FolderDepth = FolderDepth.Deep,
            };

            var files = await this.folder.CreateFileQueryWithOptions(options).GetFilesAsync();

            foreach (var file in files)
            {
                var path = file.Path;
                if (!this.FileFilter.IsImageFile(path))
                {
                    continue;
                }

                // relative path without the prefix separator.
                var relativePath = path.Substring(this.folder.Path.Length + 1);
                var lastSeparatorIndex = relativePath.LastIndexOf(this.Separator);

                var parentPath = lastSeparatorIndex  == -1? 
                    this.Root:
                    relativePath.Substring(0, lastSeparatorIndex) ;

                var fileList = folderFileEntries.GetValueOrDefault(parentPath, new List<string>()) as List<string>;
                
                fileList?.Add(relativePath);
                this.folderFileEntries[parentPath] = fileList;
            }

        }
    }
}