﻿// <copyright file="PdfMediaProvider.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP.MediaProvider
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Data.Pdf;
    using Windows.Graphics.Imaging;
    using Windows.Storage;
    using Windows.Storage.Streams;

    /// <summary>
    /// A media provider based on PDF file.
    /// </summary>
    public class PdfMediaProvider : AbstractMediaProvider
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
            this.FileFilter = new PdfFileFilter();
        }

        /// <summary>
        /// Read the pdf document from file.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task Load()
        {
            this.pdfDocument = await PdfDocument.LoadFromFileAsync(this.file);
            this.pageCount = this.pdfDocument.PageCount;

            this.pages = new string[this.pageCount];
            for (uint i = 0; i < this.pageCount; i++)
            {
                this.pages[i] = i.ToString();
            }
        }

        /// <inheritdoc/>
        public override async Task<IEnumerable<string>> GetChildEntries(string entry)
        {
            return await Task.Run<IEnumerable<string>>(() => this.pages);
            
        }

        /// <inheritdoc/>
        public override async Task<Stream> OpenEntryAsync(string entry)
        {
            var irs = await this.OpenEntryAsRandomAccessStreamAsync(entry);
            var decoder = await BitmapDecoder.CreateAsync(irs);
            var bitmap = decoder.GetSoftwareBitmapAsync();

            var outputIrs = new InMemoryRandomAccessStream();

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outputIrs);
            encoder.SetSoftwareBitmap(await bitmap);

            await encoder.FlushAsync();

            var outputStream = new MemoryStream();
            outputIrs.Seek(0);
            await outputIrs.AsStreamForRead().CopyToAsync(outputStream);

            outputIrs.Dispose();
            outputStream.Seek(0, SeekOrigin.Begin);

            return outputStream;
        }

        /// <inheritdoc/>
        public override async Task<IRandomAccessStream> OpenEntryAsRandomAccessStreamAsync(string entry)
        {
            var pageindex = uint.Parse(entry);
            var page = this.pdfDocument.GetPage(pageindex);
            var stream = new InMemoryRandomAccessStream();

            await page.RenderToStreamAsync(stream);

            return stream;
        }

        /// <inheritdoc/>
        public override async Task<IEnumerable<string>> GetAllFileEntries()
        {
            return await Task.Run(()=> this.pages);
        }

        /// <inheritdoc/>
        public override string GetParentEntry(string entry)
        {
            return this.Root;
        }

        /// <inheritdoc/>
        public override string SuggestFileNameToSave(string entry)
        {
            return entry + ".png";
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<string>> DoGetFolderEntries()
        {
            return await Task.Run<IEnumerable<string>>(() => new string[] { this.Root });
        }
    }
}