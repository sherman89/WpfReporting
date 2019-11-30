using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;

namespace Sherman.WpfReporting.Lib
{
    public class Printing
    {
        /// <summary>
        /// Get all printers in <see cref="PrinterType"/>.
        /// </summary>
        public IReadOnlyList<PrinterModel> GetPrinters()
        {
            const PrinterType flags = PrinterType.Usb | PrinterType.Pdf | PrinterType.Xps | PrinterType.Network;
            return GetPrinters(flags);
        }

        public IReadOnlyList<PrinterModel> GetPrinters(PrinterType printerTypes)
        {
            var printers = new List<PrinterModel>();

            PrintQueueCollection localQueues;
            using (var printServer = new PrintServer())
            {
                var flags = new[] { EnumeratedPrintQueueTypes.Local };
                localQueues = printServer.GetPrintQueues(flags);
            }

            if (printerTypes.HasFlag(PrinterType.Usb))
            {
                var usbPrinters = localQueues.Where(q => q.QueuePort.Name.StartsWith("USB"));
                foreach (var usbPrinter in usbPrinters)
                {
                    var pageSizeCapabilities = usbPrinter.GetPrintCapabilities().PageMediaSizeCapability;
                    printers.Add(new PrinterModel(usbPrinter.FullName, PrinterType.Usb, pageSizeCapabilities));
                }
            }

            if (printerTypes.HasFlag(PrinterType.Pdf))
            {
                var pdfPrintQueue = localQueues.SingleOrDefault(lq => lq.QueueDriver.Name == Constants.PdfPrinterDriveName);
                if (pdfPrintQueue != null)
                {
                    var pageSizeCapabilities = pdfPrintQueue.GetPrintCapabilities().PageMediaSizeCapability;
                    printers.Add(new PrinterModel(pdfPrintQueue.FullName, PrinterType.Pdf, pageSizeCapabilities));
                }
            }

            if (printerTypes.HasFlag(PrinterType.Xps))
            {
                var xpsPrintQueue = localQueues.SingleOrDefault(lq => lq.QueueDriver.Name == Constants.XpsPrinterDriveName);
                if (xpsPrintQueue != null)
                {
                    var pageSizeCapabilities = xpsPrintQueue.GetPrintCapabilities().PageMediaSizeCapability;
                    printers.Add(new PrinterModel(xpsPrintQueue.FullName, PrinterType.Xps, pageSizeCapabilities));
                }
            }

            if (printerTypes.HasFlag(PrinterType.Network))
            {
                PrintQueueCollection networkQueues;
                using (var printServer = new PrintServer())
                {
                    var flags = new[] { EnumeratedPrintQueueTypes.Connections };
                    networkQueues = printServer.GetPrintQueues(flags);
                }

                foreach (var networkQueue in networkQueues)
                {
                    var pageSizeCapabilities = networkQueue.GetPrintCapabilities().PageMediaSizeCapability;
                    printers.Add(new PrinterModel(networkQueue.FullName, PrinterType.Network, pageSizeCapabilities));
                    networkQueue.Dispose();
                }
            }

            foreach (var localQueue in localQueues)
            {
                localQueue.Dispose();
            }

            return printers;
        }

        public PrintTicket GetPrintTicket(string printerName, PageMediaSize paperSize, PageOrientation pageOrientation)
        {
            PrintQueueCollection printQueues;
            using (var printServer = new PrintServer())
            {
                var flags = new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections };
                printQueues = printServer.GetPrintQueues(flags);
            }

            var selectedQueue = printQueues.SingleOrDefault(pq => pq.FullName == printerName);
            if (selectedQueue != null)
            {
                var myTicket = new PrintTicket
                {
                    CopyCount = 1,
                    PageOrientation = pageOrientation,
                    OutputColor = OutputColor.Color,
                    PageMediaSize = paperSize
                };

                var mergeTicketResult = selectedQueue.MergeAndValidatePrintTicket(selectedQueue.DefaultPrintTicket, myTicket);
                //TODO: Validate merged ticket?
                
                return mergeTicketResult.ValidatedPrintTicket;
            }

            throw new Exception($"Printer name \"{printerName}\" not found in local or network queues.");
        }

        public PrintCapabilities GetPrinterCapabilitiesForPrintTicket(PrintTicket printTicket, string printerName)
        {
            using (var printServer = new PrintServer())
            {
                // GetPrintQueue(queueName) might not work with some types of network printers,
                // but giving the queue description strangely works, but this is not a safe solution.
                // Instead we just get all queues and filter them, that always works.
                var queues = printServer.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections });

                using (var queue = queues.SingleOrDefault(pq => pq.FullName == printerName))
                {
                    return queue?.GetPrintCapabilities(printTicket);
                }
            }
        }

        /// <summary>
        /// Returns the minimum page margins supported by the printer for a specific page size. Make sure the <see cref="PrintCapabilities"/> parameter
        /// contains the correct printer and page size, otherwise you will get the wrong margins.
        /// </summary>
        /// <param name="printerCapabilities"><see cref="PrintCapabilities"/> for a specific printer and page size.</param>
        /// <returns>Minimum margins that this printer supports for a given page size.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="printerCapabilities" /> is <see langword="null" />.</exception>
        public Thickness GetMinimumPageMargins(PrintCapabilities printerCapabilities)
        {
            if (printerCapabilities is null)
            {
                throw new ArgumentNullException(nameof(PrintCapabilities), $"{nameof(PrintCapabilities)} cannot be null.");
            }

            if (!printerCapabilities.OrientedPageMediaWidth.HasValue)
            {
                throw new ArgumentNullException(nameof(printerCapabilities.OrientedPageMediaWidth), $"{nameof(printerCapabilities.OrientedPageMediaWidth)} cannot be null.");
            }

            if (!printerCapabilities.OrientedPageMediaHeight.HasValue)
            {
                throw new ArgumentNullException(nameof(printerCapabilities.OrientedPageMediaHeight), $"{nameof(printerCapabilities.OrientedPageMediaHeight)} cannot be null.");
            }

            if (printerCapabilities.PageImageableArea == null)
            {
                throw new ArgumentNullException(nameof(printerCapabilities.PageImageableArea), $"{nameof(printerCapabilities.PageImageableArea)} cannot be null.");
            }

            var minLeftMargin = printerCapabilities.PageImageableArea.OriginWidth;
            var minTopMargin = printerCapabilities.PageImageableArea.OriginHeight;
            var minRightMargin = printerCapabilities.OrientedPageMediaWidth.Value - printerCapabilities.PageImageableArea.ExtentWidth - minLeftMargin;
            var minBottomMargin = printerCapabilities.OrientedPageMediaHeight.Value - printerCapabilities.PageImageableArea.ExtentHeight - minTopMargin;

            return new Thickness(minLeftMargin, minTopMargin, minRightMargin, minBottomMargin);
        }

        /// <summary>
        /// Writes the <see cref="DocumentPaginator"/> to an <see cref="XpsDocument"/> in memory and returns it as a bytearray.
        /// </summary>
        /// <param name="documentPaginator"></param>
        /// <returns></returns>
        public byte[] GetXpsFileBytesFromDocumentPaginator(DocumentPaginator documentPaginator)
        {
            if (documentPaginator == null)
            {
                throw new ArgumentNullException(nameof(DocumentPaginator), "DocumentPaginator cannot be null.");
            }

            // Convert FixedDocument to XPS file in memory
            var ms = new MemoryStream();
            var package = Package.Open(ms, FileMode.Create);
            var doc = new XpsDocument(package);
            var writer = XpsDocument.CreateXpsDocumentWriter(doc);
            writer.Write(documentPaginator);
            doc.Close();
            package.Close();
            
            // Get XPS file bytes
            var bytes = ms.ToArray();
            ms.Dispose();

            return bytes;
        }
    }
}
