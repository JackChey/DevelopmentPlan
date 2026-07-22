namespace InprovePlan.ApiTests.Contracts;

public class ProductDtoJson
{
    public long Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public long ProductTypeId { get; set; }
    public int ProductStatus { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
}

