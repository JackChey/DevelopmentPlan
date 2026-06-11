using InprovePlan.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.UserCase.AppOrders;

/// <summary>
/// 订单 DTO。
///
/// 注意：
/// TotalAmount 当前没有映射数据库列，
/// 但可以通过 UnitPrice * Quantity 计算后返回。
/// </summary>
public sealed record AppOrderDto(
    long Id,
    string OrderNo,
    long ProductId,
    string ProductName,
    string ProductCode,
    string Currency,
    decimal UnitPrice,
    decimal Quantity,
    decimal TotalAmount,
    long UserId,
    DateTimeOffset OccurredTime,
    AppOrderStatus OrderStatus,
    bool Cancelled,
    long AddressId);