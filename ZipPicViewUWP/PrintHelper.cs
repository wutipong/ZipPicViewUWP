namespace ZipPicViewUWP
{
    using System;
    using System.Threading.Tasks;
    using Windows.Graphics.Printing;
    using Windows.Graphics.Printing.OptionDetails;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Imaging;
    using Windows.UI.Xaml.Printing;

    public class PrintHelper
    {
        private PrintPage printPage;
        private PrintDocument printDocument;
        private IPrintDocumentSource printDocumentSource;
        private PrintPanel.LayoutOption printLayout;
        private Page caller;
        private string title;

        public PrintHelper(Page page)
        {
            this.caller = page;
        }

        public BitmapImage BitmapImage { get; set; }

        public void RegisterForPrinting()
        {
            this.printDocument = new PrintDocument();
            this.printDocumentSource = this.printDocument.DocumentSource;
            this.printDocument.Paginate += this.CreatePrintPreviewPages;
            this.printDocument.GetPreviewPage += this.GetPrintPreviewPage;
            this.printDocument.AddPages += this.AddPrintPages;

            PrintManager printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested += this.PrintTaskRequested;
        }

        private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
        {
            PrintTask printTask = null;

            printTask = e.Request.CreatePrintTask(this.title, sourceRequested =>
            {
                sourceRequested.SetSource(this.printDocumentSource);
            });

            var printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
            var displayedOptions = printDetailedOptions.DisplayedOptions;

            var alignOption = printDetailedOptions.CreateItemListOption("LayoutOption", "Layout Option");

            alignOption.AddItem("Center", "Center");
            alignOption.AddItem("AlignLeftOrTop", "Align to the Left or to the Top");
            alignOption.AddItem("AlignRightOrBottom", "Align to the right or to the bottom");

            this.printLayout = PrintPanel.LayoutOption.Centered;
            displayedOptions.Clear();

            displayedOptions.Add(StandardPrintTaskOptions.Copies);
            displayedOptions.Add("LayoutOption");
            displayedOptions.Add(StandardPrintTaskOptions.MediaSize);
            displayedOptions.Add(StandardPrintTaskOptions.Orientation);
            displayedOptions.Add(StandardPrintTaskOptions.ColorMode);
            displayedOptions.Add(StandardPrintTaskOptions.PrintQuality);

            printDetailedOptions.OptionChanged += this.printDetailedOptions_OptionChanged;
        }

        private async void printDetailedOptions_OptionChanged(PrintTaskOptionDetails sender, PrintTaskOptionChangedEventArgs args)
        {
            var invalidatePreview = false;

            switch (args.OptionId)
            {
                case "LayoutOption":

                    switch (sender.Options["LayoutOption"].Value as string)
                    {
                        case "Center":
                            this.printLayout = PrintPanel.LayoutOption.Centered;
                            break;

                        case "AlignLeftOrTop":
                            this.printLayout = PrintPanel.LayoutOption.AlignLeftOrTop;
                            break;

                        case "AlignRightOrBottom":
                            this.printLayout = PrintPanel.LayoutOption.AlignRightOrBottom;
                            break;
                    }

                    invalidatePreview = true;
                    break;
            }

            if (invalidatePreview)
            {
                await this.caller.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.printDocument.InvalidatePreview();
                });
            }
        }

        private void AddPrintPages(object sender, AddPagesEventArgs e)
        {
            PrintDocument printDoc = (PrintDocument)sender;
            printDoc.AddPage(this.printPage);
            printDoc.AddPagesComplete();
        }

        private void GetPrintPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            PrintDocument printDoc = (PrintDocument)sender;
            this.printPage = new PrintPage()
            {
                LayoutOption = this.printLayout,
                ImageSource = this.BitmapImage,
            };
            printDoc.SetPreviewPage(e.PageNumber, this.printPage);
        }

        private void CreatePrintPreviewPages(object sender, PaginateEventArgs e)
        {
            PrintDocument printDoc = (PrintDocument)sender;

            printDoc.SetPreviewPageCount(1, PreviewPageCountType.Intermediate);
        }

        public async Task<bool> ShowPrintUIAsync(string title)
        {
            this.title = title;
            return await PrintManager.ShowPrintUIAsync();
        }

    }
}

