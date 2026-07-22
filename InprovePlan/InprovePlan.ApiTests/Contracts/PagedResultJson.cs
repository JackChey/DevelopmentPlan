namespace InprovePlan.ApiTests.Contracts;

public class PagedResultJson<T>
{
    public long Total { get; set; }
    public int Count { get; set; }
    public IReadOnlyList<T> Items { get; set; } = [];
    public PageMetadataJson Metadata { get; set; } = new();
}
