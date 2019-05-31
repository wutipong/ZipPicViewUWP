// <copyright file="MainPage.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
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

    /// <summary>
    /// Main page of the program.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private FileOpenPicker fileOpenPicker = null;
        private FolderPicker folderPicker = null;
        private PrintHelper printHelper;
        private ThumbnailPage[] thumbnailPages;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
        }

        private async void AboutButtonClick(object sender, RoutedEventArgs e)
        {
            AboutDialog dialog = new AboutDialog();
            await dialog.ShowAsync();
        }

        private void AdvanceImage(int step)
        {
            MediaManager.Advance(true, false, step);
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
            this.fullscreenButton.Icon = new SymbolIcon(Symbol.BackToWindow);
            this.fullscreenButton.Label = "Exit Fullscreen";
            var view = ApplicationView.GetForCurrentView();
            if (view.TryEnterFullScreenMode())
            {
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            }

            await this.imageControl.UpdateImage();
        }

        private void FullscreenButtonUnchecked(object sender, RoutedEventArgs e)
        {
            this.fullscreenButton.Icon = new SymbolIcon(Symbol.FullScreen);
            this.fullscreenButton.Label = "Fullscreen";
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
        }

        private void HideImageControl()
        {
            this.imageControl.Hide();
            this.imageControl.AutoEnabled = false;
            this.splitView.IsEnabled = true;
        }

        private void ImageControlCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.HideImageControl();

            var parent = MediaManager.CurrentFolder;
            var folderIndex = Array.IndexOf(MediaManager.FolderEntries, parent);
            this.subFolderListCtrl.SelectedIndex = folderIndex;
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

            this.page.TopAppBar.Visibility = Visibility.Visible;
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

            this.page.TopAppBar.Visibility = Visibility.Visible;
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

        private async void PageKeyUp(object sender, KeyRoutedEventArgs e)
        {
            var key = e.Key;
            if (key == VirtualKey.Left ||
                key == VirtualKey.PageUp)
            {
                //await this.AdvanceImage(-1);
            }
            else if (key == VirtualKey.Right ||
                key == VirtualKey.PageDown ||
                key == VirtualKey.Space)
            {
                //await this.AdvanceImage(1);
            }

            //e.Handled = true;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            this.printHelper = new PrintHelper(this);
            this.printHelper.RegisterForPrinting();
        }

        private void PageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.fullscreenButton.IsChecked = ApplicationView.GetForCurrentView().IsFullScreenMode;
        }

        private async Task RebuildSubFolderList()
        {
            this.subFolderListCtrl.Items.Clear();
            this.thumbnailPages = new ThumbnailPage[MediaManager.FolderEntries.Length];

            for (int i = 0; i < MediaManager.FolderEntries.Length; i++)
            {
                var folder = MediaManager.FolderEntries[i];
                var name = folder;

                if (name != MediaManager.Provider.Root)
                {
                    int count = name.Count(c => c == MediaManager.Provider.Separator);

                    name = name.Substring(name.LastIndexOf(MediaManager.Provider.Separator) + 1);

                    var prefix = string.Empty;
                    for (int s = 0; s < count; s++)
                    {
                        prefix += "  ";
                    }

                    name = prefix + name;
                }

                var item = new FolderListItem { Text = name, Value = folder };
                this.subFolderListCtrl.Items.Add(item);

                var cover = await MediaManager.FindFolderThumbnailEntry(folder);

                if (cover != null)
                {
                    var t = this.UpdateFolderThumbnail(cover, item);
                }

                this.thumbnailPages[i] = new ThumbnailPage();
                await this.thumbnailPages[i].SetFolderEntry(folder);
                this.thumbnailPages[i].ItemClicked += this.ThumbnailPage_ItemClicked;
            }

            this.subFolderListCtrl.SelectedIndex = 0;
        }

        private void ThumbnailPage_ItemClicked(object source, string entry)
        {
            this.imageControl.Show();
            this.viewerPanel.Visibility = Visibility.Visible;

            MediaManager.CurrentEntry = entry;
            if (this.viewerPanel.Visibility == Visibility.Visible)
            {
                this.splitView.IsEnabled = false;
            }
        }

        private async Task UpdateFolderThumbnail(string entry, FolderListItem item)
        {
            var (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(entry);

            if (error != null)
            {
                stream = await MediaManager.CreateErrorImageStream();
            }

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await ImageHelper.CreateThumbnail(decoder, 40, 50);
            SoftwareBitmapSource source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);

            item.SetImageSourceAsync(source);
        }

        private async Task<Exception> ChangeMediaProvider(AbstractMediaProvider provider)
        {
            this.thumbnailPages?[this.subFolderListCtrl.SelectedIndex].CancellationToken.Cancel();

            FolderReadingDialog dialog = new FolderReadingDialog();
            dialog.ShowAsync(ContentDialogPlacement.Popup);
            var waitTask = Task.Delay(1000);

            Exception error;
            error = await MediaManager.ChangeProvider(provider);
            if (error != null)
            {
                dialog.Hide();
                return error;
            }

            await this.RebuildSubFolderList();

            this.HideImageControl();
            this.IsEnabled = true;

            await waitTask;

            dialog.Hide();

            return null;
        }

        private void SubFolderButtonClick(object sender, RoutedEventArgs e)
        {
            this.splitView.IsPaneOpen = true;
        }

        private async void SubFolderListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            this.ThumbnailBorder.Opacity = 0;
            var selected = ((FolderListItem)e.AddedItems.First()).Value;

            foreach (var removed in e.RemovedItems)
            {
                int index = Array.IndexOf(this.subFolderListCtrl.Items.ToArray(), removed);
                this.thumbnailPages[index].CancellationToken?.Cancel();
            }

            this.ThumbnailBorderOpenStoryboard.Begin();

            this.ThumbnailBorder.Child = this.thumbnailPages[this.subFolderListCtrl.SelectedIndex];
            await this.thumbnailPages[this.subFolderListCtrl.SelectedIndex].ResumeLoadThumbnail();
        }

        private void SetFileNameTextBox(string filename)
        {
            this.filenameTextBlock.Text = filename.Ellipses(100);
        }
    }
}