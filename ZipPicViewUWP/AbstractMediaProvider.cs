// <copyright file="AbstractMediaProvider.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage.Streams;

    /// <summary>
    /// Abstract base class for media providers. Media provider is an object that provides images to the viewer.
    /// Subclass of this class implements how these image files are read.
    /// </summary>
    public abstract class AbstractMediaProvider : IDisposable
    {
        /// <summary>
        /// Gets or sets the associated file filter to be used with this provider.
        /// </summary>
        public FileFilter FileFilter { get; protected set; }

        /// <summary>
        /// Gets the root directory name.
        /// </summary>
        protected string Root => @"\";

        /// <summary>
        /// Gets or sets the path separator.
        /// </summary>
        protected char Separator
        {
            get; set;
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
            if (this.FileFilter == null)
            {
                return true;
            }

            return this.FileFilter.IsImageFile(entryName);
        }

        /// <summary>
        /// Get the list of all file entries under this provider.
        /// </summary>
        /// <returns>List of child entries.</returns>
        public abstract Task<(string[], Exception error)> GetAllFileEntries();

        /// <summary>
        /// Get the list of child entries of the input entry.
        /// </summary>
        /// <param name="entry">parent entry.</param>
        /// <returns>List of child entries.</returns>
        public abstract Task<(string[], Exception error)> GetChildEntries(string entry);

        /// <summary>
        /// Get the parent of the input entry.
        /// </summary>
        /// <param name="entry">child entry.</param>
        /// <returns>Parent entry.</returns>
        public abstract Task<(string, Exception error)> GetParentEntry(string entry);

        /// <summary>
        /// Get the list of folder entries in this media provider.
        /// </summary>
        /// <returns>Folder entries.</returns>
        public abstract Task<(string[], Exception error)> GetFolderEntries();

        /// <summary>
        /// Read the entry and return as an <c>RandomAccessStream</c>.
        /// </summary>
        /// <param name="entry">entry to read from.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public abstract Task<(IRandomAccessStream, Exception error)> OpenEntryAsRandomAccessStreamAsync(string entry);

        /// <summary>
        /// Read the entry and return as an <c>Stream</c>.
        /// </summary>
        /// <param name="entry">entry to read from.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public abstract Task<(Stream stream, string suggestedFileName, Exception error)> OpenEntryAsync(string entry);
    }
}