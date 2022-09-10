// <copyright file="MainPage.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media.Imaging;
    using ZipPicViewUWP.MediaProvider;
    using ZipPicViewUWP.Utility;
    using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
    using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
    using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;

    /// <summary>
    /// Main page of the program.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private FileOpenPicker fileOpenPicker = null;
        private FolderPicker folderPicker = null;
        private List<ThumbnailPage> thumbnailPages;
        private string currentFolder;
        private PrintHelper printHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the current file name.
        /// </summary>
        public string FileName { get; private set; } = string.Empty;

        private async void DisplayPanelDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count <= 0)
                {
                    return;
                }

                switch (items[0])
                {
                    case StorageFile _:
                    case StorageFolder _:
                        e.AcceptedOperation = DataPackageOperation.Copy;
                        break;
                }
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private async void DisplayPanelDrop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                return;
            }

            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count <= 0)
            {
                return;
            }

            switch (items[0])
            {
                case StorageFile _:
                {
                    var storageFile = items[0] as StorageFile;
                    await this.OpenFile(storageFile);
                    break;
                }

                case StorageFolder _:
                {
                    var storageFolder = items[0] as StorageFolder;
                    await this.OpenFolder(storageFolder);
                    break;
                }
            }
        }

        private void HideImageControl()
        {
            this.ViewerControl.Hide();
            this.NavigationPane.IsEnabled = true;
        }

        private void ImageControlCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.HideImageControl();

            var parent = MediaManager.CurrentFolder;
            this.NavigationPane.SelectedItem = this.NavigationPane.MenuItems.First((obj) =>
            {
                var selectedItem = obj as NavigationViewItem;
                var item = (FolderListItem)selectedItem?.Content;

                return item?.FolderEntry == parent;
            });
        }

        private async Task OpenFile(StorageFile selected)
        {
            if (selected == null)
            {
                this.IsEnabled = true;
                return;
            }

            if (selected.FileType == ".pdf")
            {
                var provider = new PdfMediaProvider(selected);
                await provider.Load();

                this.SetFileName(selected.Name);
                await this.ChangeMediaProvider(provider);
            }
            else
            {
                Stream stream = null;
                try
                {
                    stream = await selected.OpenStreamForReadAsync();

                    var archive = ArchiveMediaProvider.TryOpenArchive(stream, null, out var isEncrypted);
                    if (isEncrypted)
                    {
                        var dialog = new PasswordDialog();
                        var result = await dialog.ShowAsync();
                        if (result != ContentDialogResult.Primary)
                        {
                            return;
                        }

                        var password = dialog.Password;
                        archive = ArchiveMediaProvider.TryOpenArchive(stream, password, out isEncrypted);
                    }

                    var provider = ArchiveMediaProvider.Create(stream, archive);

                    this.SetFileName(selected.Name);
                    await this.ChangeMediaProvider(provider);
                }
                catch (Exception err)
                {
                    stream?.Dispose();
                    var dialog = new MessageDialog($"Cannot open file: {selected.Name}. {err.Message}", "Error");
                    await dialog.ShowAsync();
                    this.IsEnabled = true;
                }
            }
        }

        private async void OpenFileButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.fileOpenPicker != null)
            {
                return;
            }

            this.IsEnabled = false;
            this.fileOpenPicker = new FileOpenPicker();
            this.fileOpenPicker.FileTypeFilter.Add(".zip");
            this.fileOpenPicker.FileTypeFilter.Add(".rar");
            this.fileOpenPicker.FileTypeFilter.Add(".7z");
            this.fileOpenPicker.FileTypeFilter.Add(".pdf");
            this.fileOpenPicker.FileTypeFilter.Add("*");

            var selected = await this.fileOpenPicker.PickSingleFileAsync();
            this.fileOpenPicker = null;

            await this.OpenFile(selected);
        }

        private async Task OpenFolder(StorageFolder selected)
        {
            if (selected == null)
            {
                this.IsEnabled = true;
                return;
            }

            this.SetFileName(selected.Name);
            await this.ChangeMediaProvider(new FileSystemMediaProvider(selected));
        }

        private async void OpenFolderButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.folderPicker != null)
            {
                return;
            }

            this.IsEnabled = false;
            this.folderPicker = new FolderPicker();
            this.folderPicker.FileTypeFilter.Add("*");

            var selected = await this.folderPicker.PickSingleFolderAsync();
            this.folderPicker = null;

            await this.OpenFolder(selected);
        }

        private void PageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.ViewerControl.ExpectedImageWidth = (int)e.NewSize.Width;
            this.ViewerControl.ExpectedImageHeight = (int)e.NewSize.Height;
        }

        private async Task RebuildSubFolderList(ProgressDialog dialog)
        {
            foreach (var item in this.NavigationPane.MenuItems)
            {
                if (!(item is NavigationViewItem nv))
                {
                    continue;
                }

                if (nv.Content is FolderListItem fd)
                {
                    fd.Release();
                }
            }

            this.NavigationPane.MenuItems.Clear();

            if (this.thumbnailPages != null)
            {
                foreach (var page in this.thumbnailPages)
                {
                    page.Release();
                }
            }

            this.thumbnailPages = new List<ThumbnailPage>();

            var count = MediaManager.FolderEntries.Count();
            dialog.IsIndeterminate = false;
            dialog.Maximum = count;

            var tree = FolderListItem.BuildTreeString(MediaManager.FolderEntries);
            var itemArray = tree as string[] ?? tree.ToArray();

            for (var i = 0; i < count; i++)
            {
                var folder = MediaManager.FolderEntries.ElementAt(i);

                dialog.Value = i;
                var item = new FolderListItem { FolderEntry = folder, TreeText = itemArray.ElementAt(i) };
                var navigationItem = new NavigationViewItem() { Content = item };

                this.NavigationPane.MenuItems.Add(navigationItem);

                var cover = await MediaManager.FindFolderThumbnailEntry(folder);

                if (cover != null)
                {
                    _ = this.UpdateFolderThumbnail(cover, item);
                }
            }

            dialog.Value = count;
        }

        private void ThumbnailPage_ItemClicked(object source, string entry)
        {
            MediaManager.CurrentEntry = entry;
            this.ViewerControl.Show();
            this.NavigationPane.IsEnabled = false;
        }

        private async Task UpdateFolderThumbnail(string entry, FolderListItem item)
        {
            var bitmap = await MediaManager.CreateThumbnail(entry, 20, 32);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);

            item.SetImageSourceAsync(source);
        }

        private async Task ChangeMediaProvider(AbstractMediaProvider provider)
        {
            if (this.NavigationPane.SelectedItem is NavigationViewItem selectedItem)
            {
                if (selectedItem.Content is FolderListItem item)
                {
                    var page = this.thumbnailPages.Find(p => p.Entry == item.FolderEntry);
                    page?.CancellationToken.Cancel();
                }
            }

            var dialog = new ProgressDialog
            {
                BodyText = "Reading folder structure...",
                Title = "Please wait.",
            };

            _ = dialog.ShowAsync(ContentDialogPlacement.Popup);
            try
            {
                await MediaManager.ChangeProvider(provider);
                await this.RebuildSubFolderList(dialog);
            }
            finally
            {
                dialog.Hide();
                GC.Collect();
            }

            this.NavigationPane.SelectedItem = this.NavigationPane.MenuItems[0];

            this.ThumbnailBorder.Opacity = 0;

            this.HideImageControl();
            this.IsEnabled = true;
        }

        private void SetFileName(string filename)
        {
            this.FileName = filename;
        }

        private async void NavigationPane_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (this.NavigationPane.SelectedItem == null)
            {
                return;
            }

            if (e.IsSettingsSelected)
            {
                this.ThumbnailBorder.Child = new SettingPage();
                return;
            }

            var dialog = new ProgressDialog()
            {
                BodyText = "Reading folder contents.",
                Title = "Please wait",
            };

            try
            {
                _ = dialog.ShowAsync(ContentDialogPlacement.Popup);
                if (this.currentFolder != null && this.thumbnailPages != null)
                {
                    var currentPage = this.thumbnailPages.Find(p => p.Entry == this.currentFolder);
                    currentPage?.CancellationToken.Cancel();
                }

                var item = this.NavigationPane.SelectedItem as NavigationViewItem;

                var folderListItem = item?.Content as FolderListItem;

                var folder = folderListItem?.FolderEntry;
                var page = this.thumbnailPages?.Find((p) => p.Entry == folder);

                if (page == null)
                {
                    page = new ThumbnailPage
                    {
                        PrintHelper = this.printHelper,
                        Notification = this.Notification,
                    };
                    page.ItemClicked += this.ThumbnailPage_ItemClicked;
                    await page.SetFolderEntry(folder);

                    this.thumbnailPages?.Add(page);
                    if (this.thumbnailPages?.Count > 10)
                    {
                        var removePage = this.thumbnailPages[0];
                        this.thumbnailPages.Remove(removePage);

                        removePage.Release();
                    }
                }

                this.ThumbnailBorderOpenStoryboard.Begin();

                this.ThumbnailBorder.Child = page;

                page.Title = folder == MediaManager.Provider.Root ? this.FileName : folder.ExtractFilename();

                page.TitleStyle = folder == MediaManager.Provider.Root
                    ? Windows.UI.Text.FontStyle.Oblique
                    : Windows.UI.Text.FontStyle.Normal;

                this.currentFolder = folderListItem?.FolderEntry;
                _ = page.ResumeLoadThumbnail();
            }
            finally
            {
                dialog.Hide();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.printHelper = new PrintHelper(this);
            this.printHelper.RegisterForPrinting();

            this.ViewerControl.PrintHelper = this.printHelper;
        }

        private async void NavigationViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(sender is NavigationViewItem item))
            {
                return;
            }

            if (item.Content as string != "About")
            {
                return;
            }

            var dialog = new AboutDialog();
            await dialog.ShowAsync();
        }
    }
}