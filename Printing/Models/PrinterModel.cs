using System.Collections.Generic;
using System.Printing;

namespace Sherman.WpfReporting.Lib.Models
{
    public class PrinterModel
    {
        public PrinterModel(string fullName, PrinterType printerType, IReadOnlyCollection<PageMediaSize> pageSizeCapabilities, IReadOnlyCollection<PageOrientation> pageOrientationCapabilities)
        {
            FullName = fullName;
            PrinterType = printerType;
            PageSizeCapabilities = pageSizeCapabilities;
            PageOrientationCapabilities = pageOrientationCapabilities;
        }

        public string FullName { get; }

        public PrinterType PrinterType { get; }

        public IReadOnlyCollection<PageMediaSize> PageSizeCapabilities { get; }

        public IReadOnlyCollection<PageOrientation> PageOrientationCapabilities { get; }
    }
}