using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Sorting;

/// <summary>
/// 排序查询参数。
/// 
/// 该对象一般由前端传入。
/// 例如：
/// sortBy=createdAt
/// sortDirection=desc
/// </summary>
public sealed class SortQuery
{
    /// <summary>
    /// 排序字段。
    /// 
    /// 注意：
    /// 这里接收的是前端字段名，不一定等于实体属性名。
    /// 
    /// 例如前端传 createdAt，
    /// 后端可以映射到实体的 CreatedAt 属性。
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// 排序方向。
    /// 
    /// 只允许：
    /// - asc
    /// - desc
    /// 
    /// 不建议直接接收任意字符串后拼接 SQL。
    /// </summary>
    public string? SortDirection { get; init; }
}
