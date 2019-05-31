// <copyright file="ViewerControl.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.Graphics.Imaging;
    using Windows.Storage.Pickers;
    using Windows.Storage.Streams;
    using Windows.System.Display;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media.Imaging;
    using ZipPicViewUWP.Utility;

    /// <summary>
    /// Overlay control on the image viewer.
    /// </summary>
    public sealed partial class ViewerControl : UserControl
    {
        private static readonly TimeSpan[] AdvanceDurations =
            {
                new TimeSpan(0, 0, 1),
                new TimeSpan(0, 0, 5),
                new TimeSpan(0, 0, 10),
                new TimeSpan(0, 0, 15),
                new TimeSpan(0, 0, 30),
                new TimeSpan(0, 1, 0),
                new TimeSpan(0, 2, 30),
                new TimeSpan(0, 5, 0),
                new TimeSpan(0, 10, 0),
                new TimeSpan(0, 15, 0),
                new TimeSpan(0, 30, 0),
                new TimeSpan(1, 0, 0),
            };

        private readonly DispatcherTimer timer;
        private int counter;
        private string filename;
        private DisplayRequest displayRequest;
        private MediaElement clickSound;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewerControl"/> class.
        /// </summary>
        public ViewerControl()
        {
            this.InitializeComponent();
            this.timer = new DispatcherTimer();
            this.timer.Tick += this.Timer_Tick;
            this.timer.Interval = new TimeSpan(0, 0, 1);

            this.DurationList.Items.Clear();
            var oneMinute = TimeSpan.FromMinutes(1.00);
            foreach (var duration in AdvanceDurations)
            {
                var durationStr = duration < oneMinute ?
                    string.Format("{0} Second(s)", duration.Seconds) :
                    string.Format("{0}:{1:00} Minute(s)", (int)duration.TotalMinutes, duration.Seconds);

                this.DurationList.Items.Add(durationStr);
            }

            var applicationData = Windows.Storage.ApplicationData.Current;
            applicationData.LocalSettings.Values.TryGetValue("durationIndex", out var durationIndex);
            this.DurationList.SelectedIndex = durationIndex == null ? 0 : (int)durationIndex;

            applicationData.LocalSettings.Values.TryGetValue("randomAdvance", out var randomAdvance);
            this.RandomToggle.IsOn = randomAdvance == null ? false : (bool)randomAdvance;

            applicationData.LocalSettings.Values.TryGetValue("globalAdvance", out var globalAdvance);
            this.GlobalToggle.IsOn = randomAdvance == null ? false : (bool)randomAdvance;

            applicationData.LocalSettings.Values.TryGetValue("precount", out var precount);
            this.PrecountToggle.IsOn = precount == null ? false : (bool)precount;

            if (!Windows.Graphics.Printing.PrintManager.IsSupported())
            {
                this.PrintButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// An event delegate. This is used to be implemented the event when the auto advance is enabled and is signaled.
        /// </summary>
        /// <param name="sender">Sender.</param>
        public delegate void AutoAdvanceEvent(object sender);

        /// <summary>
        /// An event delegate. This is used to be implemented the event triggered during pre-count.
        /// </summary>
        /// <param name="sender">Sender.</param>
        public delegate void PreCountEvent(object sender);

        /// <summary>
        /// An event triggered when close button is clicked.
        /// </summary>
        public event RoutedEventHandler CloseButtonClick
        {
            add { this.CloseButton.Click += value; }
            remove { this.CloseButton.Click -= value; }
        }

        /// <summary>
        /// Gets or sets whether or not auto advance is enabled.
        /// </summary>
        public bool? AutoEnabled
        {
            get { return this.AutoButton.IsChecked; }
            set { this.AutoButton.IsChecked = value; }
        }

        /// <summary>
        /// Gets or sets the image dimention text.
        /// </summary>
        public string DimensionText
        {
            get { return this.OriginalDimension.Text; }
            set { this.OriginalDimension.Text = value; }
        }

        /// <summary>
        /// Gets or sets the image file name.
        /// </summary>
        public string Filename
        {
            get
            {
                return this.filename;
            }

            set
            {
                this.filename = value;
                this.FilenameTextBlock.Text = this.filename.Ellipses(70);
            }
        }

        /// <summary>
        /// Reset the timer counter.
        /// </summary>
        public void ResetCounter()
        {
            if (this.AutoButton.IsChecked == false)
            {
                return;
            }

            this.counter = (int)AdvanceDurations[this.DurationList.SelectedIndex].TotalSeconds;
            this.timer.Start();
        }

        /// <summary>
        /// Show the viewer control.
        /// </summary>
        public async void Show()
        {
            await this.UpdateImage(false);
            this.Opacity = 0;
            this.Visibility = Visibility.Visible;
            this.ShowStoryBoard.Begin();
            this.displayRequest = new DisplayRequest();
            this.displayRequest.RequestActive();
        }

        /// <summary>
        /// Hide the viewer control.
        /// </summary>
        public void Hide()
        {
            if (this.Visibility == Visibility.Collapsed)
            {
                return;
            }

            this.displayRequest?.RequestActive();
            this.displayRequest = null;
            this.HideStoryBoard.Begin();
        }

        /// <summary>
        /// Update the Image Control with new content.
        /// </summary>
        /// <param name="withDelay">A flag whether or not to delay 250ms before display a loading control.</param>
        /// <returns>A Task.</returns>
        public async Task UpdateImage(bool withDelay = false)
        {
            var file = MediaManager.CurrentEntry;
            bool showLoading = true;

            uint width = (uint)this.ImageBorder.RenderSize.Width;
            uint height = (uint)this.ImageBorder.RenderSize.Height;

            var createBitmapTask = Task.Run<(SoftwareBitmap Bitmap, uint PixelWidth, uint PixelHeight)>(async () =>
            {
                var (stream, error) = await MediaManager.Provider.OpenEntryAsRandomAccessStreamAsync(file);
                if (error != null)
                {
                    stream = await MediaManager.CreateErrorImageStream();
                }

                var decoder = await BitmapDecoder.CreateAsync(stream);
                var output = await ImageHelper.CreateResizedBitmap(decoder, width, height);

                stream.Dispose();
                showLoading = false;
                return (output, decoder.PixelWidth, decoder.PixelHeight);
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(withDelay ? 500 : 0);
                if (showLoading)
                {
                    this.LoadingControl.IsLoading = true;
                }
            });

            this.FilenameTextBlock.Text = file.ExtractFilename();

            var source = new SoftwareBitmapSource();
            var (bitmap, origWidth, origHeight) = await createBitmapTask;
            await source.SetBitmapAsync(bitmap);
            this.OriginalDimension.Text = string.Format("{0}x{1}", origWidth, origHeight);
            this.Image.Source = source;

            this.ResetCounter();
            this.LoadingControl.IsLoading = false;
        }

        private void AutoButton_Checked(object sender, RoutedEventArgs e)
        {
            this.AutoButton.Content = new SymbolIcon(Symbol.Pause);
            this.AutoDurationButton.IsEnabled = false;

            this.SaveButton.IsEnabled = false;
            this.PreviousButton.IsEnabled = false;
            this.NextButton.IsEnabled = false;
            this.ResetCounter();
        }

        private void AutoButton_Unchecked(object sender, RoutedEventArgs e)
        {
            this.AutoButton.Content = new SymbolIcon(Symbol.Play);
            this.AutoDurationButton.IsEnabled = true;
            this.timer.Stop();
            this.SaveButton.IsEnabled = true;
            this.PreviousButton.IsEnabled = true;
            this.NextButton.IsEnabled = true;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            this.AdvanceForward();
        }

        private void Timer_Tick(object sender, object e)
        {
            this.counter--;
            if (this.counter < 5 && this.counter > 0 && this.PrecountToggle.IsOn)
            {
                this.clickSound.Play();
            }

            if (this.counter == 0)
            {
                this.AdvanceAutoBeginStoryboard.Begin();

                this.timer.Stop();
            }
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
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

                this.Notification.Show(string.Format("The image {0} has been copied to the clipboard.", MediaManager.CurrentEntry.ExtractFilename()), 1000);
            }
            catch (Exception ex)
            {
                this.Notification.Show(ex.Message, 5000);
            }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
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

            // this.printHelper.BitmapImage = output;

            // await this.printHelper.ShowPrintUIAsync("ZipPicView - " + MediaManager.CurrentEntry.ExtractFilename());
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
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

        private void HiddenControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var pos = e.GetPosition(this.HiddenControl);

            if (pos.X < 200)
            {
                this.AdvanceBackward();
            }
            else if (pos.X > this.HiddenControl.ActualWidth - 200)
            {
                this.AdvanceForward();
            }
        }

        private void HiddenControl_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (this.AutoButton.IsChecked == true)
            {
                return;
            }

            var deltaX = e.Cumulative.Translation.X;

            if (deltaX > 5)
            {
                this.AdvanceForward();
            }
            else if (deltaX < -5)
            {
                this.AdvanceBackward();
            }
        }

        private void DurationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var applicationData = Windows.Storage.ApplicationData.Current;
            applicationData.LocalSettings.Values["durationIndex"] = this.DurationList.SelectedIndex;
        }

        private void PrecountToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var applicationData = Windows.Storage.ApplicationData.Current;
            applicationData.LocalSettings.Values["precount"] = this.PrecountToggle.IsOn;
        }

        private void RandomToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var applicationData = Windows.Storage.ApplicationData.Current;
            applicationData.LocalSettings.Values["randomAdvance"] = this.RandomToggle.IsOn;
        }

        private void GlobalToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var applicationData = Windows.Storage.ApplicationData.Current;
            applicationData.LocalSettings.Values["globalAdvance"] = this.GlobalToggle.IsOn;
        }

        private void AdvanceForward()
        {
            this.AdvanceBeginStoryboard.Begin();
        }

        private void AdvanceBackward()
        {
            this.AdvanceBackwardBeginStoryboard.Begin();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            this.AdvanceBackward();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.clickSound = await MediaManager.LoadSound("beep.wav");
        }

        private void HideStoryBoard_Completed(object sender, object e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private async void AdvanceBeginStoryboard_Completed(object sender, object e)
        {
            await MediaManager.Advance(true, false, 1);
            await this.UpdateImage();
            this.AdvanceEndStoryBoard.Begin();
        }

        private async void AdvanceBackwardBeginStoryboard_Completed(object sender, object e)
        {
            await MediaManager.Advance(true, false, -1);
            await this.UpdateImage();
            this.AdvanceBackwardEndStoryBoard.Begin();
        }

        private async void AdvanceAutoBeginStoryboard_Completed(object sender, object e)
        {
            bool current = !this.GlobalToggle.IsOn;
            bool random = this.RandomToggle.IsOn;

            await MediaManager.Advance(current, random);
            await this.UpdateImage();
            this.AdvanceAutoEndStoryBoard.Begin();
        }
    }
}