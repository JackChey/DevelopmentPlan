namespace Instructure.Sorting;

using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;


/// <summary>
/// IQueryable 安全排序扩展。
///
/// 作用：
/// 1. 给 EF Core 查询统一追加排序。
/// 2. 只允许使用 SortWhitelist 中注册过的字段。
/// 3. 禁止根据前端字段名直接拼接动态 SQL。
/// 4. 自动追加兜底排序字段，保证分页稳定。
///
/// 注意：
/// 该扩展作用于 IQueryable，
/// 因此最终排序会被 EF Core 翻译成数据库 ORDER BY，
/// 不会先查全量数据再内存排序。
/// </summary>
public static class QueryableSortingExtensions
{
    /// <summary>
    /// 应用安全排序。
    ///
    /// 执行流程：
    /// 1. 校验 query、sortQuery、whitelist 是否为空。
    /// 2. 调用 whitelist.Validate 校验排序字段和排序方向。
    /// 3. 解析最终排序字段。
    /// 4. 解析最终排序方向。
    /// 5. 从白名单获取主排序字段表达式。
    /// 6. 应用 OrderBy 或 OrderByDescending。
    /// 7. 如果主排序字段不是兜底字段，则追加 ThenBy 或 ThenByDescending。
    ///
    /// 示例：
    /// 前端传：
    /// sortBy = createdAt
    /// sortDirection = desc
    ///
    /// 最终可能生成：
    /// ORDER BY CreatedAt DESC, Id DESC
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="query">EF Core IQueryable 查询对象。</param>
    /// <param name="sortQuery">前端传入的排序参数。</param>
    /// <param name="whitelist">当前实体的排序白名单。</param>
    /// <returns>已经应用排序的 IQueryable。</returns>
    /// <exception cref="ArgumentException">
    /// 当排序字段或排序方向非法时抛出。
    /// </exception>
    public static IQueryable<TEntity> ApplySafeSorting<TEntity>(
        this IQueryable<TEntity> query,
        SortQuery sortQuery,
        SortWhitelist<TEntity> whitelist)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(sortQuery);
        ArgumentNullException.ThrowIfNull(whitelist);

        var errors = whitelist.Validate(sortQuery);

        if (errors.Count > 0)
        {
            var message = string.Join("；", errors.Select(x => x.Message));
            throw new ValidationException(message);
        }

        var sortBy = whitelist.ResolveSortBy(sortQuery.SortBy);
        var direction = whitelist.ResolveDirection(sortQuery.SortDirection);

        if (!whitelist.TryGetField(sortBy, out var primaryField))
        {
            throw new ValidationException($"不支持的排序字段：{sortBy}。");
        }

        var orderedQuery = ApplyOrderBy(query, primaryField, direction);

        // 如果主排序字段不是兜底字段，则追加兜底排序。
        //
        // 为什么要追加：
        // 假设按 CreatedAt DESC 排序，
        // 如果多条记录 CreatedAt 完全相同，
        // 数据库无法保证这些记录之间的顺序每次都一致。
        //
        // 在分页场景下，这会导致：
        // - 第 1 页和第 2 页出现重复数据
        // - 某些数据被跳过
        //
        // 追加唯一字段 Id 后，排序变得稳定。
        if (!sortBy.Equals(whitelist.TieBreakerSortBy, StringComparison.OrdinalIgnoreCase))
        {
            if (!whitelist.TryGetField(whitelist.TieBreakerSortBy, out var tieBreakerField))
            {
                throw new ArgumentException($"兜底排序字段不存在：{whitelist.TieBreakerSortBy}。");
            }

            orderedQuery = ApplyThenBy(orderedQuery, tieBreakerField, direction);
        }

        return orderedQuery;
    }

    /// <summary>
    /// 应用主排序。
    ///
    /// 根据排序方向决定调用：
    /// - Queryable.OrderBy
    /// - Queryable.OrderByDescending
    ///
    /// 注意：
    /// 这里不是字符串拼接排序，
    /// 而是使用白名单中提前定义好的表达式。
    /// </summary>
    private static IOrderedQueryable<TEntity> ApplyOrderBy<TEntity>(
        IQueryable<TEntity> query,
        SortField<TEntity> field,
        SortDirection direction)
    {
        var methodName = direction == SortDirection.Asc
            ? nameof(Queryable.OrderBy)
            : nameof(Queryable.OrderByDescending);

        return InvokeOrderMethod<TEntity, IOrderedQueryable<TEntity>>(
            query,
            field.KeySelector,
            methodName);
    }

    /// <summary>
    /// 应用次级排序。
    ///
    /// 根据排序方向决定调用：
    /// - Queryable.ThenBy
    /// - Queryable.ThenByDescending
    ///
    /// 主要用于追加唯一字段兜底排序。
    /// </summary>
    private static IOrderedQueryable<TEntity> ApplyThenBy<TEntity>(
        IOrderedQueryable<TEntity> query,
        SortField<TEntity> field,
        SortDirection direction)
    {
        var methodName = direction == SortDirection.Asc
            ? nameof(Queryable.ThenBy)
            : nameof(Queryable.ThenByDescending);

        return InvokeOrderMethod<TEntity, IOrderedQueryable<TEntity>>(
            query,
            field.KeySelector,
            methodName);
    }

    /// <summary>
    /// 通过反射调用 Queryable 的排序方法。
    ///
    /// 为什么需要反射：
    /// Queryable.OrderBy 的真实签名是：
    ///
    /// OrderBy<TEntity, TKey>(
    ///     IQueryable<TEntity> source,
    ///     Expression<Func<TEntity, TKey>> keySelector)
    ///
    /// 其中 TKey 是排序字段的类型。
    ///
    /// 但是不同字段的 TKey 不一样：
    /// - CreatedAt：DateTime
    /// - Name：string
    /// - Amount：decimal
    /// - Id：long
    ///
    /// 因此 SortField<TEntity> 中统一保存 LambdaExpression，
    /// 这里再根据表达式的 Body.Type 推断 TKey，
    /// 然后构造正确的泛型方法。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TResult">返回类型，通常是 IOrderedQueryable<TEntity>。</typeparam>
    /// <param name="query">查询对象。</param>
    /// <param name="keySelector">排序表达式。</param>
    /// <param name="methodName">
    /// 要调用的方法名：
    /// - OrderBy
    /// - OrderByDescending
    /// - ThenBy
    /// - ThenByDescending
    /// </param>
    /// <returns>排序后的查询对象。</returns>
    private static TResult InvokeOrderMethod<TEntity, TResult>(
        IQueryable<TEntity> query,
        LambdaExpression keySelector,
        string methodName)
    {
        var keyType = keySelector.Body.Type;

        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == methodName
                && method.GetParameters().Length == 2);

        var genericMethod = method.MakeGenericMethod(typeof(TEntity), keyType);

        var result = genericMethod.Invoke(
            null,
            [query, keySelector]);

        return (TResult)result!;
    }
}
