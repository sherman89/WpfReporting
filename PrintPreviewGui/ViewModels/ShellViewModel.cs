using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Caliburn.Micro;
using Sherman.WpfReporting.Gui.Reports;
using Sherman.WpfReporting.Lib;

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
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            InitializePrinters();
            LoadPrinterPageSizes();

            return base.OnActivateAsync(cancellationToken);
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

        private void InitializePrinters()
        {
            var printers = printing.GetPrinters();
            SupportedPrinters = new ObservableCollection<PrinterModel>(printers);
            selectedPrinter = SupportedPrinters.FirstOrDefault();
        }

        public void LoadPrinterPageSizes()
        {
            SupportedPageSizes.Clear();
            foreach (var pageMediaSize in SelectedPrinter.PageSizeCapabilities)
            {
                SupportedPageSizes.Add(new PageSizeModel(pageMediaSize));
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

            var printTicket = printing.GetPrintTicket(SelectedPrinter.FullName, SelectedPageSize.PageMediaSize, PageOrientation.Portrait);
            var pc = printing.GetPrinterCapabilitiesForPrintTicket(printTicket, SelectedPrinter.FullName);

            if (pc.OrientedPageMediaWidth.HasValue && pc.OrientedPageMediaHeight.HasValue)
            {
                var pageSize = new Size(pc.OrientedPageMediaWidth.Value, pc.OrientedPageMediaHeight.Value);
                
                var desiredMargin = new Thickness(15);
                var printerMinMargins = printing.GetMinimumPageMargins(pc);
                AdjustMargins(ref desiredMargin, printerMinMargins);

                var pages = await paginator.Paginate(reportFactory, pageSize, desiredMargin, CancellationToken.None);
                GeneratedDocument = paginator.GetFixedDocumentFromPages(pages, pageSize);
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
