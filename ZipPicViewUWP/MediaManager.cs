// <copyright file="MediaManager.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NaturalSort.Extension;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.Graphics.Imaging;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;
    using ZipPicViewUWP.Utility;

    /// <summary>
    /// MediaManager contains variuos functions to interact with the MediaProvider.
    /// </summary>
    public static class MediaManager
    {
        private static readonly SemaphoreSlim Semaphore;
        private static string currentEntry;
        private static string currentFolder;
        private static string[] currentFolderEntries = null;

        static MediaManager()
        {
            CurrentEntryChange += MediaManager_CurrentEntryChange;
            CurrentFolderChange += MediaManager_CurrentFolderChange;
            Semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// A delgate for property change events.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        /// <param name="newvalue">New value to be set to the property.</param>
        /// <returns>Exception when the operation fails.</returns>
        public delegate Task<Exception> PropertyChangeHandler<T>(T newvalue);

        /// <summary>
        /// Current entry change event.
        /// </summary>
        public static event PropertyChangeHandler<string> CurrentEntryChange;

        /// <summary>
        /// Current folder change event.
        /// </summary>
        public static event PropertyChangeHandler<string> CurrentFolderChange;

        /// <summary>
        /// Gets the list of all file entries within the provider.
        /// </summary>
        public static string[] FileEntries { get; private set; }

        /// <summary>
        /// Gets the list of all folder entries within the provider.
        /// </summary>
        public static string[] FolderEntries { get; private set; }

        /// <summary>
        /// Gets the current media provider.
        /// </summary>
        public static AbstractMediaProvider Provider { get; private set; }

        /// <summary>
        /// Gets or sets the current entry.
        /// </summary>
        public static string CurrentEntry
        {
            get => currentEntry;
            set
            {
                if (value != currentEntry)
                {
                    CurrentEntryChange(value);
                    currentEntry = value;
                }
            }
        }

        /// <summary>
        /// Gets or Sets the current folder.
        /// </summary>
        public static string CurrentFolder
        {
            get => currentFolder;
            set
            {
                if (value != currentFolder)
                {
                    CurrentFolderChange?.Invoke(value);
                    currentFolder = value;
                }
            }
        }

        /// <summary>
        /// Gets the files entries under the current folder.
        /// </summary>
        /// <returns>A result <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<(string[], Exception)> GetCurrentFolderFileEntries()
        {
            if (currentFolderEntries == null)
            {
                await Semaphore.WaitAsync();
                try
                {
                    var (entries, error) = await Provider.GetChildEntries(CurrentFolder);
                    if (error != null)
                    {
                        return (null, error);
                    }

                    currentFolderEntries = entries;
                    Array.Sort(currentFolderEntries, StringComparer.InvariantCultureIgnoreCase.WithNaturalSort());
                }
                finally
                {
                    Semaphore.Release();
                }
            }

            return (currentFolderEntries, null);
        }

        /// <summary>
        /// Load Sound.
        /// </summary>
        /// <param name="filename">sound file name.</param>
        /// <returns>A sound load<see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<MediaElement> LoadSound(string filename)
        {
            var sound = new MediaElement();
            var soundFile = await Package.Current.InstalledLocation.GetFileAsync(string.Format(@"Assets\{0}", filename));
            sound.AutoPlay = false;
            sound.SetSource(await soundFile.OpenReadAsync(), string.Empty);
            sound.Stop();

            return sound;
        }

        /// <summary>
        /// Change the media provider.
        /// </summary>
        /// <param name="newProvider">the new media provider.</param>
        /// <returns>Exception when there're errors. Null otherwise.</returns>
        public static async Task<Exception> ChangeProvider(AbstractMediaProvider newProvider)
        {
            await Semaphore.WaitAsync();
            try
            {
                if (newProvider == Provider)
                {
                    return null;
                }

                var (fileEntries, errorFile) = await newProvider.GetAllFileEntries();
                if (errorFile != null)
                {
                    return errorFile;
                }

                FileEntries = fileEntries;

                var (folderEntries, errorFolder) = await newProvider.GetFolderEntries();
                if (errorFolder != null)
                {
                    return errorFolder;
                }

                FolderEntries = folderEntries;
                CurrentFolder = newProvider.Root;
                currentFolderEntries = null;

                if (Provider != null)
                {
                    Provider.Dispose();
                }

                Provider = newProvider;
                return null;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// Advance the current entry.
        /// </summary>
        /// <param name="current">Use entries under current folder.</param>
        /// <param name="random">Advance randomly.</param>
        /// <param name="step">step to advance, will be ignored if the random is true.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<Exception> Advance(bool current, bool random, int step = 1)
        {
            string[] entries;

            if (current)
            {
                Exception error;
                (entries, error) = await GetCurrentFolderFileEntries();

                if (error != null)
                {
                    return error;
                }
            }
            else
            {
                entries = FileEntries;
            }

            int index = Array.IndexOf(entries, CurrentEntry);
            if (random)
            {
                index += new Random().Next(0, entries.Length);
            }
            else
            {
                index += step;
            }

            index += entries.Length;
            while (index >= entries.Length)
            {
                index -= entries.Length;
            }

            if (index < 0)
            {
                index = 0;
            }

            CurrentEntry = entries[index];

            return null;
        }

        /// <summary>
        /// Find an entry for folder thumbnail.
        /// </summary>
        /// <param name="folder">Folder to look at.</param>
        /// <returns>A string contains an entry for folder thumbnail (null if there is no entry) and an exception if there are errors.<see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<string> FindFolderThumbnailEntry(string folder)
        {
            var (children, error) = await Provider.GetChildEntries(folder);
            if (error != null)
            {
                throw error;
            }

            var cover = Provider.FileFilter.FindCoverPage(children);

            return cover;
        }

        /// <summary>
        /// Create an image for error.
        /// </summary>
        /// <returns>Task for creating image error stream.</returns>
        public static async Task<IRandomAccessStream> CreateErrorImageStream()
        {
            var file = await Package.Current.InstalledLocation.GetFileAsync(@"Assets\ErrorImage.png");
            return await file.OpenReadAsync();
        }

        /// <summary>
        /// Create thumbnail image of the input entry.
        /// </summary>
        /// <param name="entry">input.</param>
        /// <param name="width">expected image width.</param>
        /// <param name="height">expected image height.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<SoftwareBitmap> CreateThumbnail(string entry, int width, int height)
        {
            await Semaphore.WaitAsync();
            try
            {
                var (stream, error) = await Provider.OpenEntryAsRandomAccessStreamAsync(entry);

                if (error != null)
                {
                    stream = await CreateErrorImageStream();
                }

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var bitmap = await ImageHelper.CreateThumbnail(decoder, (uint)width, (uint)height);

                return bitmap;
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// Create an resized image of the entry. The output is resized to the largest size smaller then the input size.
        /// </summary>
        /// <param name="entry">Entry.</param>
        /// <param name="width">Expected Width.</param>
        /// <param name="height">Expected Height.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<(SoftwareBitmap, int origWidth, int origHeight)> CreateImage(string entry, int width, int height)
        {
            await Semaphore.WaitAsync();
            try
            {
                var (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(entry);
                if (error != null)
                {
                    stream = await MediaManager.CreateErrorImageStream();
                }

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var output = await ImageHelper.CreateResizedBitmap(decoder, (uint)width, (uint)height);

                stream.Dispose();
                return (output, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
            }
            finally
            {
                Semaphore.Release();
            }
        }

        /// <summary>
        /// Copy the content of this file to clipboard.
        /// </summary>
        /// <param name="entry">File to copy.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<Exception> CopyToClipboard(string entry)
        {
            try
            {
                var (stream, error) = await Provider.OpenEntryAsRandomAccessStreamAsync(entry);
                if (error != null)
                {
                    return error;
                }

                var dataPackage = new DataPackage();
                var memoryStream = new InMemoryRandomAccessStream();

                await RandomAccessStream.CopyAsync(stream, memoryStream);

                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(memoryStream));

                Clipboard.SetContent(dataPackage);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        /// <summary>
        /// Save file as.
        /// </summary>
        /// <param name="entry">Source entry>.</param>
        /// <param name="file">Output file.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<Exception> SaveFileAs(string entry, StorageFile file)
        {
            var (stream, error) = await MediaManager.Provider.OpenEntryAsync(entry);
            var output = await file.OpenStreamForWriteAsync();

            if (error != null)
            {
                return error;
            }

            stream.CopyTo(output);
            output.Dispose();

            stream.Dispose();

            return null;
        }

        /// <summary>
        /// Print the image file.
        /// </summary>
        /// <param name="printHelper">Print helper object.</param>
        /// <param name="entry">Entry to print.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<Exception> Print(PrintHelper printHelper, string entry)
        {
            {
                var (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(entry);
                if (error != null)
                {
                    return error;
                }

                var output = new BitmapImage();
                output.SetSource(stream);

                printHelper.BitmapImage = output;

                await printHelper.ShowPrintUIAsync("Printing - " + entry.ExtractFilename());
            }

            return null;
        }

        private static Task<Exception> MediaManager_CurrentEntryChange(string newvalue)
        {
            CurrentFolder = Provider.GetParentEntry(newvalue);
            return null;
        }

        private static Task<Exception> MediaManager_CurrentFolderChange(string newvalue)
        {
            currentFolderEntries = null;

            return null;
        }
    }
}