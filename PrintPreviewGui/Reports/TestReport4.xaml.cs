namespace Sherman.WpfReporting.Gui.Reports
{
    public partial class TestReport4
    {
        public TestReport4()
        {
            InitializeComponent();

            for (var i = 1; i <= 100; i++)
            {
                ItemsControl1.Items.Add(new ReportDataModel(i, $"Row {i}", "Part of list 1"));
            }

            for (var i = 1; i <= 50; i++)
            {
                ItemsControl2.Items.Add(new ReportDataModel(i, $"Row {i}", "Part of list 2"));
            }
        }
    }
}
