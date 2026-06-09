using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Paging;

/// <summary>
/// 分页元数据。
/// 
/// 用于描述当前分页状态，方便前端判断是否还有上一页、下一页。
/// </summary>
public sealed class PageMetadata
{
    /// <summary>
    /// 符合查询条件的总记录数。
    /// 
    /// 注意：
    /// 这是数据库 Count 查询得到的总数，
    /// 不是当前页返回的数量。
    /// </summary>
    public long Total { get; init; }

    /// <summary>
    /// 当前页实际返回数量。
    /// 
    /// 例如：
    /// PageSize = 20，但最后一页只有 3 条，
    /// 则 Count = 3。
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// 当前页码。
    /// 
    /// 与请求参数 PageIndex 保持一致。
    /// </summary>
    public int PageIndex { get; init; }

    /// <summary>
    /// 每页条数。
    /// 
    /// 与请求参数 PageSize 保持一致。
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// 总页数。
    /// 
    /// Total = 0 时，TotalPages = 0。
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// 是否存在上一页。
    /// </summary>
    public bool HasPrevious => PageIndex > 1 && TotalPages > 0;

    /// <summary>
    /// 是否存在下一页。
    /// </summary>
    public bool HasNext => PageIndex < TotalPages;
}
