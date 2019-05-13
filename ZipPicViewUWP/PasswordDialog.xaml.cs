// <copyright file="PasswordDialog.xaml.cs" company="Wutipong Wongsakuldej">
// Copyright (c) Wutipong Wongsakuldej. All rights reserved.
// </copyright>

namespace ZipPicViewUWP
{
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// A dialog class to fill archive's password.
    /// </summary>
    public sealed partial class PasswordDialog : ContentDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordDialog"/> class.
        /// </summary>
        public PasswordDialog()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets: Return the value of the password field.
        /// </summary>
        public string Password
        {
            get { return this.passwordBox.Password; }
        }
    }
}