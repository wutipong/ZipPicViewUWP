// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ZipPicViewUWP
{
    using Windows.UI.Xaml.Controls;

    public sealed partial class PasswordDialog : ContentDialog
    {
        public PasswordDialog()
        {
            this.InitializeComponent();
        }

        public string Password
        {
            get { return this.passwordBox.Password; }
        }
    }
}
