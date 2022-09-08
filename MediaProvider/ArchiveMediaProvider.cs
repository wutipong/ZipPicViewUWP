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
        private string[] _fileList;
        private readonly Dictionary<string, string[]> _folderFileEntries = new Dictionary<string, string[]>();

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
        protected string[] FileList => this._fileList ?? (this._fileList = this.CreateFileList());

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
        public override Task<(string[], Exception error)> GetChildEntries(string entry)
        {
            return Task.Run<(string[], Exception)>(() =>
            {
                if (this._folderFileEntries.ContainsKey(entry))
                {
                    return (this._folderFileEntries[entry], null);
                }

                try
                {
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

                    this._folderFileEntries[entry] = output.ToArray();
                    return (this._folderFileEntries[entry], null);
                }
                catch (Exception e)
                {
                    return (null, e);
                }
            });
        }

        /// <inheritdoc/>
        public override Task<(Stream stream, Exception error)> OpenEntryAsync(string entry)
        {
            return Task.Run<(Stream, Exception)>(() =>
            {
                var outputStream = new MemoryStream();
                if (this.Archive == null)
                {
                    return (null, new Exception("Cannot Read Archive"));
                }

                try
                {
                    lock (this.Archive)
                    {
                        var archiveEntry = this.Archive.Entries.First(e => e.Key == entry);
                        archiveEntry.WriteTo(outputStream);
                        outputStream.Position = 0;

                        return (outputStream, null);
                    }
                }
                catch (Exception e)
                {
                    return (null, e);
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
        public override async Task<(IRandomAccessStream, Exception error)> OpenEntryAsRandomAccessStreamAsync(string entry)
        {
            try
            {
                var (stream, error) = await this.OpenEntryAsync(entry);
                return (stream.AsRandomAccessStream(), error);
            }
            catch (Exception e)
            {
                return (null, e);
            }
        }

        /// <inheritdoc/>
        protected override async Task<(string[], Exception error)> DoGetFolderEntries()
        {
            return await Task.Run<(string[], Exception)>(() =>
            {
                try
                {
                    return (this.CreateFolderList(), null);
                }
                catch (Exception e)
                {
                    return (null, e);
                }
            });
        }

        /// <summary>
        /// Create a list of folders contained in the archive.
        /// </summary>
        /// <returns>folder list.</returns>
        protected virtual string[] CreateFolderList()
        {
            var output = new List<string>();
            Exception exception = null;
            lock (this.Archive)
            {
                if (this.Archive != null)
                {
                    try
                    {
                        var folderEntries = from entry in this.Archive.Entries
                                            where entry.IsDirectory
                                            orderby entry.Key
                                            select entry.Key;

                        output.Add(this.Root);
                        foreach (var folder in folderEntries)
                        {
                            output.Add(folder.EndsWith(this.Separator) ? folder.Substring(0, folder.Length - 1) : folder);
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
                    catch (Exception err)
                    {
                        exception = err;
                    }
                }
            }

            if (exception != null)
            {
                throw exception;
            }

            return output.ToArray();
        }

        /// <summary>
        /// Create a list of file contained in the archive.
        /// </summary>
        /// <returns>file list.</returns>
        protected virtual string[] CreateFileList()
        {
            var files = new List<string>();
            lock (this.Archive)
            {
                files.AddRange(from e in this.Archive.Entries where !e.IsDirectory select e.Key);
            }

            return files.ToArray();
        }

        private char DetermineSeparator()
        {
            return this.Archive.Entries.Any(entry => entry.Key.Contains('\\')) ? '\\' : '/';
        }
    }
}