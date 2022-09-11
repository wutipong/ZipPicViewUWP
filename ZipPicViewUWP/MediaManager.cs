// <copyright file="MediaManager.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.Graphics.Imaging;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Controls;
    using ZipPicViewUWP.MediaProvider;

    /// <summary>
    /// MediaManager contains various functions to interact with the MediaProvider.
    /// </summary>
    public static class MediaManager
    {
        private static readonly SemaphoreSlim Semaphore;
        private static string currentEntry;
        private static string currentFolder;
        private static IEnumerable<string> currentFolderEntries = null;

        static MediaManager()
        {
            CurrentEntryChange += OnCurrentEntryChange;
            CurrentFolderChange += OnCurrentFolderChange;
            Semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// A delegate for property change events.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        /// <param name="v">New value to be set to the property.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public delegate Task PropertyChangeHandler<in T>(T v);

        /// <summary>
        /// Current entry change event.
        /// </summary>
        public static event PropertyChangeHandler<string> CurrentEntryChange;

        /// <summary>
        /// Current folder change event.
        /// </summary>
        public static event PropertyChangeHandler<string> CurrentFolderChange;

        /// <summary>
        /// Provider change event.
        /// </summary>
        public static event PropertyChangeHandler<AbstractMediaProvider> ProviderChange;

        /// <summary>
        /// Gets the list of all file entries within the provider.
        /// </summary>
        public static IEnumerable<string> FileEntries { get; private set; }

        /// <summary>
        /// Gets the list of all folder entries within the provider.
        /// </summary>
        public static IEnumerable<string> FolderEntries { get; private set; }

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
                if (value == currentEntry)
                {
                    return;
                }

                CurrentEntryChange?.Invoke(value);
                currentEntry = value;
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
                if (value == currentFolder)
                {
                    return;
                }

                CurrentFolderChange?.Invoke(value);
                currentFolder = value;
            }
        }

        /// <summary>
        /// Gets the files entries under the current folder.
        /// </summary>
        /// <returns>A result <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task<IEnumerable<string>> GetCurrentFolderFileEntries()
        {
            if (currentFolderEntries != null)
            {
                return currentFolderEntries;
            }

            await Semaphore.WaitAsync();
            try
            {
                currentFolderEntries = await Provider.GetChildEntries(CurrentFolder);
            }
            finally
            {
                Semaphore.Release();
            }

            return currentFolderEntries;
        }

        /// <summary>
        /// Load Sound.
        /// </summary>
        /// <param name="filename">sound file name.</param>
        /// <returns>A sound load<see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<MediaElement> LoadSound(string filename)
        {
            var sound = new MediaElement();
            var soundFile = await Package.Current.InstalledLocation.GetFileAsync($@"Assets\{filename}");
            sound.AutoPlay = false;
            sound.SetSource(await soundFile.OpenReadAsync(), string.Empty);
            sound.Stop();

            return sound;
        }

        /// <summary>
        /// Change the media provider.
        /// </summary>
        /// <param name="newProvider">the new media provider.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task ChangeProvider(AbstractMediaProvider newProvider)
        {
            await Semaphore.WaitAsync();
            try
            {
                if (newProvider == Provider)
                {
                    return;
                }

                FileEntries = await newProvider.GetAllFileEntries();
                FolderEntries = await newProvider.GetFolderEntries();

                CurrentFolder = newProvider.Root;
                currentFolderEntries = null;

                Provider?.Dispose();
                ProviderChange?.Invoke(newProvider);

                Provider = newProvider;
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
        public static async Task Advance(bool current, bool random, int step = 1)
        {
            var entries = current ?
                (await GetCurrentFolderFileEntries()).ToArray() :
                FileEntries.ToArray();

            var index = Array.IndexOf(entries, CurrentEntry);
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
        }

        /// <summary>
        /// Find an entry for folder thumbnail.
        /// </summary>
        /// <param name="folder">Folder to look at.</param>
        /// <returns>A string contains an entry for folder thumbnail (null if there is no entry) and an exception if there are errors.<see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<string> FindFolderThumbnailEntry(string folder)
        {
            var children = await Provider.GetChildEntries(folder);
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
            SoftwareBitmap bitmap;
            try
            {
                var stream = await Provider.OpenEntryAsRandomAccessStreamAsync(entry);
                var decoder = await BitmapDecoder.CreateAsync(stream);
                bitmap = await ImageHelper.CreateThumbnail(decoder, (uint)width, (uint)height);
            }
            catch (Exception)
            {
                bitmap = await CreateErrorBitmap(width, height);
            }
            finally
            {
                Semaphore.Release();
            }

            return bitmap;
        }

        /// <summary>
        /// Create an resized image of the entry. The output is resized to the largest size smaller then the input size.
        /// </summary>
        /// <param name="entry">Entry.</param>
        /// <param name="width">Expected Width.</param>
        /// <param name="height">Expected Height.</param>
        /// <param name="mode">Image manipulation algorithm.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<(SoftwareBitmap, int origWidth, int origHeight)> CreateImage(
            string entry, int width, int height, BitmapInterpolationMode mode)
        {
            await Semaphore.WaitAsync();
            SoftwareBitmap bitmap;
            int outWidth = 0, outHeight = 0;
            try
            {
                var stream = await Provider.OpenEntryAsRandomAccessStreamAsync(entry);
                var decoder = await BitmapDecoder.CreateAsync(stream);
                bitmap = await ImageHelper.CreateResizedBitmap(decoder, (uint)width, (uint)height, mode);
                outWidth = (int)decoder.PixelWidth;
                outHeight = (int)decoder.PixelHeight;

                stream.Dispose();
            }
            catch (Exception)
            {
                bitmap = await CreateErrorBitmap(width, height);
            }
            finally
            {
                Semaphore.Release();
            }

            return (bitmap, outWidth, outHeight);
        }

        /// <summary>
        /// Copy the content of this file to clipboard.
        /// </summary>
        /// <param name="entry">File to copy.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task CopyToClipboard(string entry)
        {
            var stream = await Provider.OpenEntryAsRandomAccessStreamAsync(entry);
            var dataPackage = new DataPackage();
            var memoryStream = new InMemoryRandomAccessStream();

            await RandomAccessStream.CopyAsync(stream, memoryStream);

            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(memoryStream));

            Clipboard.SetContent(dataPackage);
        }

        /// <summary>
        /// Save file as.
        /// </summary>
        /// <param name="entry">Source entry>.</param>
        /// <param name="file">Output file.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task SaveFileAs(string entry, StorageFile file)
        {
            using (var output = await file.OpenStreamForWriteAsync())
            using (var stream = await Provider.OpenEntryAsync(entry))
            {
                await stream.CopyToAsync(output);
            }
        }

        private static async Task<SoftwareBitmap> CreateErrorBitmap(int width, int height)
        {
            var stream = await CreateErrorImageStream();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await ImageHelper.CreateThumbnail(decoder, (uint)width, (uint)height);

            return bitmap;
        }

        private static Task OnCurrentEntryChange(string v)
        {
            CurrentFolder = Provider.GetParentEntry(v);
            return null;
        }

        private static Task OnCurrentFolderChange(string v)
        {
            currentFolderEntries = null;

            return null;
        }
    }
}