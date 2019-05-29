namespace ZipPicViewUWP
{
    using System;
    using System.Threading.Tasks;
    using NaturalSort.Extension;
    public static class MediaManager
    {
        private static AbstractMediaProvider provider;
        private static string currentItem;
        private static string currentFolder;
        private static string[] currentFolderItems;

        public static event PropertyChangeHandler<AbstractMediaProvider> MediaProviderChange;

        public static event PropertyChangeHandler<string> CurrentItemChange;

        public static event PropertyChangeHandler<string> CurrentFolderChange;

        public static string[] FileEntries { get; private set; }

        public static string[] FolderEntries { get; private set; }

        public delegate Task<Exception> PropertyChangeHandler<T>(T newvalue);

        public static bool AutoAdvance { get; set; }

        public static bool AutoAdvanceLocally { get; set; }

        public static bool AutoAdvanceRandomly { get; set; }

        static MediaManager()
        {
            MediaProviderChange += MediaManager_MediaProviderChangeAsync;
            CurrentItemChange += MediaManager_CurrentItemChange;
            CurrentFolderChange += MediaManager_CurrentFolderChange;
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

        private static async Task<Exception> MediaManager_CurrentFolderChange(string folder)
        {
            var (currentFolderItems, error) = await Provider.GetChildEntries(folder);
            if (error != null)
            {
                return error;
            }

            CurrentFolderItems = currentFolderItems;

            return null;
        }

        private static Task<Exception> MediaManager_CurrentItemChange(string newvalue)
        {
            CurrentFolder = Provider.GetParentEntry(newvalue);
            return null;
        }

        public static AbstractMediaProvider Provider
        {
            get => provider;
        }

        public static async Task<Exception> SetProvider(AbstractMediaProvider value)
        {
            if (value != provider)
            {
                var error = await MediaProviderChange(value);
                if (error != null)
                {
                    return error;
                }
            }

            provider = value;
            return null;
        }

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

        public static string[] CurrentFolderItems
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

        public static void Advance(int step = 1)
        {
            string[] eligibleItems;
            if (AutoAdvance == true)
            {
                eligibleItems = AutoAdvanceLocally ? CurrentFolderItems : FileEntries;
            }
            else
            {
                eligibleItems = CurrentFolderItems;
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
    }
}
