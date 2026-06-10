namespace Instructure.Repositories;

using InprovePlan.Domain.BaseEntities;
using Instructure.Data;
using Instructure.Paging;
using Instructure.Sorting;
using Instructure.Specification;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// EF Core 只读仓储实现。
///
/// 该类只负责查询，不负责写入。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public class EfReadRepository<TEntity> : IReadRepository<TEntity>
    where TEntity : class,IEntity
{
    /// <summary>
    /// EF Core DbContext。
    /// </summary>
    protected readonly AppDbContext DbContext;

    /// <summary>
    /// 当前实体对应的 DbSet。
    /// </summary>
    protected readonly DbSet<TEntity> DbSet;

    public EfReadRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(
        object id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return await DbSet.FindAsync(
            [id],
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await DbSet
            .AsNoTracking()
            .AnyAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(
            specification,
            includeRelations: true);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(
            specification,
            includeRelations: true);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<long> LongCountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(
            specification,
            includeRelations: false);

        return await query.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<TEntity>> PageAsync(
        ISpecification<TEntity> specification,
        Pagination pagination,
        SortQuery sortQuery,
        SortWhitelist<TEntity> sortWhitelist,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);
        ArgumentNullException.ThrowIfNull(sortQuery);
        ArgumentNullException.ThrowIfNull(sortWhitelist);

        var pagingErrors = pagination.Validate();

        if (pagingErrors.Count > 0)
        {
            var message = string.Join("；", pagingErrors.Select(x => x.Message));

            throw new ValidationException(new Dictionary<string, string[]>() { { "分页信息异常", pagingErrors.Select(e => e.Message).ToArray() } });
        }

        // Count 查询只应用条件，不应用 Include、排序和分页。
        // 这样 SQL 更简单，性能更可控。
        var countQuery = ApplySpecification(
            specification,
            includeRelations: false);

        var total = await countQuery.LongCountAsync(cancellationToken);

        // Items 查询应用条件、Include、排序、分页。
        var itemsQuery = ApplySpecification(
            specification,
            includeRelations: true);

        itemsQuery = itemsQuery.ApplySafeSorting(
            sortQuery,
            sortWhitelist);

        var items = await itemsQuery
            .Skip(pagination.GetSkipCount())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<TEntity>.Create(
            items,
            total,
            pagination);
    }

    /// <summary>
    /// 应用查询规范。
    ///
    /// 统一通过 SpecificationEvaluator 处理：
    /// - AsNoTracking
    /// - Where 条件
    /// - Include
    ///
    /// 注意：
    /// 这里返回 IQueryable，不立即执行查询。
    /// 真正执行发生在 ToListAsync、FirstOrDefaultAsync、LongCountAsync。
    /// </summary>
    protected virtual IQueryable<TEntity> ApplySpecification(
        ISpecification<TEntity> specification,
        bool includeRelations)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return SpecificationEvaluator.Apply(
            DbSet.AsQueryable(),
            specification,
            includeRelations);
    }

    public async Task<TEntity?> FirstOrDefaultAsNoTrackingAsync(
    Expression<Func<TEntity, bool>> predicate,
    CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }
}
