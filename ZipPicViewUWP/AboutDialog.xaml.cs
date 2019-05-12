// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ZipPicViewUWP
{
    using System;
    using Windows.ApplicationModel;
    using Windows.Storage;
    using Windows.UI.Xaml.Controls;

    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            this.Version.Text = (package.IsDevelopmentMode ? "(Debug)" : string.Empty) +
                string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

            this.Initialize();
        }

        public async void Initialize()
        {
            this.ReleaseNote.Text =
                await FileIO.ReadTextAsync(await Package.Current.InstalledLocation.GetFileAsync(@"Assets\Release.md"));
        }
    }
}