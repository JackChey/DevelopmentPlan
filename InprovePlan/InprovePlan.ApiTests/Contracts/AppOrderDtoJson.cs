using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.ApiTests.Contracts;

public class AppOrderDtoJson
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public long UserId { get; set; }
    public int OrderStatus { get; set; }
    public long AddressId { get; set; }
}
