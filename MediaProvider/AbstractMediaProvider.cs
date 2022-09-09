// <copyright file="AbstractMediaProvider.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP.MediaProvider
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NaturalSort.Extension;
    using Windows.Storage.Streams;
    using ZipPicViewUWP.Utility;

    /// <summary>
    /// Abstract base class for media providers. Media provider is an object that provides images to the viewer.
    /// Subclass of this class implements how these image files are read.
    /// </summary>
    public abstract class AbstractMediaProvider : IDisposable
    {
        private IEnumerable<string> folderEntries;

        /// <summary>
        /// Gets or sets the associated file filter to be used with this provider.
        /// </summary>
        public FileFilter FileFilter { get; protected set; }

        /// <summary>
        /// Gets the root directory name.
        /// </summary>
        public string Root => @"\";

        /// <summary>
        /// Gets or sets the path separator.
        /// </summary>
        public char Separator
        {
            get; protected set;
        }

        /// <summary>
        /// Dispose the resource after the object is no longer being used.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Determine if the file is an image file.
        /// </summary>
        /// <param name="entryName">entry name.</param>
        /// <returns><c>true</c> if it is an image file, <c>false</c> otherwise.</returns>
        public bool FilterImageFileType(string entryName)
        {
            return this.FileFilter?.IsImageFile(entryName) ?? false;
        }

        /// <summary>
        /// Get the list of all file entries under this provider.
        /// </summary>
        /// <returns>List of child entries.</returns>
        public virtual async Task<IEnumerable<string>> GetAllFileEntries()
        {
            var folders = await this.GetFolderEntries();
            var output = new List<string>();
            foreach (var folder in folders)
            {
                var files = await this.GetChildEntries(folder);
                var sorted = files.OrderBy(s=>s, StringComparer.OrdinalIgnoreCase.WithNaturalSort());
                
                output.AddRange(sorted);
            }

            return output;
        }

        /// <summary>
        /// Get the list of child entries of the input entry.
        /// </summary>
        /// <param name="entry">parent entry.</param>
        /// <returns>List of child entries.</returns>
        public abstract Task<IEnumerable<string>> GetChildEntries(string entry);

        /// <summary>
        /// Get the parent of the input entry.
        /// </summary>
        /// <param name="entry">child entry.</param>
        /// <returns>Parent entry.</returns>
        public virtual string GetParentEntry(string entry)
        {
            var lastSeparator = entry.LastIndexOf(this.Separator);
            if (lastSeparator == -1)
            {
                return this.Root;
            }

            if (lastSeparator == entry.Length - 1)
            {
                lastSeparator = entry.LastIndexOf(this.Separator, 0, lastSeparator);
            }

            return entry.Substring(0, lastSeparator);
        }

        /// <summary>
        /// Get the list of folder entries in this media provider.
        /// </summary>
        /// <returns>Folder entries.</returns>
        public async Task<IEnumerable<string>> GetFolderEntries()
        {
            if (this.folderEntries != null)
            {
                return this.folderEntries;
            }

            this.folderEntries = await this.DoGetFolderEntries();

            var comparer = StringComparer.InvariantCultureIgnoreCase.WithNaturalSort();

            this.folderEntries = folderEntries.OrderBy(s=>s, Comparer<string>.Create((string s1, string s2) =>
            {
                if (s1 == this.Root)
                {
                    return -1;
                }

                if (s2 == this.Root)
                {
                    return 1;
                }
                
                return comparer.Compare(s1, s2);
                
            }));

            return this.folderEntries;
        }

        /// <summary>
        /// Read the entry and return as an <c>RandomAccessStream</c>.
        /// </summary>
        /// <param name="entry">entry to read from.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public abstract Task<IRandomAccessStream> OpenEntryAsRandomAccessStreamAsync(string entry);

        /// <summary>
        /// Read the entry and return as an <c>Stream</c>.
        /// </summary>
        /// <param name="entry">entry to read from.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public abstract Task<Stream> OpenEntryAsync(string entry);

        /// <summary>
        /// Suggest the filename to be saved.
        /// </summary>
        /// <param name="entry">Entry".</param>
        /// <returns>A file name.</returns>
        public virtual string SuggestFileNameToSave(string entry)
        {
            return entry.ExtractFilename();
        }

        /// <summary>
        /// Get the list of folder entries in this media provider.
        /// </summary>
        /// <returns>Folder entries.</returns>
        protected abstract Task<IEnumerable<string>> DoGetFolderEntries();
    }
}