using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using PrintPreviewGui.ViewModels;

namespace PrintPreviewGui
{
    public class Printing
    {
        public List<PrinterViewModel> GetPrinters()
        {
            var printers = new List<PrinterViewModel>();

            PrintQueueCollection localQueues;
            using (var printServer = new PrintServer())
            {
                var flags = new[] { EnumeratedPrintQueueTypes.Local };
                localQueues = printServer.GetPrintQueues(flags);
            }

            // USB Printers
            var usbPrinters = localQueues.Where(q => q.QueuePort.Name.StartsWith("USB"));
            foreach (var usbPrinter in usbPrinters)
            {
                var pageSizeCapabilities = usbPrinter.GetPrintCapabilities().PageMediaSizeCapability;
                printers.Add(new PrinterViewModel(usbPrinter.FullName, PrinterType.Usb, pageSizeCapabilities));
            }

            // Microsoft PDF Printer
            var pdfPrintQueue = localQueues.SingleOrDefault(lq => lq.QueueDriver.Name == Constants.PdfPrinterDriveName);
            if (pdfPrintQueue != null)
            {
                var pageSizeCapabilities = pdfPrintQueue.GetPrintCapabilities().PageMediaSizeCapability;
                printers.Add(new PrinterViewModel(pdfPrintQueue.FullName, PrinterType.Pdf, pageSizeCapabilities));
            }

            // Microsoft XPS Printer
            var xpsPrintQueue = localQueues.SingleOrDefault(lq => lq.QueueDriver.Name == Constants.XpsPrinterDriveName);
            if (xpsPrintQueue != null)
            {
                var pageSizeCapabilities = xpsPrintQueue.GetPrintCapabilities().PageMediaSizeCapability;
                printers.Add(new PrinterViewModel(xpsPrintQueue.FullName, PrinterType.Xps, pageSizeCapabilities));
            }

            // Network printers
            PrintQueueCollection networkQueues;
            using (var printServer = new PrintServer())
            {
                var flags = new[] { EnumeratedPrintQueueTypes.Connections };
                networkQueues = printServer.GetPrintQueues(flags);
            }

            foreach (var networkPrinter in networkQueues)
            {
                var pageSizeCapabilities = networkPrinter.GetPrintCapabilities().PageMediaSizeCapability;
                printers.Add(new PrinterViewModel(networkPrinter.FullName, PrinterType.Network, pageSizeCapabilities));
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
            using var printServer = new PrintServer();

            // GetPrintQueue(queueName) might not work with some types of network printers,
            // but giving the queue description strangely works, but this is not a safe solution.
            // Instead we just get all queues and filter them, that always works.
            var queues = printServer.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections });

            using var queue = queues.SingleOrDefault(pq => pq.FullName == printerName);
            return queue?.GetPrintCapabilities(printTicket);
        }
    }
}
