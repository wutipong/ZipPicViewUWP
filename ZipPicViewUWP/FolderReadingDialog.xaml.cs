// <copyright file="FolderReadingDialog.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System.Globalization;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// A dialog to display when media provider is being read.
    /// </summary>
    public sealed partial class FolderReadingDialog : ContentDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FolderReadingDialog"/> class.
        /// </summary>
        public FolderReadingDialog()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        /// <summary>
        /// Gets or sets Maximum value of the progress bar.
        /// </summary>
        public double Maximum
        {
            get => this.Progress.Maximum;
            set
            {
                this.Progress.Maximum = value;
                this.MaximumText.Text = this.IsIndeterminate ? "-" : this.Maximum.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets or sets the current progress.
        /// </summary>
        public double Value
        {
            get => this.Progress.Value;
            set
            {
                this.Progress.Value = value;
                this.ValueText.Text = this.IsIndeterminate ? "-" : value.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the progress is indeterminate or not.
        /// </summary>
        public bool IsIndeterminate
        {
            get => this.Progress.IsIndeterminate;
            set
            {
                this.Progress.IsIndeterminate = value;
                this.MaximumText.Text = value ? "-" : this.Maximum.ToString(CultureInfo.InvariantCulture);
                this.ValueText.Text = value ? "-" : this.Value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}