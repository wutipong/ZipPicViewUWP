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
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Core;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.Graphics.Imaging;
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using Windows.System;
    using Windows.UI.Popups;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media.Imaging;
    using ZipPicViewUWP.Utility;

    using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
    using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
    using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
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

                if (items[0] is StorageFile)
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
                else if (items[0] is StorageFolder)
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private async void DisplayPanelDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count <= 0)
                {
                    return;
                }

                if (items[0] is StorageFile)
                {
                    var storageFile = items[0] as StorageFile;
                    await this.OpenFile(storageFile);
                }
                else if (items[0] is StorageFolder)
                {
                    var storageFolder = items[0] as StorageFolder;
                    await this.OpenFolder(storageFolder);
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
            var folderIndex = Array.IndexOf(MediaManager.FolderEntries, parent);
            this.NavigationPane.SelectedItem = this.NavigationPane.MenuItems.First((obj) =>
            {
                var selectedItem = obj as NavigationViewItem;
                var item = (FolderListItem)selectedItem.Content;

                return item.FolderEntry == parent;
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

                await this.ChangeMediaProvider(provider);
                this.SetFileName(selected.Name);
            }
            else
            {
                Stream stream = null;
                try
                {
                    stream = await selected.OpenStreamForReadAsync();

                    var archive = ArchiveMediaProvider.TryOpenArchive(stream, null, out bool isEncrypted);
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

                    var error = await this.ChangeMediaProvider(provider);
                    if (error != null)
                    {
                        throw error;
                    }

                    this.SetFileName(selected.Name);
                }
                catch (Exception err)
                {
                    var dialog = new MessageDialog(string.Format("Cannot open file: {0} : {1}.", selected.Name, err.Message), "Error");
                    await dialog.ShowAsync();
                    stream?.Dispose();
                    this.IsEnabled = true;
                    return;
                }
            }

            this.Page.TopAppBar.Visibility = Visibility.Visible;
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

            await this.ChangeMediaProvider(new FileSystemMediaProvider(selected));
            this.SetFileName(selected.Name);
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

        private async Task RebuildSubFolderList(FolderReadingDialog dialog)
        {
            foreach (var item in this.NavigationPane.MenuItems)
            {
                if (item is NavigationViewItem nv)
                {
                    if (nv.Content is FolderListItem fd)
                    {
                        fd.Release();
                    }
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

            var count = MediaManager.FolderEntries.Length;
            dialog.IsIndeterminate = false;
            dialog.Maximum = count;

            var trees = FolderListItem.BuildTreeString(MediaManager.FolderEntries);

            for (int i = 0; i < count; i++)
            {
                var folder = MediaManager.FolderEntries[i];

                dialog.Value = i;

                var item = new FolderListItem { FolderEntry = folder, TreeText = trees[i] };
                var navigationitem = new NavigationViewItem() { Content = item };

                this.NavigationPane.MenuItems.Add(navigationitem);

                var cover = await MediaManager.FindFolderThumbnailEntry(folder);

                if (cover != null)
                {
                    _ = this.UpdateFolderThumbnail(cover, item);
                }
            }

            dialog.Value = count;

            this.NavigationPane.SelectedItem = this.NavigationPane.MenuItems[0];
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
            SoftwareBitmapSource source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);

            item.SetImageSourceAsync(source);
        }

        private async Task<Exception> ChangeMediaProvider(AbstractMediaProvider provider)
        {
            if (this.NavigationPane.SelectedItem is NavigationViewItem selectedItem)
            {
                if (selectedItem.Content is FolderListItem item)
                {
                    var page = this.thumbnailPages.Find((p) => p.Entry == item.FolderEntry);
                    if (page != null)
                    {
                        page.CancellationToken.Cancel();
                    }
                }
            }

            FolderReadingDialog dialog = new FolderReadingDialog();
            _ = dialog.ShowAsync(ContentDialogPlacement.Popup);

            Exception error;
            error = await MediaManager.ChangeProvider(provider);
            if (error != null)
            {
                dialog.Hide();
                return error;
            }

            await this.RebuildSubFolderList(dialog);

            this.HideImageControl();
            this.IsEnabled = true;

            dialog.Hide();
            this.currentFolder = null;

            GC.Collect();

            return null;
        }

        private void SetFileName(string filename)
        {
            this.FileName = filename;
            this.FilenameTextBlock.Text = filename.Ellipses(100);

            var page = this.thumbnailPages.Find((p) => p.Entry == MediaManager.Provider.Root);
            if (page != null)
            {
                page.Title = filename;
                page.TitleStyle = Windows.UI.Text.FontStyle.Italic;
            }
        }

        private void ImageControl_ControlLayerVisibilityChange(object sender, Visibility e)
        {
        }

        private async void NavigationPane_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
        {
            var selectedItem = this.NavigationPane.SelectedItem as NavigationViewItem;

            if (e.IsSettingsSelected == true)
            {
                this.ThumbnailBorder.Child = new SettingPage();
                return;
            }

            if (this.currentFolder != null && this.thumbnailPages != null)
            {
                var currentPage = this.thumbnailPages.Find((p) => p.Entry == this.currentFolder);
                if (currentPage != null)
                {
                    currentPage.CancellationToken.Cancel();
                }
            }

            var item = selectedItem as NavigationViewItem;

            var folderListItem = item.Content as FolderListItem;

            var folder = folderListItem.FolderEntry;
            var page = this.thumbnailPages.Find((p) => p.Entry == folder);

            if (page == null)
            {
                page = new ThumbnailPage()
                {
                    Title = folder == MediaManager.Provider.Root ?
                        this.FileName : folder.ExtractFilename(),
                    TitleStyle = folder == MediaManager.Provider.Root ?
                        Windows.UI.Text.FontStyle.Oblique : Windows.UI.Text.FontStyle.Normal,
                    PrintHelper = this.printHelper,
                    Notification = this.Notification,
                };
                page.ItemClicked += this.ThumbnailPage_ItemClicked;
                await page.SetFolderEntry(folder);

                this.thumbnailPages.Add(page);
                if (this.thumbnailPages.Count > 10)
                {
                    var removePage = this.thumbnailPages[0];
                    this.thumbnailPages.Remove(removePage);

                    removePage.Release();
                }
            }

            this.ThumbnailBorder.Child = page;
            this.ThumbnailBorderOpenStoryboard.Begin();

            this.currentFolder = folderListItem.FolderEntry;
            await page.ResumeLoadThumbnail();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.printHelper = new PrintHelper(this);
            this.printHelper.RegisterForPrinting();

            this.ViewerControl.PrintHelper = this.printHelper;
        }

        private async void NavigationViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is NavigationViewItem item)
            {
                if (item.Content as string == "About")
                {
                    AboutDialog dialog = new AboutDialog();
                    await dialog.ShowAsync();
                }
            }

            return;
        }
    }
}