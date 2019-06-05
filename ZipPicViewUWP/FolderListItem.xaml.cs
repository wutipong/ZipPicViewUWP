// <copyright file="FolderListItem.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Imaging;
    using ZipPicViewUWP.Utility;

    /// <summary>
    /// Folder list item control.
    /// </summary>
    public sealed partial class FolderListItem : UserControl
    {
        private string entry;
        private SoftwareBitmapSource imageSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderListItem"/> class.
        /// </summary>
        public FolderListItem()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the actual folder value.
        /// </summary>
        public string FolderEntry
        {
            get => this.entry;
            set
            {
                this.entry = value;

                if (this.entry == MediaManager.Provider.Root)
                {
                    this.name.Text = "<ROOT>";
                    this.name.FontStyle = Windows.UI.Text.FontStyle.Oblique;
                }
                else
                {
                    int prefixCount = this.entry.Count(c => c == MediaManager.Provider.Separator);
                    this.name.Text = value.ExtractFilename();
                }
            }
        }

        /// <summary>
        /// Gets or sets the tree portion of the control.
        /// </summary>
        public string TreeText
        {
            get => this.TreeTextBlock.Text;
            set => this.TreeTextBlock.Text = value;
        }

        /// <summary>
        /// Gets or sets the thumbnail image source.
        /// </summary>
        public SoftwareBitmapSource ImageSource
        {
            get => this.imageSource;

            set
            {
                this.imageSource = value;
                if (value == null)
                {
                    this.image.Visibility = Visibility.Collapsed;
                    this.folderIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    this.image.Source = value;
                    this.image.Visibility = Visibility.Visible;
                    this.folderIcon.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Build a tree string from the input entries array.
        /// </summary>
        /// <param name="entries">Input entries.</param>
        /// <returns>Tree string array.</returns>
        public static string[] BuildTreeString(string[] entries)
        {
            List<char>[] tree = new List<char>[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                tree[i] = new List<char>();
                if (entries[i] == MediaManager.Provider.Root)
                {
                    continue;
                }

                int currentLevel = entries[i].Count((c) => c == MediaManager.Provider.Separator);

                for (int j = 0; j < currentLevel; j++)
                {
                    tree[i].Add(' ');
                }

                tree[i].Add('└');

                for (int j = i - 1; j > 0; j--)
                {
                    int lastLevel = entries[j].Count((c) => c == MediaManager.Provider.Separator);
                    if (lastLevel > currentLevel)
                    {
                        tree[j][currentLevel] = '│';
                    }

                    if (lastLevel == currentLevel)
                    {
                        tree[j][currentLevel] = '├';
                        break;
                    }

                    if (lastLevel < currentLevel)
                    {
                        break;
                    }
                }
            }

            string[] output = new string[entries.Length];
            for (int i = 0; i < output.Length; i++)
            {
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < tree[i].Count; j++)
                {
                    // sb.Append(" ");
                    sb.Append(tree[i][j]);
                }

                output[i] = sb.ToString();
            }

            return output;
        }

        /// <summary>
        /// Set the thumbnail image source asynchronously.
        /// </summary>
        /// <param name="source">image source.</param>
        public async void SetImageSourceAsync(SoftwareBitmapSource source)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.ImageSource = source;
            });
        }

        /// <summary>
        /// Release memory used by this control.
        /// </summary>
        public void Release()
        {
            if (this.ImageSource != null)
            {
                this.ImageSource.Dispose();
                this.ImageSource = null;
            }
        }
    }
}