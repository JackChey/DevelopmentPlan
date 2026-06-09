using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Paging;

/// <summary>
/// 分页查询参数。
/// 
/// 生产约定：
/// 1. PageIndex 从 1 开始，不使用从 0 开始的页码。
/// 2. PageSize 必须大于 0。
/// 3. PageSize 不能超过 MaxPageSize。
/// 4. 非法分页参数不自动修正，应由应用层返回明确错误响应。
/// </summary>
public sealed class Pagination
{
    /// <summary>
    /// 默认页码。
    /// </summary>
    public const int DefaultPageIndex = 1;

    /// <summary>
    /// 默认每页条数。
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// 允许的最大每页条数。
    /// 
    /// 防止前端传入过大的 pageSize，导致数据库压力过高。
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// 当前页码。
    /// 
    /// 约定从 1 开始：
    /// - PageIndex = 1 表示第一页
    /// - PageIndex = 2 表示第二页
    /// 
    /// 不允许小于 1。
    /// </summary>
    public int PageIndex { get; init; } = DefaultPageIndex;

    /// <summary>
    /// 每页条数。
    /// 
    /// 必须满足：
    /// - 大于 0
    /// - 小于等于 MaxPageSize
    /// </summary>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>
    /// 获取 EF Core Skip 需要的跳过数量。
    /// 
    /// 例如：
    /// PageIndex = 1, PageSize = 20 => Skip = 0
    /// PageIndex = 2, PageSize = 20 => Skip = 20
    /// PageIndex = 3, PageSize = 20 => Skip = 40
    /// </summary>
    public int GetSkipCount()
    {
        // 使用 long 计算，避免极端参数下 int 溢出。
        var skip = ((long)PageIndex - 1) * PageSize;

        if (skip > int.MaxValue)
        {
            throw new ValidationException("分页偏移量过大，请缩小 pageIndex 或 pageSize。");
        }

        return (int)skip;
    }

    /// <summary>
    /// 校验分页参数。
    /// 
    /// 仓储层执行查询前，应确保该方法返回空集合。
    /// Controller 或 ApplicationService 可以根据错误集合返回统一错误响应。
    /// </summary>
    public IReadOnlyList<PagingValidationError> Validate()
    {
        var errors = new List<PagingValidationError>();

        if (PageIndex < 1)
        {
            errors.Add(new PagingValidationError(
                Field: nameof(PageIndex),
                Message: "pageIndex 必须大于等于 1。"));
        }

        if (PageSize < 1)
        {
            errors.Add(new PagingValidationError(
                Field: nameof(PageSize),
                Message: "pageSize 必须大于等于 1。"));
        }

        if (PageSize > MaxPageSize)
        {
            errors.Add(new PagingValidationError(
                Field: nameof(PageSize),
                Message: $"pageSize 不能大于 {MaxPageSize}。"));
        }

        var skip = ((long)PageIndex - 1) * PageSize;

        if (skip > int.MaxValue)
        {
            errors.Add(new PagingValidationError(
                Field: nameof(PageIndex),
                Message: "分页偏移量过大，请缩小 pageIndex 或 pageSize。"));
        }

        return errors;
    }

    /// <summary>
    /// 判断当前分页参数是否合法。
    /// </summary>
    public bool IsValid => Validate().Count == 0;
}
