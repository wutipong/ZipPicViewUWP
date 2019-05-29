// <copyright file="ViewerControl.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
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
        private RoutedEventHandler nextButtonClick;
        private AutoAdvanceEvent onAutoAdvance;
        private PreCountEvent onPreCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewerControl"/> class.
        /// </summary>
        public ViewerControl()
        {
            this.InitializeComponent();
            this.timer = new DispatcherTimer();
            this.timer.Tick += this.Timer_Tick;
            this.timer.Interval = new TimeSpan(0, 0, 1);

            this.NextButton.Click += this.NextButton_Click;

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

            this.DurationList.SelectionChanged += (_, __) =>
            {
                applicationData.LocalSettings.Values["durationIndex"] = this.DurationList.SelectedIndex;
            };

            applicationData.LocalSettings.Values.TryGetValue("randomAdvance", out var randomAdvance);
            this.RandomToggle.IsOn = randomAdvance == null ? false : (bool)randomAdvance;

            this.RandomToggle.Toggled += (_, __) =>
            {
                applicationData.LocalSettings.Values["randomAdvance"] = this.RandomToggle.IsOn;
            };

            applicationData.LocalSettings.Values.TryGetValue("globalAdvance", out var globalAdvance);
            this.GlobalToggle.IsOn = randomAdvance == null ? false : (bool)randomAdvance;

            this.GlobalToggle.Toggled += (_, __) =>
            {
                applicationData.LocalSettings.Values["globalAdvance"] = this.GlobalToggle.IsOn;
            };

            applicationData.LocalSettings.Values.TryGetValue("precount", out var precount);
            this.PrecountToggle.IsOn = precount == null ? false : (bool)precount;
            this.PrecountToggle.Toggled += (_, __) =>
            {
                applicationData.LocalSettings.Values["precount"] = this.PrecountToggle.IsOn;
            };

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
        /// An event triggered when copy button is clicked.
        /// </summary>
        public event RoutedEventHandler CopyButtonClick
        {
            add { this.CopyButton.Click += value; }
            remove { this.CopyButton.Click -= value; }
        }

        /// <summary>
        /// An event triggered when next button is clicked.
        /// </summary>
        public event RoutedEventHandler NextButtonClick
        {
            add { this.nextButtonClick += value; }
            remove { this.nextButtonClick -= value; }
        }

        /// <summary>
        /// An event triggered when auto advance event is triggered.
        /// </summary>
        public event AutoAdvanceEvent OnAutoAdvance
        {
            add { this.onAutoAdvance += value; }
            remove { this.onAutoAdvance -= value; }
        }

        /// <summary>
        /// An event triggered when pre-count event is triggered.
        /// </summary>
        public event PreCountEvent OnPreCount
        {
            add { this.onPreCount += value; }
            remove { this.onPreCount -= value; }
        }

        /// <summary>
        /// An event triggered when previous button is clicked.
        /// </summary>
        public event RoutedEventHandler PreviousButtonClick
        {
            add { this.PreviousButton.Click += value; }
            remove { this.PreviousButton.Click -= value; }
        }

        /// <summary>
        /// An event triggered when print button is clicked.
        /// </summary>
        public event RoutedEventHandler PrintButtonClick
        {
            add { this.PrintButton.Click += value; }
            remove { this.PrintButton.Click -= value; }
        }

        /// <summary>
        /// An event triggered when save button is clicked.
        /// </summary>
        public event RoutedEventHandler SaveButtonClick
        {
            add { this.SaveButton.Click += value; }
            remove { this.SaveButton.Click -= value; }
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
        /// Gets a value indicating whether or not Auto Advance is randomly advanced.
        /// </summary>
        public bool RandomEnabled
        {
            get { return this.RandomToggle.IsOn; }
            private set { this.RandomToggle.IsOn = value; }
        }

        /// <summary>
        /// Gets a value indicating whether or not Auto Advance is advanced to all folder.
        /// </summary>
        public bool GlobalEnabled
        {
            get { return this.GlobalToggle.IsOn; }
            private set { this.GlobalToggle.IsOn = value; }
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
            this.nextButtonClick?.Invoke(this, e);
        }

        private void Timer_Tick(object sender, object e)
        {
            this.counter--;
            if (this.counter < 5 && this.counter > 0 && this.PrecountToggle.IsOn)
            {
                this.onPreCount?.Invoke(this);
            }

            if (this.counter == 0)
            {
                this.onAutoAdvance?.Invoke(this);

                this.timer.Stop();
            }
        }
    }
}