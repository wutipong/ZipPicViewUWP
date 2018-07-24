using NaturalSort.Extension;
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
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Display;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using ZipPicViewUWP.Utility;

namespace ZipPicViewUWP
{
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

        private string FileName
        {
            get { return filename; }
            set
            {
                filename = value;
                filenameTextBlock.Text = filename.Ellipses(100);
            }
        }

        private string currentImageFile;

        public async Task<Exception> SetMediaProvider(AbstractMediaProvider provider)
        {
            FolderReadingDialog dialog = new FolderReadingDialog();
            var t = dialog.ShowAsync(ContentDialogPlacement.Popup);
            var waitTask = Task.Delay(1000);

            if (this.provider != null) this.provider.Dispose();
            this.provider = provider;

            subFolderListCtrl.Items.Clear();
            var (list, error) = await provider.GetFolderEntries();

            if (error != null)
            {
                dialog.Hide();
                return error;
            }

            folderList = list;

            await RebuildSubFolderList();

            subFolderListCtrl.SelectedIndex = 0;
            HideImageControl();
            IsEnabled = true;

            await waitTask;

            dialog.Hide();

            return null;
        }

        private async Task RebuildSubFolderList()
        {
            var comparer = StringComparer.InvariantCultureIgnoreCase.WithNaturalSort();
            Array.Sort(folderList, (s1, s2) =>
            {
                if (s1 == "\\") return -1;
                else if (s2 == "\\") return 1;
                else return comparer.Compare(s1, s2);
            });

            foreach (var f in folderList)
            {
                var folder = f;
                if (folder == "/") folder = "\\";
                if (folder != "\\")
                {
                    char separator = folder.Contains("\\") ? '\\' : '/';
                    int count = folder.Count(c => c == separator);

                    if (folder.EndsWith("" + separator))
                        folder = folder.Substring(0, folder.Length - 1);

                    folder = folder.Substring(folder.LastIndexOf(separator) + 1);

                    var prefix = "";
                    for (int i = 0; i < count; i++) prefix += "  ";
                    folder = prefix + folder;
                }

                var (children, error) = await provider.GetChildEntries(f);
                if (error != null)
                {
                    throw error;
                }

                var item = new FolderListItem { Text = folder, Value = f };
                subFolderListCtrl.Items.Add(item);

                if (children.Length > 0)
                {
                    var cover = provider.FileFilter.FindCoverPage(children);
                    if (cover != null)
                    {
                        var t = SetFolderThumbnail(cover, item);
                    }
                }
            }
        }

        private async Task SetFolderThumbnail(string entry, FolderListItem item)
        {
            SoftwareBitmapSource source = null;
            var (stream, error) = await provider.OpenEntryAsRandomAccessStreamAsync(entry);

            if (error != null)
                throw error;

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await ImageHelper.CreateThumbnail(decoder, 40, 50);
            source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);

            item.SetImageSourceAsync(source);
        }

        public MainPage()
        {
            this.InitializeComponent();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            clickSound = await LoadSound("beep.wav");
            imageControl.OnPreCount += ImageControl_OnPreCount;
            printHelper = new PrintHelper(this);
            printHelper.RegisterForPrinting();
        }

        private void ImageControl_OnPreCount(object sender)
        {
            clickSound.Play();
        }

        private async Task<MediaElement> LoadSound(string filename)
        {
            var sound = new MediaElement();
            var soundFile = await Package.Current.InstalledLocation.GetFileAsync(String.Format(@"Assets\{0}", filename));
            sound.AutoPlay = false;
            sound.SetSource(await soundFile.OpenReadAsync(), "");
            sound.Stop();

            return sound;
        }

        private async void OpenFileButtonClick(object sender, RoutedEventArgs e)
        {
            if (fileOpenPicker != null) return;

            IsEnabled = false;
            fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.FileTypeFilter.Add(".zip");
            fileOpenPicker.FileTypeFilter.Add(".rar");
            fileOpenPicker.FileTypeFilter.Add(".7z");
            fileOpenPicker.FileTypeFilter.Add(".pdf");
            fileOpenPicker.FileTypeFilter.Add("*");

            var selected = await fileOpenPicker.PickSingleFileAsync();
            fileOpenPicker = null;

            if (selected == null)
            {
                IsEnabled = true;
                return;
            }

            if (selected.FileType == ".pdf")
            {
                var provider = new PdfMediaProvider(selected);
                await provider.Load();

                await SetMediaProvider(provider);
                FileName = selected.Name;
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
                            return;
                        var password = dialog.Password;
                        archive = ArchiveMediaProvider.TryOpenArchive(stream, password, out isEncrypted);
                    }

                    var provider = ArchiveMediaProvider.Create(stream, archive);

                    var error = await SetMediaProvider(provider);
                    if (error != null) throw error;
                    FileName = selected.Name;
                }
                catch (Exception err)
                {
                    var dialog = new MessageDialog(String.Format("Cannot open file: {0} : {1}.", selected.Name, err.Message), "Error");
                    await dialog.ShowAsync();
                    stream?.Dispose();
                    IsEnabled = true;
                    return;
                }
            }
        }

        private async void OpenFolderButtonClick(object sender, RoutedEventArgs e)
        {
            if (folderPicker != null) return;

            IsEnabled = false;
            folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var selected = await folderPicker.PickSingleFolderAsync();
            folderPicker = null;

            if (selected == null)
            {
                IsEnabled = true;
                return;
            }

            await SetMediaProvider(new FileSystemMediaProvider(selected));
            FileName = selected.Name;
        }

        private void SubFolderButtonClick(object sender, RoutedEventArgs e)
        {
            splitView.IsPaneOpen = true;
        }

        private async void SubFolderListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            var selected = ((FolderListItem)e.AddedItems.First()).Value;
            var provider = this.provider;

            if (cancellationTokenSource != null)
                cancellationTokenSource.Cancel();

            if (thumbnailTask != null)
                await thumbnailTask;

            thumbnailTask = CreateThumbnails(selected, provider);

            var pathToken = selected.Split(new char[] { '/', '\\' });
        }

        private async Task CreateThumbnails(string selected, AbstractMediaProvider provider)
        {
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            thumbnailGrid.Items.Clear();
            var results = await provider.GetChildEntries(selected);

            fileList = results.Item1;

            Array.Sort(fileList, StringComparer.InvariantCultureIgnoreCase.WithNaturalSort());

            try
            {
                //thumbProgress.IsActive = true;
                thumbProgress.Maximum = fileList.Length;
                Thumbnail[] thumbnails = new Thumbnail[fileList.Length];

                for (int i = 0; i < fileList.Length; i++)
                {
                    var file = fileList[i];
                    thumbnails[i] = new Thumbnail();
                    var thumbnail = thumbnails[i];

                    thumbnail.Click += Thumbnail_Click;
                    thumbnail.Label.Text = file.ExtractFilename().Ellipses(25);
                    thumbnail.UserData = file;
                    thumbnail.ProgressRing.Visibility = Visibility.Collapsed;

                    thumbnailGrid.Items.Add(thumbnail);

                    token.ThrowIfCancellationRequested();
                }

                for (int i = 0; i < fileList.Length; i++)
                {
                    thumbProgressText.Text = string.Format("Loading Thumbnails {0}/{1}", i + 1, fileList.Length);
                    thumbProgress.Value = i;
                    await SetThumbnailImage(provider, fileList[i], thumbnails[i], token);
                }
                
            }
            catch { }
            finally
            {
                thumbProgressText.Text = "Idle";
                thumbProgress.Value = fileList.Length;
                cancellationTokenSource = null;
            }
        }

        private static async Task SetThumbnailImage(AbstractMediaProvider provider, string file, Thumbnail thumbnail, CancellationToken token)
        {
            var (stream, error) = await provider.OpenEntryAsRandomAccessStreamAsync(file);
            if (error != null)
            {
                throw error;
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

        private async void Thumbnail_Click(object sender, RoutedEventArgs e)
        {
            BlurBehavior.Value = 10;
            BlurBehavior.StartAnimation();
            imageControl.Visibility = Visibility.Visible;
            viewerPanel.Visibility = Visibility.Visible;

            var file = ((Thumbnail)sender).UserData;
            currentFileIndex = Array.FindIndex(fileList, (string value) => value == file);

            await SetCurrentFile(file, false);
            if (viewerPanel.Visibility == Visibility.Visible)
            {
                thumbnailGrid.IsEnabled = false;
                splitView.IsEnabled = false;
            }
        }

        private async Task SetCurrentFile(string file, bool withDelay = true)
        {
            currentImageFile = file;
            var delayTask = Task.Delay(withDelay ? 250 : 0);

            uint width = (uint)canvas.RenderSize.Width;
            uint height = (uint)canvas.RenderSize.Height;

            var createBitmapTask = Task.Run<(SoftwareBitmap Bitmap, uint PixelWidth, uint PixelHeight)>(async () =>
            {
                var (stream, error) = await provider.OpenEntryAsRandomAccessStreamAsync(file);
                var decoder = await BitmapDecoder.CreateAsync(stream);
                var output = await ImageHelper.CreateResizedBitmap(decoder, width, height);

                return (output, decoder.PixelWidth, decoder.PixelHeight);
            });

            await delayTask;
            HideImage();
            imageControl.Filename = file.ExtractFilename();

            var source = new SoftwareBitmapSource();
            var (bitmap, origWidth, origHeight) = await createBitmapTask;
            await source.SetBitmapAsync(bitmap);
            imageControl.DimensionText = string.Format("{0}x{1}", origWidth, origHeight);
            image.Source = source;

            ShowImage();
            imageControl.ResetCounter();

            if (viewerPanel.Visibility == Visibility.Collapsed)
            {
                imageBorder.Visibility = Visibility.Collapsed;
                hiddenImageControl.Visibility = Visibility.Collapsed;
            }

            displayRequest = new DisplayRequest();
            displayRequest.RequestActive();
        }

        private void ShowImage()
        {
            loadingBorder.Visibility = Visibility.Collapsed;
            imageBorder.Visibility = Visibility.Visible;
            hiddenImageControl.Visibility = Visibility.Visible;
            ImageTransitionBehavior.Value = 0;
            ImageTransitionBehavior.StartAnimation();
        }

        private void HideImage()
        {
            loadingBorder.Visibility = Visibility.Visible;
            ImageTransitionBehavior.Value = 10;
            ImageTransitionBehavior.StartAnimation();
        }

        private void CanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var child in canvas.Children)
            {
                if (child is FrameworkElement fe)
                {
                    fe.Width = e.NewSize.Width;
                    fe.Height = e.NewSize.Height;
                }
            }

            foreach (var child in viewerPanel.Children)
            {
                if (child is FrameworkElement fe)
                {
                    fe.Width = e.NewSize.Width;
                    fe.Height = e.NewSize.Height;
                }
            }
        }

        private void ImageControlCloseButtonClick(object sender, RoutedEventArgs e)
        {
            HideImageControl();
        }

        private void HideImageControl()
        {
            BlurBehavior.Value = 0;
            BlurBehavior.StartAnimation();
            imageBorder.Visibility = Visibility.Collapsed;
            imageControl.Visibility = Visibility.Collapsed;
            hiddenImageControl.Visibility = Visibility.Collapsed;
            viewerPanel.Visibility = Visibility.Collapsed;
            thumbnailGrid.IsEnabled = true;
            imageControl.AutoEnabled = false;
            splitView.IsEnabled = true;

            if (displayRequest != null)
            {
                displayRequest.RequestRelease();
                displayRequest = null;
            }
        }

        private async void ImageControlNextButtonClick(object sender, RoutedEventArgs e)
        {
            await AdvanceImage(1);
        }

        private async void ImageControlPrevButtonClick(object sender, RoutedEventArgs e)
        {
            await AdvanceImage(-1);
        }

        private async void ImageControlSaveButtonClick(object sender, RoutedEventArgs e)
        {
            var filename = fileList[currentFileIndex];
            var (stream, suggestedFileName, error) = await provider.OpenEntryAsync(filename);

            var picker = new FileSavePicker
            {
                SuggestedFileName = suggestedFileName
            };

            picker.FileTypeChoices.Add("All", new List<string>() { "." });
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                var output = await file.OpenStreamForWriteAsync();

                if (error != null)
                    throw error;

                stream.CopyTo(output);
                output.Dispose();
            }
            stream.Dispose();
        }

        private async Task AdvanceImage(int step)
        {
            currentFileIndex += step;
            while (currentFileIndex < 0 || currentFileIndex >= fileList.Length)
            {
                if (currentFileIndex < 0) currentFileIndex += fileList.Length;
                else if (currentFileIndex >= fileList.Length) currentFileIndex -= fileList.Length;
            }

            await SetCurrentFile(fileList[currentFileIndex]);
        }

        private async void HiddenImageControlManipulationCompleted(object sender, Windows.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            var deltaX = e.Cumulative.Translation.X;

            if (deltaX > 5)
            {
                await AdvanceImage(-1);
            }
            else if (deltaX < -5)
            {
                await AdvanceImage(1);
            }
        }

        private async void PageKeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (imageBorder.Visibility == Visibility.Collapsed) return;
            var key = e.Key;
            if (key == VirtualKey.Left ||
                key == VirtualKey.PageUp)
            {
                await AdvanceImage(-1);
            }
            else if (key == VirtualKey.Right ||
                key == VirtualKey.PageDown ||
                key == VirtualKey.Space)
            {
                await AdvanceImage(1);
            }
            e.Handled = true;
        }

        private void FullscreenButton_Checked(object sender, RoutedEventArgs e)
        {
            fullscreenButton.Icon = new SymbolIcon(Symbol.BackToWindow);
            fullscreenButton.Label = "Exit Fullscreen";
            var view = ApplicationView.GetForCurrentView();
            if (view.TryEnterFullScreenMode())
            {
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            }
        }

        private void FullscreenButtonUnchecked(object sender, RoutedEventArgs e)
        {
            fullscreenButton.Icon = new SymbolIcon(Symbol.FullScreen);
            fullscreenButton.Label = "Fullscreen";
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
        }

        private void PageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            fullscreenButton.IsChecked = ApplicationView.GetForCurrentView().IsFullScreenMode;
        }

        private async void ImageControlPrintButtonClick(object sender, RoutedEventArgs e)
        {
            var stream = await provider.OpenEntryAsRandomAccessStreamAsync(currentImageFile);
            var output = new BitmapImage();
            output.SetSource(stream.Item1);

            printHelper.BitmapImage = output;

            await printHelper.ShowPrintUIAsync("ZipPicView - " + currentImageFile.ExtractFilename());
        }

        private async void ImageControlCopyButtonClick(object sender, RoutedEventArgs e)
        {
            var stream = await provider.OpenEntryAsRandomAccessStreamAsync(currentImageFile);
            var dataPackage = new DataPackage();

            var memoryStream = new InMemoryRandomAccessStream();

            await RandomAccessStream.CopyAsync(stream.Item1, memoryStream);

            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(memoryStream));

            try
            {
                Clipboard.SetContent(dataPackage);

                inAppNotification.Show(string.Format("The image {0} has been copied to the clipboard", currentImageFile.ExtractFilename()), 1000);
            }
            catch (Exception ex)
            {
                inAppNotification.Show(ex.Message, 5000);
            }
        }

        private async void AboutButtonClick(object sender, RoutedEventArgs e)
        {
            AboutDialog dialog = new AboutDialog();
            await dialog.ShowAsync();
        }

        private async void HiddenImageControlTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var pos = e.GetPosition(hiddenImageControl);

            if (pos.X < 200)
            {
                await AdvanceImage(-1);
            }
            else if (pos.X > hiddenImageControl.ActualWidth - 200)
            {
                await AdvanceImage(1);
            }
            else
            {
                imageControl.Visibility = imageControl.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                page.TopAppBar.Visibility = page.TopAppBar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private async void ImageControlOnAutoAdvance(object sender)
        {
            if (fileList.Length == 0) return;
            int advance = imageControl.IsAutoAdvanceRandomly ? random.Next(fileList.Length) : 1;

            await AdvanceImage(advance);
        }
    }
}