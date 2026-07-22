namespace InprovePlan.ApiTests.Contracts;

public class PageMetadataJson
{
    public long Total { get; set; }
    public int Count { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
