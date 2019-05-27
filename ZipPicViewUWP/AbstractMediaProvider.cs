﻿// <copyright file="AbstractMediaProvider.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NaturalSort.Extension;
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
        public virtual async Task<(string[], Exception error)> GetAllFileEntries()
        {
            var (folders, error) = await this.GetFolderEntries();
            if (error != null)
            {
                return (null, error);
            }

            var output = new List<string>();
            foreach (var folder in folders)
            {
                var (files, errorFile) = await this.GetChildEntries(folder);
                if (errorFile != null)
                {
                    return (null, errorFile);
                }

                Array.Sort(files, StringComparer.OrdinalIgnoreCase.WithNaturalSort());

                output.AddRange(files);
            }

            return (output.ToArray(), null);
        }

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