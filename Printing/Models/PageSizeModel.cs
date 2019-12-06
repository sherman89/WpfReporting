using System.Printing;

namespace Sherman.WpfReporting.Lib.Models
{
    public class PageSizeModel
    {
        public PageSizeModel(PageMediaSize pageMediaSizeName)
        {
            PageMediaSize = pageMediaSizeName;
        }

        public PageMediaSize PageMediaSize { get; }

        public string PageSizeName => PageMediaSize.PageMediaSizeName.ToString();
    }
}