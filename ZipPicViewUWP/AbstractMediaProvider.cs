namespace ZipPicViewUWP
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage.Streams;

    public abstract class AbstractMediaProvider : IDisposable
    {
        public FileFilter FileFilter { get; protected set; }

        protected string Root => @"\";

        protected char Separator
        {
            get; set;
        }

        public abstract Task<(string[], Exception error)> GetFolderEntries();

        public abstract Task<(string[], Exception error)> GetChildEntries(string entry);

        public abstract Task<(Stream stream, string suggestedFileName, Exception error)> OpenEntryAsync(string entry);

        public virtual void Dispose()
        {
        }

        public abstract Task<(IRandomAccessStream, Exception error)> OpenEntryAsRandomAccessStreamAsync(string entry);

        public bool FilterImageFileType(string entryName)
        {
            if (this.FileFilter == null)
            {
                return true;
            }

            return this.FileFilter.IsImageFile(entryName);
        }

    }
}