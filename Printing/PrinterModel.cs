using System.Collections.Generic;
using System.Printing;

namespace Sherman.WpfReporting.Lib
{
    public class PrinterModel
    {
        public PrinterModel(string fullName, PrinterType printerType, IReadOnlyCollection<PageMediaSize> pageSizeCapabilities)
        {
            FullName = fullName;
            PrinterType = printerType;
            PageSizeCapabilities = pageSizeCapabilities;
        }

        public string FullName { get; }

        public PrinterType PrinterType { get; }

        public IReadOnlyCollection<PageMediaSize> PageSizeCapabilities { get; }
    }
}