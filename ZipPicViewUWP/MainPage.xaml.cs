// <copyright file="MainPage.xaml.cs" company="Wutipong Wongsakuldej">
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
    using Windows.ApplicationModel.Core;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.Graphics.Imaging;
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using Windows.Storage.Streams;
    using Windows.System;
    using Windows.System.Display;
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
        private MediaElement clickSound;
        private DisplayRequest displayRequest;
        private FileOpenPicker fileOpenPicker = null;
        private FolderPicker folderPicker = null;
        private PrintHelper printHelper;
        private Task thumbnailTask = null;
        private ThumbnailPage[] thumbnailPages;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
        }

        private static async Task<IRandomAccessStream> GetErrorImageStream()
        {
            var file = await Package.Current.InstalledLocation.GetFileAsync(@"Assets\ErrorImage.png");
            return await file.OpenReadAsync();
        }

        private async void AboutButtonClick(object sender, RoutedEventArgs e)
        {
            AboutDialog dialog = new AboutDialog();
            await dialog.ShowAsync();
        }

        private async Task AdvanceImage(int step)
        {
            MediaManager.Advance(true, false, step);
            await this.ChangeCurrentEntry(MediaManager.CurrentEntry);
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

        private void FullscreenButton_Checked(object sender, RoutedEventArgs e)
        {
            this.fullscreenButton.Icon = new SymbolIcon(Symbol.BackToWindow);
            this.fullscreenButton.Label = "Exit Fullscreen";
            var view = ApplicationView.GetForCurrentView();
            if (view.TryEnterFullScreenMode())
            {
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            }
        }

        private void FullscreenButtonUnchecked(object sender, RoutedEventArgs e)
        {
            this.fullscreenButton.Icon = new SymbolIcon(Symbol.FullScreen);
            this.fullscreenButton.Label = "Fullscreen";
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
        }

        private async void HiddenImageControlManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (this.imageControl.AutoEnabled == true)
            {
                return;
            }

            var deltaX = e.Cumulative.Translation.X;

            if (deltaX > 5)
            {
                await this.AdvanceImage(-1);
            }
            else if (deltaX < -5)
            {
                await this.AdvanceImage(1);
            }
        }

        private async void HiddenImageControlTapped(object sender, TappedRoutedEventArgs e)
        {
            var pos = e.GetPosition(this.hiddenImageControl);

            if (pos.X < 200)
            {
                if (this.imageControl.AutoEnabled == false)
                {
                    await this.AdvanceImage(-1);
                }
            }
            else if (pos.X > this.hiddenImageControl.ActualWidth - 200)
            {
                if (this.imageControl.AutoEnabled == false)
                {
                    await this.AdvanceImage(1);
                }
            }
            else
            {
                this.imageControl.Visibility = (this.imageControl.Visibility == Visibility.Visible) ?
                    Visibility.Collapsed :
                    Visibility.Visible;

                this.page.TopAppBar.Visibility = (this.page.TopAppBar.Visibility == Visibility.Visible) ?
                    Visibility.Collapsed :
                    Visibility.Visible;
            }
        }

        private void HideImage()
        {
            this.loadingBorder.Visibility = Visibility.Visible;
            this.ImageTransitionBehavior.Value = 10;
            this.ImageTransitionBehavior.StartAnimation();
        }

        private void HideImageControl()
        {
            this.BlurBehavior.Value = 0;
            this.BlurBehavior.StartAnimation();
            this.imageBorder.Visibility = Visibility.Collapsed;
            this.imageControl.Visibility = Visibility.Collapsed;
            this.hiddenImageControl.Visibility = Visibility.Collapsed;
            this.viewerPanel.Visibility = Visibility.Collapsed;
            // this.ThumbnailBorder.Child.IsEnabled = true;
            this.imageControl.AutoEnabled = false;
            this.splitView.IsEnabled = true;

            if (this.displayRequest != null)
            {
                this.displayRequest.RequestRelease();
                this.displayRequest = null;
            }
        }

        private void ImageControlOnPreCount(object sender)
        {
            this.clickSound.Play();
        }

        private async void ImageControlCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.ControlCloseBehavior.StartAnimation();
            this.ControlCloseBehavior.Value = 0;
            await Task.Delay((int)this.ControlCloseBehavior.Duration);
            this.HideImageControl();
            this.ControlCloseBehavior.Value = 1.0;

            var parent = MediaManager.CurrentFolder;
            var folderIndex = Array.IndexOf(MediaManager.FolderEntries, parent);
            this.subFolderListCtrl.SelectedIndex = folderIndex;
        }

        private async void ImageControlCopyButtonClick(object sender, RoutedEventArgs e)
        {
            var (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(MediaManager.CurrentEntry);
            if (error != null)
            {
                var dialog = new MessageDialog(string.Format("Cannot open image file: {0}.", MediaManager.CurrentEntry), "Error");
                await dialog.ShowAsync();

                return;
            }

            var dataPackage = new DataPackage();

            var memoryStream = new InMemoryRandomAccessStream();

            await RandomAccessStream.CopyAsync(stream, memoryStream);

            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(memoryStream));

            try
            {
                Clipboard.SetContent(dataPackage);

                this.inAppNotification.Show(string.Format("The image {0} has been copied to the clipboard", MediaManager.CurrentEntry.ExtractFilename()), 1000);
            }
            catch (Exception ex)
            {
                this.inAppNotification.Show(ex.Message, 5000);
            }
        }

        private async void ImageControlNextButtonClick(object sender, RoutedEventArgs e)
        {
            await this.AdvanceImage(1);
        }

        private async void ImageControlOnAutoAdvance(object sender)
        {
            bool current = !this.imageControl.GlobalEnabled;
            bool random = this.imageControl.RandomEnabled;

            MediaManager.Advance(current, random);

            await this.ChangeCurrentEntry(MediaManager.CurrentEntry);
        }

        private async void ImageControlPrevButtonClick(object sender, RoutedEventArgs e)
        {
            await this.AdvanceImage(-1);
        }

        private async void ImageControlPrintButtonClick(object sender, RoutedEventArgs e)
        {
            var (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(MediaManager.CurrentEntry);
            if (error != null)
            {
                var dialog = new MessageDialog(string.Format("Cannot open image file: {0}.", MediaManager.CurrentEntry), "Error");
                await dialog.ShowAsync();

                return;
            }

            var output = new BitmapImage();
            output.SetSource(stream);

            this.printHelper.BitmapImage = output;

            await this.printHelper.ShowPrintUIAsync("ZipPicView - " + MediaManager.CurrentEntry.ExtractFilename());
        }

        private async void ImageControlSaveButtonClick(object sender, RoutedEventArgs e)
        {
            var filename = MediaManager.CurrentEntry;
            var (stream, suggestedFileName, error) = await MediaManager.Provider.OpenEntryAsync(filename);

            if (error != null)
            {
                var dialog = new MessageDialog(string.Format("Cannot open image file: {0}.", MediaManager.CurrentEntry), "Error");
                await dialog.ShowAsync();

                return;
            }

            var picker = new FileSavePicker
            {
                SuggestedFileName = suggestedFileName,
            };

            picker.FileTypeChoices.Add("All", new List<string>() { "." });
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                var output = await file.OpenStreamForWriteAsync();

                if (error != null)
                {
                    throw error;
                }

                stream.CopyTo(output);
                output.Dispose();
            }

            stream.Dispose();
        }

        private async Task<MediaElement> LoadSound(string filename)
        {
            var sound = new MediaElement();
            var soundFile = await Package.Current.InstalledLocation.GetFileAsync(string.Format(@"Assets\{0}", filename));
            sound.AutoPlay = false;
            sound.SetSource(await soundFile.OpenReadAsync(), string.Empty);
            sound.Stop();

            return sound;
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
            if (this.imageBorder.Visibility == Visibility.Collapsed)
            {
                return;
            }

            var key = e.Key;
            if (key == VirtualKey.Left ||
                key == VirtualKey.PageUp)
            {
                await this.AdvanceImage(-1);
            }
            else if (key == VirtualKey.Right ||
                key == VirtualKey.PageDown ||
                key == VirtualKey.Space)
            {
                await this.AdvanceImage(1);
            }

            e.Handled = true;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            this.clickSound = await this.LoadSound("beep.wav");
            this.imageControl.OnPreCount += this.ImageControlOnPreCount;
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

        private async void ThumbnailPage_ItemClicked(object source, string entry)
        {
            this.BlurBehavior.Value = 10;
            this.BlurBehavior.StartAnimation();
            this.imageControl.Visibility = Visibility.Visible;
            this.viewerPanel.Visibility = Visibility.Visible;

            await this.ChangeCurrentEntry(MediaManager.CurrentEntry, false);

            if (this.viewerPanel.Visibility == Visibility.Visible)
            {
                this.splitView.IsEnabled = false;
            }
        }

        private async Task ChangeCurrentEntry(string file, bool withDelay = true)
        {
            MediaManager.CurrentEntry = file;
            var delayTask = Task.Delay(withDelay ? 250 : 0);

            uint width = (uint)this.displayPanel.RenderSize.Width;
            uint height = (uint)this.displayPanel.RenderSize.Height;

            var createBitmapTask = Task.Run<(SoftwareBitmap Bitmap, uint PixelWidth, uint PixelHeight)>(async () =>
            {
                var (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(file);
                if (error != null)
                {
                    stream = await GetErrorImageStream();
                }

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var output = await ImageHelper.CreateResizedBitmap(decoder, width, height);

                stream.Dispose();
                return (output, decoder.PixelWidth, decoder.PixelHeight);
            });

            await delayTask;
            this.HideImage();
            this.imageControl.Filename = file.ExtractFilename();

            var source = new SoftwareBitmapSource();
            var (bitmap, origWidth, origHeight) = await createBitmapTask;
            await source.SetBitmapAsync(bitmap);
            this.imageControl.DimensionText = string.Format("{0}x{1}", origWidth, origHeight);
            this.image.Source = source;

            this.ShowImage();
            this.imageControl.ResetCounter();

            if (this.viewerPanel.Visibility == Visibility.Collapsed)
            {
                this.imageBorder.Visibility = Visibility.Collapsed;
                this.hiddenImageControl.Visibility = Visibility.Collapsed;
            }

            this.displayRequest = new DisplayRequest();
            this.displayRequest.RequestActive();
        }

        private async Task UpdateFolderThumbnail(string entry, FolderListItem item)
        {
            var (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(entry);

            if (error != null)
            {
                stream = await GetErrorImageStream();
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
            _ = dialog.ShowAsync(ContentDialogPlacement.Popup);
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

        private void ShowImage()
        {
            this.loadingBorder.Visibility = Visibility.Collapsed;
            this.imageBorder.Visibility = Visibility.Visible;
            this.hiddenImageControl.Visibility = Visibility.Visible;
            this.ImageTransitionBehavior.Value = 0;
            this.ImageTransitionBehavior.StartAnimation();
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