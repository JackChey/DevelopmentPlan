namespace Instructure.Repositories;

using InprovePlan.Domain.BaseEntities;
using Instructure.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core 可写仓储实现。
///
/// 继承 EfReadRepository，表示：
/// 既拥有查询能力，也拥有写入能力。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public class EfRepository<TEntity>
    : EfReadRepository<TEntity>, IRepository<TEntity>
    where TEntity : class,IEntity
{
    public EfRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await DbSet.AddAsync(
            entity,
            cancellationToken);

        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<bool> TryAddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            await DbSet.AddAsync(
           entity,
           cancellationToken);

        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public virtual async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        await DbSet.AddRangeAsync(
            entities,
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DbSet.Remove(entity);
    }

    /// <inheritdoc />
    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        DbSet.RemoveRange(entities);
    }

    /// <inheritdoc />
    public virtual Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }
}
