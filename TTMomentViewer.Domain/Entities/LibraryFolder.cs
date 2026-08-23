namespace TTMomentViewer.Domain.Entities;

public class LibraryFolder
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public List<Moment> Moments { get; set; } = [];
}
