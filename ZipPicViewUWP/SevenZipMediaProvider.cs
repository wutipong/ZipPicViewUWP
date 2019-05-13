// <copyright file="SevenZipMediaProvider.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using SharpCompress.Archives;

    /// <summary>
    /// Media provider class for 7zip file source.
    /// </summary>
    internal class SevenZipMediaProvider : ArchiveMediaProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SevenZipMediaProvider"/> class.
        /// </summary>
        /// <param name="stream">Stream to read media from.</param>
        /// <param name="archive">Archive to read media from.</param>
        public SevenZipMediaProvider(Stream stream, IArchive archive)
            : base(stream, archive)
        {
            this.Separator = '/';
        }

        /// <inheritdoc/>
        protected override string[] CreateFileList()
        {
            var output = new LinkedList<string>();

            if (this.Archive != null)
            {
                lock (this.Archive)
                {
                    foreach (var entry in this.Archive.Entries)
                    {
                        output.AddLast(entry.Key);
                    }
                }
            }

            return output.ToArray();
        }

        /// <inheritdoc/>
        protected override string[] CreateFolderList()
        {
            var folders = new HashSet<string>() { this.Root };

            foreach (var entry in this.FileList)
            {
                var parts = entry.Split(this.Separator);

                if (parts.Length == 1)
                {
                    continue;
                }

                string path = parts[0];
                folders.Add(path);

                for (int i = 1; i < parts.Length - 1; i++)
                {
                    path += this.Separator + parts[i];
                    folders.Add(path);
                }
            }

            return folders.ToArray();
        }
    }
}