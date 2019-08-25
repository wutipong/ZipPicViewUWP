using System;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MangaReader
{
    public sealed partial class Thumbnail : UserControl
    {
        public Thumbnail()
        {
            this.InitializeComponent();
        }

        public string TitleText
        {
            get => Title.Text;
            set => Title.Text = value;
        }

        public int Rating
        {
            get => (int)RatingControl.Value;
            set => RatingControl.Value = value;
        }

        public ImageSource Source
        {
            get => CoverImage.Source;
            set => CoverImage.Source = value;
        }

        public event TypedEventHandler<Thumbnail, object> RatingChanged;

        private void RatingControl_ValueChanged(RatingControl sender, object args)
        {
            RatingChanged?.Invoke(this, (int)sender.Value);
        }
    }
}