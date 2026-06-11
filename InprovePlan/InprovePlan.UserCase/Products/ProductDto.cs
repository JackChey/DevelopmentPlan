using InprovePlan.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.UserCase.Products;

/// <summary>
/// 商品返回 DTO。
///
/// 注意：
/// DTO 用于接口返回，不直接返回领域实体。
/// </summary>
public sealed record ProductDto(
    long Id,
    string ProductCode,
    string ProductName,
    string ProductDescription,
    int ProductTypeId,
    AppProductStatus ProductStatus,
    decimal UnitPrice,
    string Currency);