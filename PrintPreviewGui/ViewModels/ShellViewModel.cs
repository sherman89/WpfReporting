using System;
using System.Collections.ObjectModel;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Printing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Caliburn.Micro;
using PrintPreviewGui.Reports;

namespace PrintPreviewGui.ViewModels
{
    public class ShellViewModel : Screen
    {
        private readonly ReportGenerator reportGenerator;
        private readonly Printing printing;

        public ShellViewModel()
        {
            reportGenerator = new ReportGenerator();
            printing = new Printing();

            SupportedPrinters = new ObservableCollection<PrinterViewModel>();
            SupportedPageSizes = new ObservableCollection<PageSizeViewModel>();
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            InitializePrinters();
            LoadPrinterPageSizes();

            return base.OnActivateAsync(cancellationToken);
        }

        public ObservableCollection<PrinterViewModel> SupportedPrinters { get; set; }

        private PrinterViewModel selectedPrinter;
        public PrinterViewModel SelectedPrinter
        {
            get => selectedPrinter;
            set
            {
                selectedPrinter = value;
                NotifyOfPropertyChange(() => SelectedPrinter);
            }
        }

        public ObservableCollection<PageSizeViewModel> SupportedPageSizes { get; set; }

        private PageSizeViewModel selectedPageSize;
        public PageSizeViewModel SelectedPageSize
        {
            get => selectedPageSize;
            set
            {
                selectedPageSize = value;
                NotifyOfPropertyChange(() => SelectedPageSize);
            }
        }

        private void InitializePrinters()
        {
            var printers = printing.GetPrinters();
            SupportedPrinters = new ObservableCollection<PrinterViewModel>(printers);
            selectedPrinter = SupportedPrinters.FirstOrDefault();
        }

        public void LoadPrinterPageSizes()
        {
            SupportedPageSizes.Clear();
            foreach (var pageMediaSize in SelectedPrinter.PageSizeCapabilities)
            {
                SupportedPageSizes.Add(new PageSizeViewModel(pageMediaSize));
            }

            var a4 = SupportedPageSizes.SingleOrDefault(ps => ps.PageMediaSize.PageMediaSizeName == PageMediaSizeName.ISOA4);
            SelectedPageSize = a4 ?? SupportedPageSizes.First();
        }

        private FixedDocument generatedDocument;
        public FixedDocument GeneratedDocument
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
            selectedReport = reportNumber;

            Func<FrameworkElement> reportFactory;
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

            var printTicket = printing.GetPrintTicket(SelectedPrinter.FullName, SelectedPageSize.PageMediaSize, PageOrientation.Portrait);
            var pc = printing.GetPrinterCapabilitiesForPrintTicket(printTicket, SelectedPrinter.FullName);

            if (pc.OrientedPageMediaWidth.HasValue && pc.OrientedPageMediaHeight.HasValue)
            {
                var pageSize = new Size(pc.OrientedPageMediaWidth.Value, pc.OrientedPageMediaHeight.Value);
                
                var desiredMargin = new Thickness(15);
                var printerMinMargins = reportGenerator.GetMinimumPageMargins(pc);
                AdjustMargins(ref desiredMargin, printerMinMargins);

                var pages = await reportGenerator.GenerateReport(reportFactory, pageSize, desiredMargin);
                GeneratedDocument = reportGenerator.GetFixedDocumentFromProcessedPages(pages, pageSize);
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
