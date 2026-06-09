namespace Instructure.Specification;

using InprovePlan.Domain.BaseEntities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// 查询规范执行器。
/// 
/// 作用：
/// 把 Specification 中定义的条件真正应用到 IQueryable 上。
/// 
/// 关键点：
/// 1. Where 作用在 IQueryable 上，由数据库执行。
/// 2. Include 只在查询列表数据时使用。
/// 3. Count 查询时通常不需要 Include，避免生成不必要的复杂 SQL。
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>
    /// 应用查询规范。
    /// 
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <param name="query">原始 IQueryable，例如 dbContext.Orders。</param>
    /// <param name="specification">查询规范。</param>
    /// <param name="includeRelations">
    /// 是否应用 Include。
    /// 
    /// 查询 Items 时通常为 true。
    /// 查询 Count 时建议为 false。
    /// </param>
    /// <returns>应用条件后的 IQueryable。</returns>
    public static IQueryable<TEntity> Apply<TEntity>(
        IQueryable<TEntity> query,
        ISpecification<TEntity> specification,
        bool includeRelations = true)
        where TEntity : class,IEntity
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(specification);

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        foreach (var criteria in specification.Criteria)
        {
            query = query.Where(criteria);
        }

        if (includeRelations)
        {
            foreach (var include in specification.Includes)
            {
                query = query.Include(include);
            }
        }

        return query;
    }
}
