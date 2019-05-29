﻿// <copyright file="MediaManager.cs" company="Wutipong Wongsakuldej">
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
        private static AbstractMediaProvider provider;
        private static string currentItem;
        private static string currentFolder;
        private static string[] currentFolderItems = null;

        static MediaManager()
        {
            MediaProviderChange += MediaManager_MediaProviderChangeAsync;
            CurrentItemChange += MediaManager_CurrentItemChange;
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
        /// Gets or sets a value indicating whether the auto advance operation flag is on.
        /// </summary>
        public static bool AutoAdvance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether item will be advanced throughout the current media provider during auto-advance operation.
        /// </summary>
        public static bool AutoAdvanceLocally { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not item will be advanced randomly during auto-advance operation.
        /// </summary>
        public static bool AutoAdvanceRandomly { get; set; }

        /// <summary>
        /// Gets the current media provider.
        /// </summary>
        public static AbstractMediaProvider Provider
        {
            get => provider;
        }

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
                    CurrentFolderChange(value);
                    currentFolder = value;
                }
            }
        }

        /// <summary>
        /// Gets the files entries under the current folder.
        /// </summary>
        public static string[] CurrentFolderEntries
        {
            get => currentFolderItems;
            private set
            {
                if (value != currentFolderItems)
                {
                    CurrentFolderItemsChange(value);
                    currentFolderItems = value;
                }
            }
        }

        /// <summary>
        /// Set the media provider.
        /// </summary>
        /// <param name="newProvider">the new media provider.</param>
        /// <returns>Exception when there're errors. Null otherwise.</returns>
        public static async Task<Exception> SetProvider(AbstractMediaProvider newProvider)
        {
            if (newProvider != provider)
            {
                var error = await MediaProviderChange(provider);
                if (error != null)
                {
                    return error;
                }
            }

            provider = newProvider;
            return null;
        }

        /// <summary>
        /// Advance the current entry.
        /// </summary>
        /// <param name="step">step to advance, will be ignored if the AdvanceRandomly is true.</param>
        public static void Advance(int step = 1)
        {
            string[] eligibleItems;
            if (AutoAdvance == true)
            {
                eligibleItems = AutoAdvanceLocally ? CurrentFolderEntries : FileEntries;
            }
            else
            {
                eligibleItems = CurrentFolderEntries;
            }

            int index = Array.IndexOf(eligibleItems, CurrentEntry);
            if (AutoAdvanceRandomly)
            {
                index += new Random().Next(eligibleItems.Length);
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

            var cover = MediaManager.Provider.FileFilter.FindCoverPage(children);

            return cover;
        }

        private static void CurrentFolderItemsChange(string[] items)
        {
            CurrentEntry = items.Length != 0 ? items[0] : string.Empty;
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

            return null;
        }
    }
}