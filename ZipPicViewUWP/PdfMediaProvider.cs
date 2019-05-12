namespace ZipPicViewUWP
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Data.Pdf;
    using Windows.Graphics.Imaging;
    using Windows.Storage;
    using Windows.Storage.Streams;

    internal class PdfMediaProvider : AbstractMediaProvider
    {
        private PdfDocument pdfDocument;
        private StorageFile file;
        private uint pageCount = 0;

        public PdfMediaProvider(StorageFile file)
        {
            this.file = file;
            this.FileFilter = new PdfFileFIlter();
        }

        public async Task Load()
        {
            this.pdfDocument = await PdfDocument.LoadFromFileAsync(this.file);
            this.pageCount = this.pdfDocument.PageCount;
        }

        public override async Task<(string[], Exception error)> GetChildEntries(string entry)
        {
            return await Task.Run<(string[], Exception)>(() =>
            {
                var output = new string[this.pageCount];
                for (uint i = 0; i < this.pageCount; i++)
                {
                    output[i] = i.ToString();
                }

                return (output, null);
            });
        }

        public override async Task<(string[], Exception error)> GetFolderEntries()
        {
            return await Task.Run<(string[], Exception)>(() =>
            {
                return (new string[] { "/" }, null);
            });
        }

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

        public override async Task<(IRandomAccessStream, Exception error)> OpenEntryAsRandomAccessStreamAsync(string entry)
        {
            var pageindex = uint.Parse(entry);

            var page = this.pdfDocument.GetPage(pageindex);

            var stream = new InMemoryRandomAccessStream();

            await page.RenderToStreamAsync(stream);

            return (stream, null);
        }
    }
}
