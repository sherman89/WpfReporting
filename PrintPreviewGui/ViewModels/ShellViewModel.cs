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
using Sherman.WpfReporting.Gui.Reports;
using Sherman.WpfReporting.Lib;
using Sherman.WpfReporting.Lib.Models;

namespace Sherman.WpfReporting.Gui.ViewModels
{
    public class ShellViewModel : Screen
    {
        private readonly Paginator paginator;
        private readonly Printing printing;

        public ShellViewModel()
        {
            paginator = new Paginator();
            printing = new Printing();

            SupportedPrinters = new ObservableCollection<PrinterModel>();
            SupportedPageSizes = new ObservableCollection<PageSizeModel>();
            SupportedPageOrientations = new ObservableCollection<PageOrientationModel>();
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            InitializePrinters();
            LoadPrinterPageSizes();
            LoadPrinterPageOrientations();

            return base.OnActivateAsync(cancellationToken);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            try
            {
                xpsDocument?.Close();
                File.Delete(xpsDocument?.Uri.AbsolutePath);
            }
            catch
            {
            }

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

        private void InitializePrinters()
        {
            var printers = printing.GetPrinters();
            SupportedPrinters = new ObservableCollection<PrinterModel>(printers);
            selectedPrinter = SupportedPrinters.FirstOrDefault();
        }

        public void LoadPrinterPageSizes()
        {
            var currentSelectedPage = SelectedPageSize?.PageMediaSize?.PageMediaSizeName;

            SupportedPageSizes.Clear();
            foreach (var pageMediaSize in SelectedPrinter.PageSizeCapabilities)
            {
                SupportedPageSizes.Add(new PageSizeModel(pageMediaSize));
            }

            SelectedPageSize = SupportedPageSizes.SingleOrDefault(ps => ps.PageMediaSize.PageMediaSizeName == currentSelectedPage) ?? SupportedPageSizes.First();
        }

        public void LoadPrinterPageOrientations()
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
            if (selectedReport > 0 && SelectedPageSize != null)
            {
                await LoadReport(selectedReport);
            }
        }

        private int selectedReport;
        public async Task LoadReport(int reportNumber)
        {
            if (xpsDocument != null)
            {
                xpsDocument.Close();
                File.Delete(xpsDocument.Uri.AbsolutePath);
                xpsDocument = null;
            }

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

                var pages = await paginator.Paginate(reportFactory, pageSize, desiredMargin, CancellationToken.None);
                var fixedDocument = paginator.GetFixedDocumentFromPages(pages, pageSize);

                // We could simply now assign the fixedDocument to GeneratedDocument
                // But then for some reason the DocumentViewer search feature breaks
                // The solution is to create an XPS file first and get the FixedDocumentSequence
                // from it and then use that in the DocumentViewer

                xpsDocument = printing.GetXpsDocumentFromFixedDocument(fixedDocument);
                GeneratedDocument = xpsDocument.GetFixedDocumentSequence();
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
