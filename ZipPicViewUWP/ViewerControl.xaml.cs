using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ZipPicViewUWP.Utility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ZipPicViewUWP
{
    public sealed partial class ViewerControl : UserControl
    {
        private static readonly TimeSpan[] advanceDurations =
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
                new TimeSpan(1, 0, 0)
            };

        private DispatcherTimer timer;

        private int counter;

        public delegate void PreCountEvent(object sender);

        private PreCountEvent onPreCount;

        public delegate void AutoAdvanceEvent(object sender);

        private AutoAdvanceEvent onAutoAdvance;

        private RoutedEventHandler nextButtonClick;

        public event PreCountEvent OnPreCount
        {
            add { onPreCount += value; }
            remove { onPreCount -= value; }
        }

        public event AutoAdvanceEvent OnAutoAdvance
        {
            add { onAutoAdvance += value; }
            remove { onAutoAdvance -= value; }
        }

        public bool IsAutoAdvanceRandomly { get { return RandomToggle.IsOn; } }

        public ViewerControl()
        {
            this.InitializeComponent();
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);

            NextButton.Click += NextButton_Click;

            DurationList.Items.Clear();
            var oneMinute = TimeSpan.FromMinutes(1.00);
            foreach (var duration in advanceDurations)
            {
                var durationStr = duration < oneMinute ?
                    String.Format("{0} Second(s)", duration.Seconds) :
                    String.Format("{0}:{1:00} Minute(s)", (int)duration.TotalMinutes, duration.Seconds);

                DurationList.Items.Add(durationStr);
            }

            var applicationData = Windows.Storage.ApplicationData.Current;
            applicationData.LocalSettings.Values.TryGetValue("durationIndex", out var index);
            DurationList.SelectedIndex = index == null ? 0 : (int)index;

            DurationList.SelectionChanged += (_, __) =>
            {
                applicationData.LocalSettings.Values["durationIndex"] = DurationList.SelectedIndex;
            };

            applicationData.LocalSettings.Values.TryGetValue("precount", out var precount);
            PrecountToggle.IsOn = precount == null ? false : (bool)precount;
            PrecountToggle.Toggled += (_, __) =>
            {
                applicationData.LocalSettings.Values["precount"] = PrecountToggle.IsOn;
            };

            if (!Windows.Graphics.Printing.PrintManager.IsSupported())
            {
                PrintButton.Visibility = Visibility.Collapsed;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            nextButtonClick?.Invoke(this, e);
        }

        private void Timer_Tick(object sender, object e)
        {
            counter--;
            if (counter < 5 && counter > 0 && PrecountToggle.IsOn) { onPreCount?.Invoke(this); }
            if (counter == 0)
            {
                onAutoAdvance?.Invoke(this);

                timer.Stop();
            }
        }

        private string filename;

        public string Filename
        {
            get { return filename; }
            set
            {
                filename = value;
                FilenameTextBlock.Text = filename.Ellipses(70);
            }
        }

        public string DimensionText
        {
            get { return OriginalDimension.Text; }
            set { OriginalDimension.Text = value; }
        }

        public event RoutedEventHandler NextButtonClick
        {
            add { nextButtonClick += value; }
            remove { nextButtonClick -= value; }
        }

        public event RoutedEventHandler PreviousButtonClick
        {
            add { PreviousButton.Click += value; }
            remove { PreviousButton.Click -= value; }
        }

        public event RoutedEventHandler CloseButtonClick
        {
            add { CloseButton.Click += value; }
            remove { CloseButton.Click -= value; }
        }

        public event RoutedEventHandler SaveButtonClick
        {
            add { SaveButton.Click += value; }
            remove { SaveButton.Click -= value; }
        }

        public event RoutedEventHandler PrintButtonClick
        {
            add { PrintButton.Click += value; }
            remove { PrintButton.Click -= value; }
        }

        public event RoutedEventHandler CopyButtonClick
        {
            add { CopyButton.Click += value; }
            remove { CopyButton.Click -= value; }
        }

        public bool? AutoEnabled
        {
            get { return AutoButton.IsChecked; }
            set { AutoButton.IsChecked = value; }
        }

        private void AutoButton_Checked(object sender, RoutedEventArgs e)
        {
            AutoButton.Content = new SymbolIcon(Symbol.Pause);
            AutoDurationButton.IsEnabled = false;
            
            SaveButton.IsEnabled = false;
            ResetCounter();
        }

        public void ResetCounter()
        {
            if (AutoButton.IsChecked == false)
                return;
            counter = (int)advanceDurations[DurationList.SelectedIndex].TotalSeconds;
            timer.Start();
        }

        private void AutoButton_Unchecked(object sender, RoutedEventArgs e)
        {
            AutoButton.Content = new SymbolIcon(Symbol.Play);
            AutoDurationButton.IsEnabled = true;
            timer.Stop();
            SaveButton.IsEnabled = true;
        }
    }
}