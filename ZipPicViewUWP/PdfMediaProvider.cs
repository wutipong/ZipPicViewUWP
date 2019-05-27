// <copyright file="PdfMediaProvider.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Data.Pdf;
    using Windows.Graphics.Imaging;
    using Windows.Storage;
    using Windows.Storage.Streams;

    /// <summary>
    /// A media provider based on PDF file.
    /// </summary>
    internal class PdfMediaProvider : AbstractMediaProvider
    {
        private readonly StorageFile file;
        private PdfDocument pdfDocument;
        private uint pageCount = 0;
        private string[] pages;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfMediaProvider"/> class.
        /// </summary>
        /// <param name="file">PDF file.</param>
        public PdfMediaProvider(StorageFile file)
        {
            this.file = file;
            this.FileFilter = new PdfFileFIlter();
        }

        /// <summary>
        /// Read the pdf document from file.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task Load()
        {
            this.pdfDocument = await PdfDocument.LoadFromFileAsync(this.file);
            this.pageCount = this.pdfDocument.PageCount;
        }

        /// <inheritdoc/>
        public override async Task<(string[], Exception error)> GetChildEntries(string entry)
        {
            return await Task.Run<(string[], Exception)>(() =>
            {
                this.pages = new string[this.pageCount];
                for (uint i = 0; i < this.pageCount; i++)
                {
                    this.pages[i] = i.ToString();
                }

                return (this.pages, null);
            });
        }

        /// <inheritdoc/>
        public override async Task<(string[], Exception error)> GetFolderEntries()
        {
            return await Task.Run<(string[], Exception)>(() =>
            {
                return (new string[] { "/" }, null);
            });
        }

        /// <inheritdoc/>
        public override async Task<(Stream stream, string suggestedFileName, Exception error)> OpenEntryAsync(string entry)
        {
            var (irs, error) = await this.OpenEntryAsRandomAccessStreamAsync(entry);

            if (error != null)
            {
                return (null, null, error);
            }

            var decoder = await BitmapDecoder.CreateAsync(irs);
            var bitmap = decoder.GetSoftwareBitmapAsync();

            var outputIrs = new InMemoryRandomAccessStream();

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outputIrs);
            encoder.SetSoftwareBitmap(await bitmap);

            await encoder.FlushAsync();

            var outputStream = new MemoryStream();
            outputIrs.Seek(0);
            outputIrs.AsStreamForRead().CopyTo(outputStream);

            outputIrs.Dispose();
            outputStream.Seek(0, SeekOrigin.Begin);

            return (outputStream, entry + ".png", null);
        }

        /// <inheritdoc/>
        public override async Task<(IRandomAccessStream, Exception error)> OpenEntryAsRandomAccessStreamAsync(string entry)
        {
            var pageindex = uint.Parse(entry);

            var page = this.pdfDocument.GetPage(pageindex);

            var stream = new InMemoryRandomAccessStream();

            await page.RenderToStreamAsync(stream);

            return (stream, null);
        }

        /// <inheritdoc/>
        public override Task<(string[], Exception error)> GetAllFileEntries()
        {
            return Task.Run<(string[], Exception)>(() => (this.pages, null));
        }

        /// <inheritdoc/>
        public override Task<(string, Exception error)> GetParentEntry(string entry)
        {
            return Task.Run<(string, Exception)>(() => (this.Root, null));
        }
    }
}