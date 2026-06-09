using System.ComponentModel.DataAnnotations;

namespace Instructure.Sorting;

/// <summary>
/// 某个实体的排序白名单。
/// 
/// 每个实体允许排序的字段通常不同，
/// 所以白名单应该按实体分别定义。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public sealed class SortWhitelist<TEntity>
{
    private readonly Dictionary<string, SortField<TEntity>> _fields;

    public SortWhitelist(
        IEnumerable<SortField<TEntity>> fields,
        string defaultSortBy,
        SortDirection defaultDirection,
        string tieBreakerSortBy)
    {
        _fields = fields.ToDictionary(
            field => field.Name,
            field => field,
            StringComparer.OrdinalIgnoreCase);

        DefaultSortBy = defaultSortBy;
        DefaultDirection = defaultDirection;
        TieBreakerSortBy = tieBreakerSortBy;

        if (!_fields.ContainsKey(DefaultSortBy))
        {
            throw new ValidationException($"默认排序字段 {DefaultSortBy} 不在排序白名单中。");
        }

        if (!_fields.ContainsKey(TieBreakerSortBy))
        {
            throw new ValidationException($"兜底排序字段 {TieBreakerSortBy} 不在排序白名单中。");
        }
    }

    /// <summary>
    /// 默认排序字段。
    /// 
    /// 当前端不传 sortBy 时使用。
    /// </summary>
    public string DefaultSortBy { get; }

    /// <summary>
    /// 默认排序方向。
    /// </summary>
    public SortDirection DefaultDirection { get; }

    /// <summary>
    /// 兜底排序字段。
    /// 
    /// 通常使用唯一字段，例如 id。
    /// 
    /// 目的：
    /// 当主排序字段值相同时，仍然保证排序稳定。
    /// </summary>
    public string TieBreakerSortBy { get; }

    /// <summary>
    /// 尝试获取排序字段定义。
    /// </summary>
    public bool TryGetField(
        string sortBy,
        out SortField<TEntity> field)
    {
        return _fields.TryGetValue(sortBy, out field!);
    }

    /// <summary>
    /// 校验排序参数。
    /// </summary>
    public IReadOnlyList<SortValidationError> Validate(SortQuery query)
    {
        var errors = new List<SortValidationError>();

        var sortBy = string.IsNullOrWhiteSpace(query.SortBy)
            ? DefaultSortBy
            : query.SortBy.Trim();

        if (!_fields.ContainsKey(sortBy))
        {
            errors.Add(new SortValidationError(
                Field: nameof(query.SortBy),
                Message: $"不支持的排序字段：{sortBy}。"));
        }

        if (!string.IsNullOrWhiteSpace(query.SortDirection)
            && !IsValidDirection(query.SortDirection))
        {
            errors.Add(new SortValidationError(
                Field: nameof(query.SortDirection),
                Message: "排序方向只允许 asc 或 desc。"));
        }

        return errors;
    }

    /// <summary>
    /// 解析排序方向。
    /// 
    /// 如果前端没有传排序方向，则使用默认方向。
    /// </summary>
    public SortDirection ResolveDirection(string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortDirection))
        {
            return DefaultDirection;
        }

        return sortDirection.Trim().Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? SortDirection.Asc
            : SortDirection.Desc;
    }

    /// <summary>
    /// 解析排序字段。
    /// 
    /// 如果前端没有传排序字段，则使用默认排序字段。
    /// </summary>
    public string ResolveSortBy(string? sortBy)
    {
        return string.IsNullOrWhiteSpace(sortBy)
            ? DefaultSortBy
            : sortBy.Trim();
    }

    private static bool IsValidDirection(string value)
    {
        return value.Equals("asc", StringComparison.OrdinalIgnoreCase)
            || value.Equals("desc", StringComparison.OrdinalIgnoreCase);
    }
}
