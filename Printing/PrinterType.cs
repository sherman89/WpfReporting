using System;

namespace Sherman.WpfReporting.Lib
{
    [Flags]
    public enum PrinterType
    {
        Usb = 0,
        Network = 1,
        Pdf = 2,
        Xps = 3
    }
}