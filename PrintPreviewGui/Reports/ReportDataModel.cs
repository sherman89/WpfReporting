namespace Sherman.WpfReporting.Gui.Reports
{
    public class ReportDataModel
    {
        public ReportDataModel(int id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
    }
}