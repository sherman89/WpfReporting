using System.Printing;

namespace Sherman.WpfReporting.Lib
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