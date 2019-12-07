using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Printing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using Caliburn.Micro;
using Sherman.WpfReporting.Gui.DialogManagement;
using Sherman.WpfReporting.Gui.Reports;
using Sherman.WpfReporting.Gui.ViewModels.Dialogs;
using Sherman.WpfReporting.Lib;
using Sherman.WpfReporting.Lib.Models;

namespace Sherman.WpfReporting.Gui.ViewModels
{
    public class MainViewModel : Screen
    {
        private readonly IPaginator paginator;
        private readonly IPrinting printing;
        private readonly IDialogService dialogService;
        private readonly ProgressDialogViewModel progressDialog;

        public MainViewModel(IPaginator paginator, IPrinting printing, IDialogService dialogService, ProgressDialogViewModel progressDialog)
        {
            this.paginator = paginator;
            this.printing = printing;
            this.dialogService = dialogService;
            this.progressDialog = progressDialog;

            SupportedPrinters = new ObservableCollection<PrinterModel>();
            SupportedPageSizes = new ObservableCollection<PageSizeModel>();
            SupportedPageOrientations = new ObservableCollection<PageOrientationModel>();
        }

        public MainViewModel()
        {
            if (!Execute.InDesignMode)
            {
                throw new InvalidOperationException("Parameterless constructor meant to be called only in design mode!");
            }

            SupportedPrinters = new ObservableCollection<PrinterModel>();
            SupportedPageSizes = new ObservableCollection<PageSizeModel>();
            SupportedPageOrientations = new ObservableCollection<PageOrientationModel>();
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // Fire and forget
            Execute.OnUIThreadAsync(() => 
            { 
                return InitializePrinters(cancellationToken);
            });

            return base.OnActivateAsync(cancellationToken);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            CleanXpsDocumentResources();

            return base.OnDeactivateAsync(close, cancellationToken);
        }

        public ObservableCollection<PrinterModel> SupportedPrinters { get; set; }

        private PrinterModel selectedPrinter;
        public PrinterModel SelectedPrinter
        {
            get => selectedPrinter;
            set
            {
                selectedPrinter = value;
                NotifyOfPropertyChange(() => SelectedPrinter);
            }
        }

        public ObservableCollection<PageSizeModel> SupportedPageSizes { get; set; }

        private PageSizeModel selectedPageSize;
        public PageSizeModel SelectedPageSize
        {
            get => selectedPageSize;
            set
            {
                selectedPageSize = value;
                NotifyOfPropertyChange(() => SelectedPageSize);
            }
        }

        public ObservableCollection<PageOrientationModel> SupportedPageOrientations { get; set; }

        private PageOrientationModel selectedPageOrientation;
        public PageOrientationModel SelectedPageOrientation
        {
            get => selectedPageOrientation;
            set
            {
                selectedPageOrientation = value;
                NotifyOfPropertyChange(() => SelectedPageOrientation);
            }
        }

        public void LoadPrinterCapabilities()
        {
            LoadPrinterPageSizes();
            LoadPrinterPageOrientations();
        }

        private async Task InitializePrinters(CancellationToken cancellationToken)
        {
            try
            {
                // Start potentially long running task on separate thread
                // For instance retrieving network printers could take some time
                var getPrintersTask = Task.Run(() =>
                {
                    return printing.GetPrinters();
                }, cancellationToken);

                // If task is taking longer than magic number, show progress indicator
                var isCompleted = getPrintersTask.Wait(300);
                if (!isCompleted)
                {
                    dialogService.Open(progressDialog);
                }

                var printers = await getPrintersTask;

                SupportedPrinters.Clear();
                foreach (var printer in printers)
                {
                    SupportedPrinters.Add(printer);
                }

                SelectedPrinter = SupportedPrinters.FirstOrDefault();
            }
            finally
            {
                dialogService.Close(progressDialog);
            }
        }

        private void LoadPrinterPageSizes()
        {
            if (SelectedPrinter != null)
            {
                var currentSelectedPage = SelectedPageSize?.PageMediaSize?.PageMediaSizeName;

                SupportedPageSizes.Clear();
                foreach (var pageMediaSize in SelectedPrinter.PageSizeCapabilities)
                {
                    SupportedPageSizes.Add(new PageSizeModel(pageMediaSize));
                }

                SelectedPageSize = SupportedPageSizes.SingleOrDefault(ps => ps.PageMediaSize.PageMediaSizeName == currentSelectedPage) ?? SupportedPageSizes.First();
            }
        }

        private void LoadPrinterPageOrientations()
        {
            if(SelectedPrinter != null)
            {
                var currentSelectedOrientation = SelectedPageOrientation?.PageOrientation;

                SupportedPageOrientations.Clear();
                foreach (var pageOrientation in SelectedPrinter.PageOrientationCapabilities)
                {
                    SupportedPageOrientations.Add(new PageOrientationModel(pageOrientation));
                }

                var defaultOrientation = SupportedPageOrientations.SingleOrDefault(po => po.PageOrientation == PageOrientation.Portrait) ?? SupportedPageOrientations.First();
                SelectedPageOrientation = SupportedPageOrientations.SingleOrDefault(po => po.PageOrientation == currentSelectedOrientation) ?? defaultOrientation;
            }
        }

        /// <summary>
        /// XPS version of the FixedDocument. 
        /// </summary>
        private XpsDocument xpsDocument;

        private IDocumentPaginatorSource generatedDocument;
        public IDocumentPaginatorSource GeneratedDocument
        {
            get => generatedDocument;
            set
            {
                generatedDocument = value;
                NotifyOfPropertyChange(() => GeneratedDocument);
            }
        }

        public async Task LoadReport()
        {
            if (selectedReport > 0 && 
                SelectedPrinter != null && 
                SelectedPageSize != null &&
                SelectedPageOrientation != null)
            {
                await LoadReport(selectedReport);
            }
        }

        private int selectedReport;
        public async Task LoadReport(int reportNumber)
        {
            dialogService.Open(progressDialog);

            try
            {
                CleanXpsDocumentResources();
                selectedReport = reportNumber;

                Func<UIElement> reportFactory;
                switch (reportNumber)
                {
                    case 1:
                        reportFactory = () => new TestReport1();
                        break;
                    case 2:
                        reportFactory = () => new TestReport2();
                        break;
                    case 3:
                        reportFactory = () => new TestReport3();
                        break;
                    default:
                        throw new ArgumentException($"Invalid value for parameter {nameof(reportNumber)}");
                }

                var printTicket = printing.GetPrintTicket(SelectedPrinter.FullName, SelectedPageSize.PageMediaSize, SelectedPageOrientation.PageOrientation);
                var printCapabilities = printing.GetPrinterCapabilitiesForPrintTicket(printTicket, SelectedPrinter.FullName);

                if (printCapabilities.OrientedPageMediaWidth.HasValue && printCapabilities.OrientedPageMediaHeight.HasValue)
                {
                    var pageSize = new Size(printCapabilities.OrientedPageMediaWidth.Value, printCapabilities.OrientedPageMediaHeight.Value);

                    var desiredMargin = new Thickness(15);
                    var printerMinMargins = printing.GetMinimumPageMargins(printCapabilities);
                    AdjustMargins(ref desiredMargin, printerMinMargins);

                    var pages = await paginator.PaginateAsync(reportFactory, pageSize, desiredMargin, CancellationToken.None);
                    var fixedDocument = paginator.GetFixedDocumentFromPages(pages, pageSize);

                    // We could simply now assign the fixedDocument to GeneratedDocument
                    // But then for some reason the DocumentViewer search feature breaks
                    // The solution is to create an XPS file first and get the FixedDocumentSequence
                    // from it and then use that in the DocumentViewer

                    xpsDocument = printing.GetXpsDocumentFromFixedDocument(fixedDocument);
                    GeneratedDocument = xpsDocument.GetFixedDocumentSequence();
                }
            }
            finally
            {
                dialogService.Close(progressDialog);
            }
        }

        private void CleanXpsDocumentResources()
        {
            if (xpsDocument != null)
            {
                try
                {
                    xpsDocument.Close();
                    File.Delete(xpsDocument.Uri.AbsolutePath);
                    xpsDocument = null;
                }
                catch
                {
                }
            }
        }

        private static void AdjustMargins(ref Thickness pageMargins, Thickness minimumMargins)
        {
            if (pageMargins.Left < minimumMargins.Left)
            {
                pageMargins.Left = minimumMargins.Left;
            }

            if (pageMargins.Top < minimumMargins.Top)
            {
                pageMargins.Top = minimumMargins.Top;
            }

            if (pageMargins.Right < minimumMargins.Right)
            {
                pageMargins.Right = minimumMargins.Right;
            }

            if (pageMargins.Bottom < minimumMargins.Bottom)
            {
                pageMargins.Bottom = minimumMargins.Bottom;
            }
        }
    }
}
