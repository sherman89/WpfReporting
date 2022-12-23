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
        private readonly IDispatcher dispatcher;

        private bool allowOnPrinterChanged = true;
        private bool allowOnPageOrientationChanged = true;
        private bool allowOnPageSizeChanged = true;

        public MainViewModel(IPaginator paginator, IPrinting printing, IDialogService dialogService, IDispatcher dispatcher)
        {
            this.paginator = paginator;
            this.printing = printing;
            this.dialogService = dialogService;
            this.dispatcher = dispatcher;

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
                return Initialize(cancellationToken);
            });

            return base.OnActivateAsync(cancellationToken);
        }

        protected async override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            await base.OnDeactivateAsync(close, cancellationToken);
            CleanXpsDocumentResources();
        }

        public ObservableCollection<PrinterModel> SupportedPrinters { get; }

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

        public async Task OnPrinterChanged()
        {
            if (!allowOnPrinterChanged)
            {
                return;
            }

            // Updating printers supported page sizes and page oritentations will
            // the selected page/orientation to trigger the SelectionChanged event
            // from XAML, and it will happen twice, so we temporarily disable it
            // then reload the report manually after updating of UI is done.

            allowOnPageSizeChanged = false;
            allowOnPageOrientationChanged = false;

            LoadPrinterPageSizes();
            LoadPrinterPageOrientations();

            allowOnPageSizeChanged = true;
            allowOnPageOrientationChanged = true;

            if (selectedReport != null)
            {
                var progressDialog = new ProgressDialogViewModel(isCancellingAllowed: false);

                try
                {
                    await dialogService.OpenAsync(progressDialog, CancellationToken.None);

                    if (selectedReport != null)
                    {
                        await ReloadReport();
                    }
                }
                finally
                {
                    await dialogService.CloseAsync(progressDialog, CancellationToken.None);
                }
            }
        }

        public ObservableCollection<PageSizeModel> SupportedPageSizes { get; }

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

        public async Task OnPageSizeChanged()
        {
            if (!allowOnPageSizeChanged)
            {
                return;
            }

            if (selectedReport != null)
            {
                var progressDialog = new ProgressDialogViewModel(isCancellingAllowed: false);

                try
                {
                    await dialogService.OpenAsync(progressDialog, CancellationToken.None);
                    await ReloadReport();
                }
                finally
                {
                    await dialogService.CloseAsync(progressDialog, CancellationToken.None);
                }
            }
        }

        public ObservableCollection<PageOrientationModel> SupportedPageOrientations { get; }

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

        public async Task OnPageOrientationChanged()
        {
            if (!allowOnPageOrientationChanged)
            {
                return;
            }

            if (selectedReport != null)
            {
                var progressDialog = new ProgressDialogViewModel(isCancellingAllowed: false);

                try
                {
                    await dialogService.OpenAsync(progressDialog, CancellationToken.None);
                    await ReloadReport();
                }
                finally
                {
                    await dialogService.CloseAsync(progressDialog, CancellationToken.None);
                }
            }
        }

        private async Task Initialize(CancellationToken cancellationToken)
        {
            allowOnPrinterChanged = false;
            allowOnPageSizeChanged = false;
            allowOnPageOrientationChanged = false;

            var progressDialog = new ProgressDialogViewModel(isCancellingAllowed: false);
            bool openDialogCancelled = true;

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
                    await dialogService.OpenAsync(progressDialog, cancellationToken);
                    openDialogCancelled = false;
                }

                var printers = await getPrintersTask;

                SupportedPrinters.Clear();
                foreach (var printer in printers)
                {
                    SupportedPrinters.Add(printer);
                }

                SelectedPrinter = SupportedPrinters.FirstOrDefault();
                LoadPrinterPageSizes();
                LoadPrinterPageOrientations();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                allowOnPrinterChanged = true;
                allowOnPageSizeChanged = true;
                allowOnPageOrientationChanged = true;

                if (!openDialogCancelled)
                {
                    await dialogService.CloseAsync(progressDialog, cancellationToken);
                }
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
            if (SelectedPrinter != null)
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

        private async Task ReloadReport()
        {
            if (selectedReport != null &&
                SelectedPrinter != null &&
                SelectedPageSize != null &&
                SelectedPageOrientation != null)
            {
                await LoadReport(selectedReport, CancellationToken.None);
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
                NotifyOfPropertyChange(() => CanPrintDocument);
            }
        }

        private Func<UIElement> selectedReport;
        public async Task OnReportSelected(int reportNumber)
        {
            var progressDialog = new ProgressDialogViewModel(isCancellingAllowed: true);

            try
            {
                await dialogService.OpenAsync(progressDialog, CancellationToken.None);
                var dialogCancellationToken = progressDialog.DialogCancellationToken;

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
                    case 4:
                        reportFactory = () => new TestReport4();
                        break;
                    default:
                        throw new ArgumentException($"Invalid value for parameter {nameof(reportNumber)}");
                }

                await LoadReport(reportFactory, dialogCancellationToken);
                selectedReport = reportFactory;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await dialogService.CloseAsync(progressDialog, CancellationToken.None);
            }
        }       

        private async Task LoadReport(Func<UIElement> reportFactory, CancellationToken cancellationToken)
        {
            var printTicket = printing.GetPrintTicket(SelectedPrinter.FullName, SelectedPageSize.PageMediaSize, SelectedPageOrientation.PageOrientation);
            var printCapabilities = printing.GetPrinterCapabilitiesForPrintTicket(printTicket, SelectedPrinter.FullName);

            if (printCapabilities.OrientedPageMediaWidth.HasValue && printCapabilities.OrientedPageMediaHeight.HasValue)
            {
                var pageSize = new Size(printCapabilities.OrientedPageMediaWidth.Value, printCapabilities.OrientedPageMediaHeight.Value);

                var desiredMargin = new Thickness(15);
                var printerMinMargins = printing.GetMinimumPageMargins(printCapabilities);
                AdjustMargins(ref desiredMargin, printerMinMargins);

                var pages = await paginator.PaginateAsync(reportFactory, pageSize, desiredMargin, cancellationToken);
                var fixedDocument = paginator.GetFixedDocumentFromPages(pages, pageSize);

                // We now could simply assign the fixedDocument to GeneratedDocument
                // But then for some reason the DocumentViewer search feature breaks
                // The solution is to create an XPS file first and get the FixedDocumentSequence
                // from it and then use that in the DocumentViewer
                
                // Delete old XPS file first
                CleanXpsDocumentResources();

                xpsDocument = printing.GetXpsDocumentFromFixedDocument(fixedDocument);
                GeneratedDocument = xpsDocument.GetFixedDocumentSequence();
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
                }
                catch
                {
                }
                finally
                {
                    xpsDocument = null;
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

        public bool CanPrintDocument => GeneratedDocument != null;

        public async Task OnPrint()
        {
            var message = "Are you sure you want to print this document?";
            var confirmDialog = new ConfirmDialogViewModel(message, "Yes", "No");

            try
            {
                var print = await dialogService.AwaitModalAsync(confirmDialog, CancellationToken.None);

                if (print)
                {
                    await Print();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async Task Print()
        {
            var progressDialog = new ProgressDialogViewModel(isCancellingAllowed: false);

            try
            {
                await dialogService.OpenAsync(progressDialog, CancellationToken.None);

                // PrintDocument will block the UI thread, so progress dialog might not appear
                // Yield control back to the current dispatcher to give UI a chance to show it
                await dispatcher.Yield();

                var printTicket = printing.GetPrintTicket(SelectedPrinter.FullName, SelectedPageSize.PageMediaSize, SelectedPageOrientation.PageOrientation);
                printing.PrintDocument(SelectedPrinter.FullName, generatedDocument, "Hello from WPF!", printTicket);
            }
            finally
            {
                await dialogService.CloseAsync(progressDialog, CancellationToken.None);
            }
        }
    }
}
