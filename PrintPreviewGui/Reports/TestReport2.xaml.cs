using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Sherman.WpfReporting.Gui.Reports
{
    public partial class TestReport2
    {
        public TestReport2()
        {
            InitializeComponent();

            for (var i = 1; i <= 100; i++)
            {
                ListView.Items.Add(new ReportDataModel(i, $"Row {i}", $"This is the description of row number {i}"));
            }
        }

        private void ListView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var listView = (ListView) sender;
            var gridView = (GridView) listView.View;
            var totalColWidth = gridView.Columns.Sum(c => c.ActualWidth) - gridView.Columns[2].Width;

            // 10 is a magic number just to get the horizontal scrollbar to disappear.
            // we don't really care about correctness much since this is just a test report.
            var descriptionColWidth = listView.ActualWidth - totalColWidth - SystemParameters.VerticalScrollBarWidth - 10;
            gridView.Columns[2].Width = descriptionColWidth;
        }
    }
}
