// <copyright file="MediaManager.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Threading.Tasks;
    using NaturalSort.Extension;

    /// <summary>
    /// MediaManager contains variuos functions to interact with the MediaProvider.
    /// </summary>
    public static class MediaManager
    {
        private static string currentItem;
        private static string currentFolder;
        private static string[] currentFolderEntries = null;

        static MediaManager()
        {
            MediaProviderChange += MediaManager_MediaProviderChangeAsync;
            CurrentItemChange += MediaManager_CurrentItemChange;
            CurrentFolderChange += MediaManager_CurrentFolderChange;
        }

        /// <summary>
        /// A delgate for property change events.
        /// </summary>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        /// <param name="newvalue">New value to be set to the property.</param>
        /// <returns>Exception when the operation fails.</returns>
        public delegate Task<Exception> PropertyChangeHandler<T>(T newvalue);

        /// <summary>
        /// Media provider change event.
        /// </summary>
        public static event PropertyChangeHandler<AbstractMediaProvider> MediaProviderChange;

        /// <summary>
        /// Current Item change event.
        /// </summary>
        public static event PropertyChangeHandler<string> CurrentItemChange;

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
            get => currentItem;
            set
            {
                if (value != currentItem)
                {
                    CurrentItemChange(value);
                    currentItem = value;
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
                var (items, error) = await Provider.GetChildEntries(CurrentFolder);
                if (error != null)
                {
                    return (null, error);
                }

                currentFolderEntries = items;
                Array.Sort(currentFolderEntries, StringComparer.InvariantCultureIgnoreCase.WithNaturalSort());
            }

            return (currentFolderEntries, null);
        }

        /// <summary>
        /// Change the media provider.
        /// </summary>
        /// <param name="newProvider">the new media provider.</param>
        /// <returns>Exception when there're errors. Null otherwise.</returns>
        public static async Task<Exception> ChangeProvider(AbstractMediaProvider newProvider)
        {
            if (newProvider != Provider)
            {
                var error = await MediaProviderChange(newProvider);
                if (error != null)
                {
                    return error;
                }
            }

            if (Provider != null)
            {
                Provider.Dispose();
            }

            Provider = newProvider;
            return null;
        }

        /// <summary>
        /// Advance the current entry.
        /// </summary>
        /// <param name="current">Use items under current folder.</param>
        /// <param name="random">Advance randomly.</param>
        /// <param name="step">step to advance, will be ignored if the random is true.</param>
        public static async void Advance(bool current = true, bool random = false, int step = 1)
        {
            string[] eligibleItems;

            if (current)
            {
                (eligibleItems, _) = await GetCurrentFolderFileEntries();
            }
            else
            {
                eligibleItems = FileEntries;
            }

            int index = Array.IndexOf(eligibleItems, CurrentEntry);
            if (random)
            {
                index += new Random(eligibleItems.Length).Next();
            }
            else
            {
                index += step;
            }

            index += eligibleItems.Length;
            while (index >= eligibleItems.Length)
            {
                index -= eligibleItems.Length;
            }

            if (index < 0)
            {
                index = 0;
            }

            CurrentEntry = eligibleItems[index];
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

        private static Task<Exception> MediaManager_CurrentItemChange(string newvalue)
        {
            CurrentFolder = Provider.GetParentEntry(newvalue);
            return null;
        }

        private static async Task<Exception> MediaManager_MediaProviderChangeAsync(AbstractMediaProvider provider)
        {
            var (fileEntries, errorFile) = await provider.GetAllFileEntries();
            if (errorFile != null)
            {
                return errorFile;
            }

            FileEntries = fileEntries;

            var (folderEntries, errorFolder) = await provider.GetFolderEntries();
            if (errorFolder != null)
            {
                return errorFolder;
            }

            var comparer = StringComparer.InvariantCultureIgnoreCase.WithNaturalSort();

            Array.Sort(folderEntries, (s1, s2) =>
            {
                if (s1 == provider.Root)
                {
                    return -1;
                }
                else if (s2 == provider.Root)
                {
                    return 1;
                }
                else
                {
                    return comparer.Compare(s1, s2);
                }
            });

            FolderEntries = folderEntries;
            CurrentFolder = provider.Root;
            currentFolderEntries = null;

            return null;
        }

        private static Task<Exception> MediaManager_CurrentFolderChange(string newvalue)
        {
            currentFolderEntries = null;

            return null;
        }
    }
}