﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.AccessCache;
using Windows.Storage;
using Windows.Storage.Search;
using LiteDB;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using ZipPicViewUWP.MediaProvider;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MangaReader
{
    internal class MangaData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Rating { get; set; } = -1;
        public string ThumbID { get; set; }

        public DateTime DateCreated { get; set; }
    }

    public enum SortBy
    {
        Name,
        CreateDate,
        CreateDateDesc,
        Rating
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ApplicationData ApplicationData => Windows.Storage.ApplicationData.Current;
        private IEnumerable<Thumbnail> thumbnails;
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var folder = await GetLibraryFolder();
            if (folder == null)
            {
                return;
            }

            NameText.Text = folder.Name;

            await DBManager.Open(folder);
            try
            {
                LoadingControl.IsLoading = true;
                await DBManager.RefreshMangaData();
            }
            finally
            {
                DBManager.Release();
                LoadingControl.IsLoading = false;
            }

            await RefreshMangaData();

            ItemGrid.ItemClick += ItemGrid_ItemClick;
        }

        private void ItemGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as Thumbnail;

            this.ApplicationData.LocalSettings.Values["selected file"] = item.TitleText;
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(ViewerPage));

            return;
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(SettingPage));
        }

        private async Task RefreshMangaData()
        {
            if (ItemGrid == null)
                return;

            LoadingControl.IsLoading = true;
            
            await DBManager.Open();
            try
            {
                var tempList = new List<Thumbnail>();

                foreach (var data in DBManager.GetAllData())
                {
                    SoftwareBitmapSource source = new SoftwareBitmapSource();
                    using (var irs = new InMemoryRandomAccessStream())
                    {
                        var stream = irs.AsStream();

                        DBManager.DownloadFile(data.ThumbID, stream);
                        stream.Flush();

                        irs.Seek(0);

                        if (irs.Size > 0)
                        {
                            var decoder = await BitmapDecoder.CreateAsync(irs);

                            var bitmap = await decoder.GetSoftwareBitmapAsync();
                            bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                            await source.SetBitmapAsync(bitmap);
                        }
                    }

                    var thumbnail = new Thumbnail()
                    {
                        TitleText = data.Name,
                        Rating = data.Rating,
                        Source = source,
                        CreateDate = data.DateCreated,
                    };

                    tempList.Add(thumbnail);
                }

                thumbnails = tempList;

                Resort(SortBy.Name);
            }
            finally
            {
                DBManager.Release();
                LoadingControl.IsLoading = false;
            }
        }

        private void Resort(SortBy sortBy)
        {
            if (ItemGrid == null)
                return;

            ItemGrid.Items.Clear();
            switch (sortBy)
            {
                case SortBy.Rating:
                    thumbnails = thumbnails.OrderByDescending(thumbnail => thumbnail.Rating);
                    break;

                case SortBy.CreateDate:
                    thumbnails = thumbnails.OrderBy(thumbnail => thumbnail.CreateDate);
                    break;

                case SortBy.CreateDateDesc:
                    thumbnails = thumbnails.OrderByDescending(thumbnail => thumbnail.CreateDate);
                    break;

                default:
                    thumbnails = thumbnails.OrderBy(thumbnail => thumbnail.Name);
                    break;
            }

            foreach (var thumbnail in thumbnails)
            {
                ItemGrid.Items.Add(thumbnail);
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

        private void SortByDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SortBy sortBy = SortBy.Name;
            switch (SortByDropDown.SelectedIndex)
            {
                case 0:
                    sortBy = SortBy.Name;
                    break;

                case 1:
                    sortBy = SortBy.CreateDate;
                    break;

                case 2:
                    sortBy = SortBy.CreateDateDesc;
                    break;

                case 3:
                    sortBy = SortBy.Rating;
                    break;
            }

            Resort(sortBy);
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();

            if (folder == null)
                return;

            NameText.Text = folder.Path;
            this.ApplicationData.LocalSettings.Values.TryGetValue("path token", out var token);

            if (token == null)
            {
                token = StorageApplicationPermissions.FutureAccessList.Add(folder);
                this.ApplicationData.LocalSettings.Values["path token"] = token;
            }
            else
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(token as string, folder);
            }

            this.ApplicationData.LocalSettings.Values["path"] = folder.Path;

            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage));
        }
    }
}