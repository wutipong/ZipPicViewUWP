// <copyright file="ArchiveMediaProvider.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP.MediaProvider

{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using SharpCompress.Archives;
    using SharpCompress.Readers;
    using Windows.Storage.Streams;

    /// <summary>
    /// Media provider class for archive file source.
    /// </summary>
    public class ArchiveMediaProvider : AbstractMediaProvider
    {
        private IEnumerable<string> _fileList;
        private readonly Dictionary<string, IEnumerable<string>> _folderFileEntries = new Dictionary<string, IEnumerable<string>>();

        private Stream _stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveMediaProvider"/> class.
        /// </summary>
        /// <param name="stream">stream of the open file.</param>
        /// <param name="archive">archive instant of the file.</param>
        public ArchiveMediaProvider(Stream stream, IArchive archive)
        {
            this.Archive = archive;
            this._stream = stream;

            this.Separator = this.DetermineSeparator();
            this.FileFilter = new PhysicalFileFilter();
        }

        /// <summary>
        /// Gets or sets archive that media files are read from.
        /// </summary>
        protected IArchive Archive { get; set; }

        /// <summary>
        /// Gets the list of file inside this archive.
        /// </summary>
        protected IEnumerable<string> FileList => this._fileList ?? (this._fileList = this.CreateFileList());

        /// <summary>
        /// Try to open the archive using supplied password.
        /// </summary>
        /// <param name="stream">The archive stream.</param>
        /// <param name="password">password of the file.</param>
        /// <param name="isEncrypted">will be assigned whether or not the file is encrypted.</param>
        /// <returns>the archive instance.</returns>
        public static IArchive TryOpenArchive(Stream stream, string password, out bool isEncrypted)
        {
            isEncrypted = false;
            var options = new ReaderOptions
            {
                Password = password,
            };

            var archive = ArchiveFactory.Open(stream, options);

            var entry = archive.Entries.FirstOrDefault(e => !e.IsDirectory);

            if (entry == null) return archive;

            isEncrypted = entry.IsEncrypted;
            if (isEncrypted && password == null)
            {
                return null;
            }

            // Try open a stream to see if it can be opened
            using (entry.OpenEntryStream())
            {
            }

            return archive;
        }

        /// <summary>
        /// Create an instance of suitable media provider based on given archive type.
        /// </summary>
        /// <param name="stream">stream of the archive file.</param>
        /// <param name="archive">archive instance.</param>
        /// <returns>media provider of the given archive.</returns>
        public static ArchiveMediaProvider Create(Stream stream, IArchive archive)
        {
            return new ArchiveMediaProvider(stream, archive);
        }

        /// <inheritdoc/>
        public override async Task<IEnumerable<string>> GetChildEntries(string entry)
        {
            return await Task.Run(() =>
            {
                if (this._folderFileEntries.ContainsKey(entry))
                {
                    return this._folderFileEntries[entry];
                }

                var output = new LinkedList<string>();
                var folder = entry == this.Root ? string.Empty : entry;

                foreach (var file in this.FileList)
                {
                    if (!file.StartsWith(folder))
                    {
                        continue;
                    }

                    var innerKey = file.Substring(folder.Length + 1);
                    if (innerKey.Contains(this.Separator))
                    {
                        continue;
                    }

                    if (this.FilterImageFileType(innerKey))
                    {
                        output.AddLast(file);
                    }
                }

                this._folderFileEntries[entry] = output;
                return this._folderFileEntries[entry];
            });
        }

        /// <inheritdoc/>
        public override Task<Stream> OpenEntryAsync(string entry)
        {
            return Task.Run<Stream>(() =>
            {
                if (this.Archive == null)
                {
                    throw new IOException("Cannot open the archive file.");
                }

                lock (this.Archive)
                {
                    try
                    {
                        var outputStream = new MemoryStream();
                        var archiveEntry = this.Archive.Entries.First(e => e.Key == entry);
                        archiveEntry.WriteTo(outputStream);
                        outputStream.Position = 0;

                        return outputStream;
                    }
                    catch (Exception err)
                    {
                        throw new InvalidOperationException("Unable to read the entry", err);
                    }
                }
            });
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            lock (this.Archive)
            {
                this.Archive.Dispose();
                this.Archive = null;
            }

            lock (this._stream)
            {
                this._stream.Dispose();
                this._stream = null;
            }
        }

        /// <inheritdoc/>
        public override async Task<IRandomAccessStream> OpenEntryAsRandomAccessStreamAsync(string entry)
        {
            var stream = await this.OpenEntryAsync(entry);
            return stream.AsRandomAccessStream();
        }

        /// <inheritdoc/>
        protected override Task<IEnumerable<string>> DoGetFolderEntries()
        {
            return Task.FromResult(this.CreateFolderList().AsEnumerable());
        }

        /// <summary>
        /// Create a list of folders contained in the archive.
        /// </summary>
        /// <returns>folder list.</returns>
        protected virtual IEnumerable<string> CreateFolderList()
        {
            var output = new List<string>();
            lock (this.Archive)
            {
                if (this.Archive == null) 
                { 
                    return Array.Empty<string>(); 
                }

                try
                {
                    var folderEntries = from entry in this.Archive.Entries
                        where entry.IsDirectory
                        orderby entry.Key
                        select entry.Key;

                    output.Add(this.Root);
                    output.AddRange(folderEntries.Select(folder => folder.EndsWith(this.Separator) ? folder.Substring(0, folder.Length - 1) : folder));
                }
                catch (Exception err)
                {
                    throw new InvalidOperationException("Unable to create folder list.", err);
                }

                foreach (var entry in this.FileList)
                {
                    var separatorIndex = entry.LastIndexOf(this.Separator);
                    if (separatorIndex < 0)
                    {
                        continue;
                    }

                    var parent = entry.Substring(0, separatorIndex);

                    if (!output.Contains(parent))
                    {
                        output.Add(parent);
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// Create a list of file contained in the archive.
        /// </summary>
        /// <returns>file list.</returns>
        protected virtual IEnumerable<string> CreateFileList()
        {
            var files = new List<string>();
            lock (this.Archive)
            {
                try
                {
                    files.AddRange(
                        from e in this.Archive.Entries
                        where !e.IsDirectory
                        select e.Key);
                }
                catch (Exception err)
                {
                    throw new InvalidOperationException("Unable to create file list.", err);
                }
            }

            return files;
        }

        private char DetermineSeparator()
        {
            try
            {
                return this.Archive.Entries.Any(entry => entry.Key.Contains('\\')) ? '\\' : '/';
            }
            catch (Exception err)
            {
                throw new InvalidOperationException("Unable to determine the path separator.", err);

            }
        }
    }
}