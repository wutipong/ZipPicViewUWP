﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using ZipPicViewUWP.MediaProvider;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MangaReader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ViewerPage : Page
    {
        private AbstractMediaProvider Provider { get; set; }
        private ApplicationData ApplicationData => Windows.Storage.ApplicationData.Current;
        private int CurrentIndex { get; set; }
        private string[] imageFiles;

        public ViewerPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var file = this.ApplicationData.LocalSettings.Values["selected file"] as string;
            var folder = await this.GetLibraryFolder();

            Provider = await OpenFile(await folder.GetFileAsync(file));

            var (files, error) = await Provider.GetAllFileEntries();
            if (error != null)
            {
                return;
            }

            imageFiles = files;
            ChangeImage(0);

            NameText.Text = file;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Provider.Dispose();
        }

        private async Task<AbstractMediaProvider> OpenFile(StorageFile selected)
        {
            if (selected == null)
            {
                this.IsEnabled = true;
                return null;
            }

            if (selected.FileType == ".pdf")
            {
                var provider = new PdfMediaProvider(selected);
                await provider.Load();

                return provider;
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
                        return null;
                    }

                    return ArchiveMediaProvider.Create(stream, archive);
                }
                catch (Exception err)
                {
                    return null;
                }
            }
        }

        private async Task<StorageFolder> GetLibraryFolder()
        {
            try
            {
                this.ApplicationData.LocalSettings.Values.TryGetValue("path token", out var token);
                var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token as string);

                return folder;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage));
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            int index = CurrentIndex;
            index--;

            if (index < 0)
            {
                index = imageFiles.Length - 1;
            }

            ChangeImage(index);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int index = CurrentIndex;
            index++;

            if (index == imageFiles.Length)
            {
                index = 0;
            }

            ChangeImage(index);
        }

        private async void ChangeImage(int index)
        {
            var file = imageFiles[index];

            var (stream, error) = await Provider.OpenEntryAsRandomAccessStreamAsync(file);
            if (error != null)
            {
                return;
            }

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync();
            bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);

            Image.Source = source;

            CurrentIndex = index;
        }
    }
}