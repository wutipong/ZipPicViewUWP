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
        private Dictionary<string, ThumbnailPage> thumbnailPages;
        private string currentFolder;
        private PrintHelper printHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
        }

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

        private async void FullscreenButton_Checked(object sender, RoutedEventArgs e)
        {
            this.FullScreenButton.Icon = new SymbolIcon(Symbol.BackToWindow);
            this.FullScreenButton.Label = "Exit Fullscreen";
            var view = ApplicationView.GetForCurrentView();
            if (view.TryEnterFullScreenMode())
            {
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            }

            await this.ViewerControl?.UpdateImage();
        }

        private void FullscreenButtonUnchecked(object sender, RoutedEventArgs e)
        {
            this.FullScreenButton.Icon = new SymbolIcon(Symbol.FullScreen);
            this.FullScreenButton.Label = "Fullscreen";
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
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
                this.SetFileNameTextBox(selected.Name);
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

                    this.SetFileNameTextBox(selected.Name);
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
            this.SetFileNameTextBox(selected.Name);

            this.Page.TopAppBar.Visibility = Visibility.Visible;
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
            this.FullScreenButton.IsChecked = ApplicationView.GetForCurrentView().IsFullScreenMode;
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
                foreach (var page in this.thumbnailPages?.Values)
                {
                    page.Release();
                }
            }

            this.thumbnailPages = new Dictionary<string, ThumbnailPage>();

            var count = MediaManager.FolderEntries.Length;
            dialog.IsIndeterminate = false;
            dialog.Maximum = count;

            var trees = FolderListItem.BuildTreeString(MediaManager.FolderEntries);

            for (int i = 0; i < count; i++)
            {
                var folder = MediaManager.FolderEntries[i];
                var name = folder;

                dialog.Value = i;

                var item = new FolderListItem { FolderEntry = folder, TreeText = trees[i] };
                var navigationitem = new NavigationViewItem() { Content = item };

                this.NavigationPane.MenuItems.Add(navigationitem);

                var cover = await MediaManager.FindFolderThumbnailEntry(folder);

                if (cover != null)
                {
                    await this.UpdateFolderThumbnail(cover, item);
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
                    this.thumbnailPages?[item.FolderEntry].CancellationToken.Cancel();
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

        private void SetFileNameTextBox(string filename)
        {
            this.FilenameTextBlock.Text = filename.Ellipses(100);
            this.thumbnailPages[MediaManager.Provider.Root].Title = filename;
            this.thumbnailPages[MediaManager.Provider.Root].TitleStyle = Windows.UI.Text.FontStyle.Italic;
        }

        private void ImageControl_ControlLayerVisibilityChange(object sender, Visibility e)
        {
            this.Page.TopAppBar.Visibility = e;
        }

        private async void NavigationPane_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
        {
            var selectedItem = this.NavigationPane.SelectedItem as NavigationViewItem;

            if (e.IsSettingsSelected == true)
            {
                this.ThumbnailBorder.Child = new SettingPage();
                return;
            }

            if (this.currentFolder != null && this.thumbnailPages.ContainsKey(this.currentFolder))
            {
                this.thumbnailPages[this.currentFolder]?.CancellationToken?.Cancel();
            }

            var item = selectedItem as NavigationViewItem;

            var folderListItem = item.Content as FolderListItem;

            var folder = folderListItem.FolderEntry;
            if (!this.thumbnailPages.ContainsKey(folder))
            {
                this.thumbnailPages[folder] = new ThumbnailPage()
                {
                    Title = folder.ExtractFilename(),
                    TitleStyle = Windows.UI.Text.FontStyle.Normal,
                    PrintHelper = this.printHelper,
                    Notification = this.Notification,
                };
                this.thumbnailPages[folder].ItemClicked += this.ThumbnailPage_ItemClicked;
                await this.thumbnailPages[folder].SetFolderEntry(folder);
            }

            this.ThumbnailBorder.Child = this.thumbnailPages[folder];
            this.ThumbnailBorderOpenStoryboard.Begin();

            this.currentFolder = folderListItem.FolderEntry;
            await this.thumbnailPages[folder].ResumeLoadThumbnail();
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