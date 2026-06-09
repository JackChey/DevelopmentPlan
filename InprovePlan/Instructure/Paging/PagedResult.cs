using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Paging;

/// <summary>
/// 统一分页查询结果。
/// 
/// 生产标准返回结构：
/// - Total：符合条件的总数
/// - Count：当前页返回数量
/// - Items：当前页数据
/// - Metadata：分页辅助信息
/// 
/// 不继承 List<T>，避免 JSON 序列化时丢失分页元数据。
/// </summary>
/// <typeparam name="T">当前页数据类型。</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// 符合查询条件的总记录数。
    /// </summary>
    public long Total { get; init; }

    /// <summary>
    /// 当前页实际返回数量。
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// 当前页数据。
    /// 
    /// 无结果或越界页时返回空集合，不返回 null。
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// 分页元数据。
    /// </summary>
    public PageMetadata Metadata { get; init; } = default!;

    /// <summary>
    /// 创建分页结果。
    /// 
    /// 该方法保证：
    /// 1. items 为 null 时自动转为空集合。
    /// 2. Count 永远等于当前页 Items 的数量。
    /// 3. TotalPages 根据 Total 和 PageSize 计算。
    /// </summary>
    public static PagedResult<T> Create(
        IReadOnlyList<T>? items,
        long total,
        Pagination pagination)
    {
        var safeItems = items ?? [];

        var totalPages = total == 0
            ? 0
            : (int)Math.Ceiling(total / (double)pagination.PageSize);

        return new PagedResult<T>
        {
            Total = total,
            Count = safeItems.Count,
            Items = safeItems,
            Metadata = new PageMetadata
            {
                Total = total,
                Count = safeItems.Count,
                PageIndex = pagination.PageIndex,
                PageSize = pagination.PageSize,
                TotalPages = totalPages
            }
        };
    }

    /// <summary>
    /// 创建空分页结果。
    /// 
    /// 常用于：
    /// - 查询无结果
    /// - 越界页
    /// - 业务规则直接判定为空
    /// </summary>
    public static PagedResult<T> Empty(Pagination pagination)
    {
        return Create([], 0, pagination);
    }
}
