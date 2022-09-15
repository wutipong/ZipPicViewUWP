// <copyright file="ViewerControl.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Toolkit.Uwp.UI.Controls;
    using Microsoft.Toolkit.Uwp.UI.Media;
    using Windows.Storage.Pickers;
    using Windows.System;
    using Windows.System.Display;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
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
        private DisplayRequest displayRequest;
        private MediaElement clickSound;
        private SoftwareBitmapSource source;
        private Settings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewerControl"/> class.
        /// </summary>
        public ViewerControl()
        {
            this.InitializeComponent();
            this.timer = new DispatcherTimer();
            this.timer.Tick += this.Timer_Tick;
            this.timer.Interval = new TimeSpan(0, 0, 1);

            this.DurationList?.Items?.Clear();
            var oneMinute = TimeSpan.FromMinutes(1.00);
            foreach (var duration in AdvanceDurations)
            {
                var durationStr = duration < oneMinute ? $"{duration.Seconds} Second(s)"
                    : $"{(int)duration.TotalMinutes}:{duration.Seconds:00} Minute(s)";

                this.DurationList?.Items?.Add(durationStr);
            }

            var applicationData = Windows.Storage.ApplicationData.Current;
            applicationData.LocalSettings.Values.TryGetValue("durationIndex", out var durationIndex);

            var durationList = this.DurationList;
            if (durationList != null)
            {
                durationList.SelectedIndex = durationIndex == null ? 0 : (int)durationIndex;
                if (durationList.SelectedIndex < 0 ||
                    durationList.SelectedIndex >= durationList.Items?.Count)
                {
                    durationList.SelectedIndex = 0;
                }

                durationList.SelectionChanged += this.DurationList_SelectionChanged;
            }

            applicationData.LocalSettings.Values.TryGetValue("randomAdvance", out var randomAdvance);
            this.RandomToggle.IsOn = randomAdvance != null && (bool)randomAdvance;
            this.RandomToggle.Toggled += this.RandomToggle_Toggled;

            applicationData.LocalSettings.Values.TryGetValue("globalAdvance", out var globalAdvance);
            this.GlobalToggle.IsOn = randomAdvance != null && (bool)randomAdvance;
            this.GlobalToggle.Toggled += this.GlobalToggle_Toggled;

            applicationData.LocalSettings.Values.TryGetValue("precount", out var precount);
            this.PrecountToggle.IsOn = precount != null && (bool)precount;
            this.PrecountToggle.Toggled += this.PrecountToggle_Toggled;

            applicationData.LocalSettings.Values.TryGetValue("background", out var background);

            this.settings = new Settings(applicationData);
            switch (this.settings.ImageViewBackground)
            {
                case ZipPicViewUWP.ImageViewBackground.Solid:
                    this.ImageBorder.Background = new SolidColorBrush()
                    {
                        Color = (Color)Application.Current.Resources["SystemAltHighColor"],
                    };
                    break;

                case ZipPicViewUWP.ImageViewBackground.Transparent:
                    this.ImageBorder.Background = new BackdropBlurBrush()
                    {
                        Amount = 3.0,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!PrintHelper.IsPrintingSupported)
            {
                this.PrintButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// An event triggered when close button is clicked.
        /// </summary>
        public event RoutedEventHandler CloseButtonClick
        {
            add => this.CloseButton.Click += value;
            remove => this.CloseButton.Click -= value;
        }

        /// <summary>
        /// An event triggered when control layer visibility is changed.
        /// </summary>
        public event EventHandler<Visibility> ControlLayerVisibilityChange;

        private enum Effect
        {
            None,
            BlackWhite,
            Sepia,
            Invert,
        }

        /// <summary>
        /// Gets or sets the width of image to be display.
        /// </summary>
        public int ExpectedImageWidth { get; set; } = 200;

        /// <summary>
        /// Gets or sets the height of image to be display.
        /// </summary>
        public int ExpectedImageHeight { get; set; } = 200;

        /// <summary>
        /// Gets or sets the print helper.
        /// </summary>
        public PrintHelper PrintHelper { get; set; }

        /// <summary>
        /// Gets or sets the notification control.
        /// </summary>
        public InAppNotification Notification { get; set; }

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
            var applicationData = Windows.Storage.ApplicationData.Current;
            this.settings = new Settings(applicationData);

            switch (this.settings.ImageViewBackground)
            {
                case ZipPicViewUWP.ImageViewBackground.Solid:
                    this.ImageBorder.Background = new SolidColorBrush()
                    {
                        Color = (Color)Application.Current.Resources["SystemAltHighColor"],
                    };
                    break;

                case ZipPicViewUWP.ImageViewBackground.Transparent:
                    this.ImageBorder.Background = new BackdropBlurBrush()
                    {
                        Amount = 3.0,
                    };
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.Opacity = 0;
            this.Visibility = Visibility.Visible;
            var updateImage = this.UpdateImage(false);
            this.Image.Opacity = 0;
            this.ShowStoryBoard.Begin();
            this.displayRequest = new DisplayRequest();
            this.displayRequest.RequestActive();

            await updateImage;
            this.ImageShowStoryBoard.Begin();
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

            this.AutoButton.IsChecked = false;
            this.displayRequest?.RequestActive();
            this.displayRequest = null;
            this.HideStoryBoard.Begin();
        }

        /// <summary>
        /// Update the Image Control with new content.
        /// </summary>
        /// <param name="withDelay">A flag whether or not to delay 250ms before display a loading control.</param>
        /// <returns>A Task.</returns>
        public async Task UpdateImage(bool withDelay = true)
        {
            var file = MediaManager.CurrentEntry;
            if (file == null)
            {
                return;
            }

            var width = this.ExpectedImageWidth;
            var height = this.ExpectedImageHeight;

            var createBitmapTask = MediaManager.CreateImage(file, width, height, this.settings.ImageViewInterpolationMode);

            var showLoading = new ShowLoadingTask
            {
                WithDelay = withDelay,
                Show = true,
                OnShow = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.LoadingControl.Visibility = Visibility.Visible;
                        this.LoadingShowStoryboard.Begin();
                    }).AsTask(),
            };

            var task = showLoading.Run();

            this.FilenameTextBlock.Text = file.ExtractFilename();

            var toolTip = new ToolTip { Content = file };
            ToolTipService.SetToolTip(this.FilenameTextBlock, toolTip);

            this.source?.Dispose();

            this.source = new SoftwareBitmapSource();
            var (bitmap, origWidth, origHeight) = await createBitmapTask;

            showLoading.Show = false;
            await task;

            await this.source.SetBitmapAsync(bitmap);
            this.OriginalDimension.Text = $"{origWidth}x{origHeight}";
            this.Image.Source = this.source;
            this.LoadingControl.Visibility = Visibility.Collapsed;
            this.ResetCounter();
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
            try
            {
                await MediaManager.CopyToClipboard(MediaManager.CurrentEntry);
                this.Notification.Show(
                    $"The image {MediaManager.CurrentEntry.ExtractFilename()} has been copied to the clipboard.", 1000);
            }
            catch (Exception)
            {
                var dialog = new MessageDialog(
                    $"Cannot copy image from file: {MediaManager.CurrentEntry.ExtractFilename()}.", "Error");
                await dialog.ShowAsync();
            }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await this.PrintHelper.Print(MediaManager.Provider, MediaManager.CurrentEntry);
            }
            catch (Exception)
            {
                var dialog = new MessageDialog(
                    $"Cannot copy image from file: {MediaManager.CurrentEntry.ExtractFilename()}.", "Error");
                await dialog.ShowAsync();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var entry = MediaManager.CurrentEntry;
            var suggestedFileName = MediaManager.Provider.SuggestFileNameToSave(entry);

            var picker = new FileSavePicker
            {
                SuggestedFileName = suggestedFileName,
            };

            picker.FileTypeChoices.Add("All", new List<string>() { "." });
            var file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                return;
            }

            try
            {
                await MediaManager.SaveFileAs(entry, file);
            }
            catch
            {
                var dialog = new MessageDialog($"Cannot save image file: {file.Name}.", "Error");
                await dialog.ShowAsync();
            }
        }

        private void ImageBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var pos = e.GetPosition(this.ImageBorder);

            if (pos.X < 200)
            {
                this.AdvanceBackward();
            }
            else if (pos.X > this.ImageBorder.ActualWidth - 200)
            {
                this.AdvanceForward();
            }
            else
            {
                if (this.ControlLayer.Visibility == Visibility.Visible)
                {
                    this.ControlLayerHideStoryBoard.Begin();
                }
                else
                {
                    this.ControlLayer.Opacity = 0;
                    this.ControlLayer.Visibility = Visibility.Visible;
                    this.ControlLayerShowStoryBoard.Begin();
                }
            }
        }

        private void ImageBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var deltaX = e.Cumulative.Translation.X;

            if (deltaX > 5)
            {
                this.AdvanceBackward();
            }
            else if (deltaX < -5)
            {
                this.AdvanceForward();
            }
        }

        private void DurationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DurationList.SelectedIndex == -1)
            {
                return;
            }

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

            CoreWindow.GetForCurrentThread().KeyUp += this.Keyboard_KeyUp;
        }

        private void Keyboard_KeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (this.Visibility == Visibility.Collapsed)
            {
                return;
            }

            var key = e.VirtualKey;
            switch (key)
            {
                case VirtualKey.Left:
                case VirtualKey.PageUp:
                    this.AdvanceBackward();
                    break;

                case VirtualKey.Right:
                case VirtualKey.PageDown:
                case VirtualKey.Space:
                    this.AdvanceForward();
                    break;
            }

            e.Handled = true;
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
            var current = !this.GlobalToggle.IsOn;
            var random = this.RandomToggle.IsOn;

            await MediaManager.Advance(current, random);
            await this.UpdateImage();
            this.AdvanceAutoEndStoryBoard.Begin();
        }

        private void ControlLayerHideStoryBoard_Completed(object sender, object e)
        {
            this.ControlLayer.Visibility = Visibility.Collapsed;
            this.ControlLayerVisibilityChange?.Invoke(this, Visibility.Collapsed);
        }

        private void ControlLayerShowStoryBoard_Completed(object sender, object e)
        {
            this.ControlLayerVisibilityChange?.Invoke(this, Visibility.Visible);
        }

        private void FilterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var invert = false;
            var blackWhite = false;
            var sepia = false;

            switch ((Effect)this.FilterList.SelectedIndex)
            {
                case Effect.BlackWhite:
                    blackWhite = true;
                    break;

                case Effect.Invert:
                    invert = true;
                    break;

                case Effect.Sepia:
                    sepia = true;
                    break;

                case Effect.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.InvertBorder.Visibility = invert ? Visibility.Visible : Visibility.Collapsed;
            this.BlackWhiteBorder.Visibility = blackWhite ? Visibility.Visible : Visibility.Collapsed;
            this.SepiaBorder.Visibility = sepia ? Visibility.Visible : Visibility.Collapsed;
        }

        private class ShowLoadingTask
        {
            public bool Show { get; set; } = true;

            public bool WithDelay { get; set; } = false;

            public Task OnShow { get; set; }

            public async Task Run()
            {
                if (this.WithDelay)
                {
                    await Task.Delay(1000);
                }

                if (!this.Show)
                {
                    return;
                }

                await this.OnShow;
            }
        }
    }
}