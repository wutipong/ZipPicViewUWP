using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Printing;

namespace ZipPicViewUWP
{
    class PrintHelper
    {
        private PrintPage printPage;
        private PrintDocument printDocument;
        private IPrintDocumentSource printDocumentSource;

        private PrintPanel.LayoutOption printLayout;
        public BitmapImage BitmapImage { get; set; }

        private Page caller;

        private string title;

        public PrintHelper(Page page)
        {
            caller = page;
        }

        public void RegisterForPrinting()
        {
            printDocument = new PrintDocument();
            printDocumentSource = printDocument.DocumentSource;
            printDocument.Paginate += CreatePrintPreviewPages;
            printDocument.GetPreviewPage += GetPrintPreviewPage;
            printDocument.AddPages += AddPrintPages;

            PrintManager printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested += PrintTaskRequested;
        }


        private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
        {
            PrintTask printTask = null;

            printTask = e.Request.CreatePrintTask(title, sourceRequested =>
            {
                sourceRequested.SetSource(printDocumentSource);
            });

            var printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
            var displayedOptions = printDetailedOptions.DisplayedOptions;

            var alignOption = printDetailedOptions.CreateItemListOption("LayoutOption", "Layout Option");

            alignOption.AddItem("Center", "Center");
            alignOption.AddItem("AlignLeftOrTop", "Align to the Left or to the Top");
            alignOption.AddItem("AlignRightOrBottom", "Align to the right or to the bottom");

            printLayout = PrintPanel.LayoutOption.Centered;
            displayedOptions.Clear();

            displayedOptions.Add(StandardPrintTaskOptions.Copies);
            displayedOptions.Add("LayoutOption");
            displayedOptions.Add(StandardPrintTaskOptions.MediaSize);
            displayedOptions.Add(StandardPrintTaskOptions.Orientation);
            displayedOptions.Add(StandardPrintTaskOptions.ColorMode);
            displayedOptions.Add(StandardPrintTaskOptions.PrintQuality);

            printDetailedOptions.OptionChanged += printDetailedOptions_OptionChanged;
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
                            printLayout = PrintPanel.LayoutOption.Centered;
                            break;

                        case "AlignLeftOrTop":
                            printLayout = PrintPanel.LayoutOption.AlignLeftOrTop;
                            break;

                        case "AlignRightOrBottom":
                            printLayout = PrintPanel.LayoutOption.AlignRightOrBottom;
                            break;

                    }
                    invalidatePreview = true;
                    break;
            }
            if (invalidatePreview)
            {
                await caller.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    printDocument.InvalidatePreview();
                });
            }
        }

        private void AddPrintPages(object sender, AddPagesEventArgs e)
        {
            PrintDocument printDoc = (PrintDocument)sender;
            printDoc.AddPage(printPage);
            printDoc.AddPagesComplete();
        }

        private void GetPrintPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            PrintDocument printDoc = (PrintDocument)sender;
            printPage = new PrintPage()
            {
                LayoutOption = printLayout,
                ImageSource = BitmapImage,
            };
            printDoc.SetPreviewPage(e.PageNumber, printPage);
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

