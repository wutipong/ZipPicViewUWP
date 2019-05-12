﻿namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NaturalSort.Extension;
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

    public sealed partial class MainPage : Page
    {
        private AbstractMediaProvider provider;
        private CancellationTokenSource cancellationTokenSource;
        private string[] fileList;
        private string[] folderList;
        private int currentFileIndex = 0;
        private Task thumbnailTask = null;
        private MediaElement clickSound;
        private string filename;
        private PrintHelper printHelper;
        private DisplayRequest displayRequest;
        private FileOpenPicker fileOpenPicker = null;
        private FolderPicker folderPicker = null;
        private Random random = new Random();
        private string currentImageFile;

        public MainPage()
        {
            this.InitializeComponent();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
        }

        private string FileName
        {
            get
            {
                return this.filename;
            }

            set
            {
                this.filename = value;
                this.filenameTextBlock.Text = this.filename.Ellipses(100);
            }
        }

        public async Task<Exception> SetMediaProvider(AbstractMediaProvider provider)
        {
            FolderReadingDialog dialog = new FolderReadingDialog();
            var t = dialog.ShowAsync(ContentDialogPlacement.Popup);
            var waitTask = Task.Delay(1000);

            if (this.provider != null)
            {
                this.provider.Dispose();
            }

            this.provider = provider;

            this.subFolderListCtrl.Items.Clear();
            var (list, error) = await provider.GetFolderEntries();

            if (error != null)
            {
                dialog.Hide();
                return error;
            }

            this.folderList = list;

            await this.RebuildSubFolderList();

            this.subFolderListCtrl.SelectedIndex = 0;
            this.HideImageControl();
            this.IsEnabled = true;

            await waitTask;

            dialog.Hide();

            return null;
        }

        private static async Task<IRandomAccessStream> GetErrorImageStream()
        {
            var file = await Package.Current.InstalledLocation.GetFileAsync(@"Assets\ErrorImage.png");
            return await file.OpenReadAsync();
        }

        private static async Task SetThumbnailImage(AbstractMediaProvider provider, string file, Thumbnail thumbnail, CancellationToken token)
        {
            var (stream, error) = await provider.OpenEntryAsRandomAccessStreamAsync(file);
            if (error != null)
            {
                stream = await GetErrorImageStream();
            }

            thumbnail.ProgressRing.Visibility = Visibility.Visible;
            var decoder = await BitmapDecoder.CreateAsync(stream);
            token.ThrowIfCancellationRequested();
            SoftwareBitmap bitmap = await ImageHelper.CreateThumbnail(decoder, 200, 200);
            token.ThrowIfCancellationRequested();
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);

            token.ThrowIfCancellationRequested();
            thumbnail.Image.Source = source;
            thumbnail.ProgressRing.Visibility = Visibility.Collapsed;
        }

        private async Task RebuildSubFolderList()
        {
            var comparer = StringComparer.InvariantCultureIgnoreCase.WithNaturalSort();
            Array.Sort(this.folderList, (s1, s2) =>
            {
                if (s1 == "\\")
                {
                    return -1;
                }
                else if (s2 == "\\")
                {
                    return 1;
                }
                else
                {
                    return comparer.Compare(s1, s2);
                }
            });

            foreach (var f in this.folderList)
            {
                var folder = f;
                if (folder == "/")
                {
                    folder = "\\";
                }

                if (folder != "\\")
                {
                    char separator = folder.Contains("\\") ? '\\' : '/';
                    int count = folder.Count(c => c == separator);

                    if (folder.EndsWith(separator))
                    {
                        folder = folder.Substring(0, folder.Length - 1);
                    }

                    folder = folder.Substring(folder.LastIndexOf(separator) + 1);

                    var prefix = string.Empty;
                    for (int i = 0; i < count; i++)
                    {
                        prefix += "  ";
                    }

                    folder = prefix + folder;
                }

                var (children, error) = await this.provider.GetChildEntries(f);
                if (error != null)
                {
                    throw error;
                }

                var item = new FolderListItem { Text = folder, Value = f };
                this.subFolderListCtrl.Items.Add(item);

                if (children.Length > 0)
                {
                    var cover = this.provider.FileFilter.FindCoverPage(children);
                    if (cover != null)
                    {
                        var t = this.SetFolderThumbnail(cover, item);
                    }
                }
            }
        }

        private async Task SetFolderThumbnail(string entry, FolderListItem item)
        {
            SoftwareBitmapSource source = null;
            var (stream, error) = await this.provider.OpenEntryAsRandomAccessStreamAsync(entry);

            if (error != null)
            {
                stream = await GetErrorImageStream();
            }

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await ImageHelper.CreateThumbnail(decoder, 40, 50);
            source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);

            item.SetImageSourceAsync(source);
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            this.clickSound = await this.LoadSound("beep.wav");
            this.imageControl.OnPreCount += this.ImageControl_OnPreCount;
            this.printHelper = new PrintHelper(this);
            this.printHelper.RegisterForPrinting();
        }

        private void ImageControl_OnPreCount(object sender)
        {
            this.clickSound.Play();
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

                await this.SetMediaProvider(provider);
                this.FileName = selected.Name;
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

                    var error = await this.SetMediaProvider(provider);
                    if (error != null)
                    {
                        throw error;
                    }

                    this.FileName = selected.Name;
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

        private async Task OpenFolder(StorageFolder selected)
        {
            if (selected == null)
            {
                this.IsEnabled = true;
                return;
            }

            await this.SetMediaProvider(new FileSystemMediaProvider(selected));
            this.FileName = selected.Name;

            this.page.TopAppBar.Visibility = Visibility.Visible;
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

            var selected = ((FolderListItem)e.AddedItems.First()).Value;
            var provider = this.provider;

            if (this.cancellationTokenSource != null)
            {
                this.cancellationTokenSource.Cancel();
            }

            if (this.thumbnailTask != null)
            {
                await this.thumbnailTask;
            }

            this.thumbnailTask = this.CreateThumbnails(selected, provider);

            var pathToken = selected.Split(new char[] { '/', '\\' });
        }

        private async Task CreateThumbnails(string selected, AbstractMediaProvider provider)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            var token = this.cancellationTokenSource.Token;

            this.thumbnailGrid.Items.Clear();
            var results = await provider.GetChildEntries(selected);

            this.fileList = results.Item1;

            Array.Sort(this.fileList, StringComparer.InvariantCultureIgnoreCase.WithNaturalSort());

            try
            {
                this.thumbProgress.Maximum = this.fileList.Length;
                Thumbnail[] thumbnails = new Thumbnail[this.fileList.Length];

                for (int i = 0; i < this.fileList.Length; i++)
                {
                    var file = this.fileList[i];
                    thumbnails[i] = new Thumbnail();
                    var thumbnail = thumbnails[i];

                    thumbnail.Click += this.Thumbnail_Click;
                    thumbnail.Label.Text = file.ExtractFilename().Ellipses(25);
                    thumbnail.UserData = file;
                    thumbnail.ProgressRing.Visibility = Visibility.Collapsed;

                    this.thumbnailGrid.Items.Add(thumbnail);

                    token.ThrowIfCancellationRequested();
                }

                for (int i = 0; i < this.fileList.Length; i++)
                {
                    this.thumbProgressText.Text = string.Format("Loading Thumbnails {0}/{1}", i + 1, this.fileList.Length);
                    this.thumbProgress.Value = i;
                    await SetThumbnailImage(provider, this.fileList[i], thumbnails[i], token);
                }
            }
            catch
            {
            }
            finally
            {
                this.thumbProgressText.Text = "Idle";
                this.thumbProgress.Value = this.fileList.Length;
                this.cancellationTokenSource = null;
            }
        }

        private async void Thumbnail_Click(object sender, RoutedEventArgs e)
        {
            this.BlurBehavior.Value = 10;
            this.BlurBehavior.StartAnimation();
            this.imageControl.Visibility = Visibility.Visible;
            this.viewerPanel.Visibility = Visibility.Visible;

            var file = ((Thumbnail)sender).UserData;
            this.currentFileIndex = Array.FindIndex(this.fileList, (string value) => value == file);

            await this.SetCurrentFile(file, false);
            if (this.viewerPanel.Visibility == Visibility.Visible)
            {
                this.thumbnailGrid.IsEnabled = false;
                this.splitView.IsEnabled = false;
            }
        }

        private async Task SetCurrentFile(string file, bool withDelay = true)
        {
            this.currentImageFile = file;
            var delayTask = Task.Delay(withDelay ? 250 : 0);

            uint width = (uint)this.displayPanel.RenderSize.Width;
            uint height = (uint)this.displayPanel.RenderSize.Height;

            var createBitmapTask = Task.Run<(SoftwareBitmap Bitmap, uint PixelWidth, uint PixelHeight)>(async () =>
            {
                var (stream, error) = await this.provider.OpenEntryAsRandomAccessStreamAsync(file);
                if (error != null)
                {
                    stream = await GetErrorImageStream();
                }

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var output = await ImageHelper.CreateResizedBitmap(decoder, width, height);

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

        private void ShowImage()
        {
            this.loadingBorder.Visibility = Visibility.Collapsed;
            this.imageBorder.Visibility = Visibility.Visible;
            this.hiddenImageControl.Visibility = Visibility.Visible;
            this.ImageTransitionBehavior.Value = 0;
            this.ImageTransitionBehavior.StartAnimation();
        }

        private void HideImage()
        {
            this.loadingBorder.Visibility = Visibility.Visible;
            this.ImageTransitionBehavior.Value = 10;
            this.ImageTransitionBehavior.StartAnimation();
        }

        private async void ImageControlCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.ControlCloseBehavior.StartAnimation();
            this.ControlCloseBehavior.Value = 0;
            await Task.Delay((int)this.ControlCloseBehavior.Duration);
            this.HideImageControl();
            this.ControlCloseBehavior.Value = 1.0;
        }

        private void HideImageControl()
        {
            this.BlurBehavior.Value = 0;
            this.BlurBehavior.StartAnimation();
            this.imageBorder.Visibility = Visibility.Collapsed;
            this.imageControl.Visibility = Visibility.Collapsed;
            this.hiddenImageControl.Visibility = Visibility.Collapsed;
            this.viewerPanel.Visibility = Visibility.Collapsed;
            this.thumbnailGrid.IsEnabled = true;
            this.imageControl.AutoEnabled = false;
            this.splitView.IsEnabled = true;

            if (this.displayRequest != null)
            {
                this.displayRequest.RequestRelease();
                this.displayRequest = null;
            }
        }

        private async void ImageControlNextButtonClick(object sender, RoutedEventArgs e)
        {
            await this.AdvanceImage(1);
        }

        private async void ImageControlPrevButtonClick(object sender, RoutedEventArgs e)
        {
            await this.AdvanceImage(-1);
        }

        private async void ImageControlSaveButtonClick(object sender, RoutedEventArgs e)
        {
            var filename = this.fileList[this.currentFileIndex];
            var (stream, suggestedFileName, error) = await this.provider.OpenEntryAsync(filename);

            if (error != null)
            {
                var dialog = new MessageDialog(string.Format("Cannot open image file: {0}.", this.currentImageFile), "Error");
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

        private async Task AdvanceImage(int step)
        {
            this.currentFileIndex += step;
            while (this.currentFileIndex < 0 || this.currentFileIndex >= this.fileList.Length)
            {
                if (this.currentFileIndex < 0)
                {
                    this.currentFileIndex += this.fileList.Length;
                }
                else if (this.currentFileIndex >= this.fileList.Length)
                {
                    this.currentFileIndex -= this.fileList.Length;
                }
            }

            await this.SetCurrentFile(this.fileList[this.currentFileIndex]);
        }

        private async void HiddenImageControlManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
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

        private void PageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.fullscreenButton.IsChecked = ApplicationView.GetForCurrentView().IsFullScreenMode;
        }

        private async void ImageControlPrintButtonClick(object sender, RoutedEventArgs e)
        {
            var (stream, error) = await this.provider.OpenEntryAsRandomAccessStreamAsync(this.currentImageFile);
            if (error != null)
            {
                var dialog = new MessageDialog(string.Format("Cannot open image file: {0}.", this.currentImageFile), "Error");
                await dialog.ShowAsync();

                return;
            }

            var output = new BitmapImage();
            output.SetSource(stream);

            this.printHelper.BitmapImage = output;

            await this.printHelper.ShowPrintUIAsync("ZipPicView - " + this.currentImageFile.ExtractFilename());
        }

        private async void ImageControlCopyButtonClick(object sender, RoutedEventArgs e)
        {
            var (stream, error) = await this.provider.OpenEntryAsRandomAccessStreamAsync(this.currentImageFile);
            if (error != null)
            {
                var dialog = new MessageDialog(string.Format("Cannot open image file: {0}.", this.currentImageFile), "Error");
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

                this.inAppNotification.Show(string.Format("The image {0} has been copied to the clipboard", this.currentImageFile.ExtractFilename()), 1000);
            }
            catch (Exception ex)
            {
                this.inAppNotification.Show(ex.Message, 5000);
            }
        }

        private async void AboutButtonClick(object sender, RoutedEventArgs e)
        {
            AboutDialog dialog = new AboutDialog();
            await dialog.ShowAsync();
        }

        private async void HiddenImageControlTapped(object sender, TappedRoutedEventArgs e)
        {
            var pos = e.GetPosition(this.hiddenImageControl);

            if (pos.X < 200)
            {
                await this.AdvanceImage(-1);
            }
            else if (pos.X > this.hiddenImageControl.ActualWidth - 200)
            {
                await this.AdvanceImage(1);
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

        private async void ImageControlOnAutoAdvance(object sender)
        {
            if (this.fileList.Length == 0)
            {
                return;
            }

            int advance = this.imageControl.IsAutoAdvanceRandomly ? this.random.Next(this.fileList.Length) : 1;

            await this.AdvanceImage(advance);
        }

        private async void DisplayPanel_DragOver(object sender, DragEventArgs e)
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

        private async void DisplayPanel_Drop(object sender, DragEventArgs e)
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
    }
}